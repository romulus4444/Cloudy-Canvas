namespace Cloudy_Canvas.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Polly;

    /// <summary>
    /// Talks to the Manebooru (Philomena) JSON API. Every lookup returns a result describing what happened instead of throwing, so a
    /// timeout or an outage can be told apart from "no results".
    /// </summary>
    public class BooruService
    {
        /// <summary>The API returns at most 50 tags per page.</summary>
        public const int TagBatchSize = 50;

        private const int MaxParallelTagRequests = 3;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly ManebooruSettings _settings;
        private readonly ILogger<BooruService> _logger;

        public BooruService(HttpClient http, IOptions<ManebooruSettings> settings, ILogger<BooruService> logger)
        {
            _http = http;
            _settings = settings.Value;
            _logger = logger;
        }

        /// <summary>Looks up one image by id. Fills in ImageId, Spoilered and SpoilerTags.</summary>
        public async Task<BooruResult> GetImageByIdAsync(long imageId, ServerSettings settings, int filterId)
        {
            var query = QueryHelper.ApplySafeMode($"id:{imageId}", settings.SafeMode);
            return ToImageResult(await SearchImagesAsync(query, filterId), settings);
        }

        /// <summary>Picks a random image matching the query. Fills in Total as well.</summary>
        public async Task<BooruResult> GetRandomImageByQueryAsync(string query, ServerSettings settings, int filterId)
        {
            var safeQuery = QueryHelper.ApplySafeMode(query, settings.SafeMode);
            return ToImageResult(await SearchImagesAsync(safeQuery, filterId, ("sf", "random")), settings);
        }

        /// <summary>Gets the most recently added image matching the query. Fills in Total as well.</summary>
        public async Task<BooruResult> GetFirstRecentImageByQueryAsync(string query, ServerSettings settings, int filterId)
        {
            var safeQuery = QueryHelper.ApplySafeMode(query, settings.SafeMode);
            return ToImageResult(await SearchImagesAsync(safeQuery, filterId, ("sf", "first_seen_at"), ("sd", "desc")), settings);
        }

        /// <summary>Gets the current featured image. NotFound if the filter hides it.</summary>
        public async Task<BooruResult> GetFeaturedImageIdAsync(ServerSettings settings, int filterId)
        {
            var featured = await GetJsonAsync<FeaturedResponse>("api/v1/json/images/featured");
            if (featured.Status != BooruStatus.Ok)
            {
                return Failure(featured);
            }

            var featuredId = featured.Value.Image?.Id ?? 0;
            return featuredId <= 0 ? BooruResult.NotFound() : await GetImageByIdAsync(featuredId, settings, filterId);
        }

        /// <summary>Gets an image's tags. Fills in Tags (without the spoilered ones), Spoilered and SpoilerTags.</summary>
        public async Task<BooruResult> GetImageTagsIdAsync(long imageId, ServerSettings settings, int filterId)
        {
            var query = QueryHelper.ApplySafeMode($"id:{imageId}", settings.SafeMode);
            var search = await SearchImagesAsync(query, filterId);
            if (search.Status != BooruStatus.Ok)
            {
                return Failure(search);
            }

            var image = search.Value.Images?.FirstOrDefault();
            if (search.Value.Total <= 0 || image == null)
            {
                return BooruResult.NotFound();
            }

            var tagIds = image.TagIds ?? new List<long>();
            var tagNames = image.Tags ?? new List<string>();
            if (tagIds.Count != tagNames.Count)
            {
                _logger.LogWarning("Image {ImageId} has {Ids} tag ids but {Names} tag names; treating it as not found", image.Id, tagIds.Count, tagNames.Count);
                return BooruResult.NotFound();
            }

            var spoilerTags = FindSpoilerTags(tagIds, settings);
            return new BooruResult
            {
                Status = BooruStatus.Ok,
                ImageId = image.Id,
                Total = 1,
                Spoilered = image.Spoilered,
                SpoilerTags = spoilerTags,
                Tags = tagNames.Where(name => !spoilerTags.Contains(name)).ToList(),
            };
        }

        /// <summary>Checks that a filter exists and is public. On success FilterId is the filter's id.</summary>
        public async Task<FilterCheckResult> CheckFilterAsync(int filter)
        {
            var response = await GetJsonAsync<FilterResponse>($"api/v1/json/filters/{filter}");
            if (response.Status == BooruStatus.HttpError && response.HttpCode is 403 or 404)
            {
                return new FilterCheckResult(BooruStatus.NotFound, null, 0);
            }

            if (response.Status != BooruStatus.Ok)
            {
                return new FilterCheckResult(response.Status, response.HttpCode, 0);
            }

            var id = response.Value.Filter?.Id ?? 0;
            return id > 0 ? new FilterCheckResult(BooruStatus.Ok, null, id) : new FilterCheckResult(BooruStatus.NotFound, null, 0);
        }

        /// <summary>
        /// Rebuilds the server's spoiler list from its default filter and saves it. If the site can't be reached the existing list is
        /// kept and the reason is returned.
        /// </summary>
        public async Task<BooruStatus> RefreshListsAsync(SocketCommandContext context, ServerSettings settings)
        {
            var fetched = await FetchSpoilerTagsAsync(settings.DefaultFilterId);
            if (fetched.Status != BooruStatus.Ok)
            {
                return fetched.Status;
            }

            settings.SpoilerList = fetched.Tags.ToList();
            await FileHelper.SaveServerSettingsAsync(settings, context);
            return BooruStatus.Ok;
        }

        /// <summary>
        /// Gets the filter's spoilered tags with their names. Names are looked up 50 at a time (a few requests in parallel) instead of
        /// one request per tag, and the whole fetch fails if any batch does, so a partial list is never returned.
        /// </summary>
        public async Task<SpoilerListResult> FetchSpoilerTagsAsync(int filterId)
        {
            var filter = await GetJsonAsync<FilterResponse>($"api/v1/json/filters/{filterId}");
            if (filter.Status != BooruStatus.Ok)
            {
                return new SpoilerListResult(filter.Status, filter.HttpCode, Array.Empty<SpoilerTag>());
            }

            var tagIds = (filter.Value.Filter?.SpoileredTagIds ?? new List<long>()).Distinct().ToList();
            var names = new Dictionary<long, string>();
            using var gate = new SemaphoreSlim(MaxParallelTagRequests);
            var batches = await Task.WhenAll(tagIds.Chunk(TagBatchSize).Select(async batch =>
            {
                await gate.WaitAsync();
                try
                {
                    var query = string.Join(" || ", batch.Select(id => $"id:{id}"));
                    return await GetJsonAsync<TagSearchResponse>(
                        "api/v1/json/search/tags", ("q", query), ("per_page", TagBatchSize.ToString()));
                }
                finally
                {
                    gate.Release();
                }
            }));

            var failed = batches.FirstOrDefault(batch => batch.Status != BooruStatus.Ok);
            if (failed != null)
            {
                return new SpoilerListResult(failed.Status, failed.HttpCode, Array.Empty<SpoilerTag>());
            }

            foreach (var tag in batches.SelectMany(batch => batch.Value.Tags ?? new List<TagDto>()))
            {
                names[tag.Id] = tag.Name;
            }

            var tags = new List<SpoilerTag>();
            foreach (var id in tagIds)
            {
                if (!names.TryGetValue(id, out var name))
                {
                    // A deleted tag can still be listed in the filter. Keep it, so images that carry it are still spoilered.
                    _logger.LogWarning("Spoilered tag {TagId} of filter {FilterId} was not found; using a placeholder name", id, filterId);
                    name = $"tag #{id}";
                }

                tags.Add(new SpoilerTag(id, name));
            }

            return new SpoilerListResult(BooruStatus.Ok, null, tags);
        }

        /// <summary>The names of the image's tags that the server spoilers, in the order the image lists them.</summary>
        public static List<string> FindSpoilerTags(IEnumerable<long> imageTagIds, ServerSettings settings)
        {
            var spoilers = new Dictionary<long, string>();
            foreach (var spoiler in settings.SpoilerList)
            {
                spoilers.TryAdd(spoiler.Id, spoiler.Name);
            }

            return (imageTagIds ?? Array.Empty<long>()).Where(spoilers.ContainsKey).Select(id => spoilers[id]).ToList();
        }

        private static BooruResult ToImageResult(ApiResult<SearchResponse> search, ServerSettings settings)
        {
            if (search.Status != BooruStatus.Ok)
            {
                return Failure(search);
            }

            var image = search.Value.Images?.FirstOrDefault();
            if (search.Value.Total <= 0 || image == null)
            {
                return BooruResult.NotFound();
            }

            return new BooruResult
            {
                Status = BooruStatus.Ok,
                ImageId = image.Id,
                Total = search.Value.Total,
                Spoilered = image.Spoilered,
                SpoilerTags = FindSpoilerTags(image.TagIds, settings),
            };
        }

        private static BooruResult Failure<T>(ApiResult<T> failed)
        {
            return new BooruResult { Status = failed.Status, HttpCode = failed.HttpCode };
        }

        private Task<ApiResult<SearchResponse>> SearchImagesAsync(string query, int filterId, params (string Key, string Value)[] extra)
        {
            var parameters = new List<(string Key, string Value)>
            {
                ("q", query),
                ("per_page", "1"),
                ("filter_id", filterId.ToString()),
            };
            parameters.AddRange(extra);
            if (!string.IsNullOrEmpty(_settings.token))
            {
                parameters.Add(("key", _settings.token));
            }

            return GetJsonAsync<SearchResponse>("api/v1/json/search/images", parameters.ToArray());
        }

        private async Task<ApiResult<T>> GetJsonAsync<T>(string path, params (string Key, string Value)[] query)
            where T : class
        {
            var uri = query.Length == 0
                ? path
                : $"{path}?{string.Join("&", query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"))}";

            try
            {
                using var response = await _http.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<T>.FromHttpError((int)response.StatusCode);
                }

                var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                return value == null ? ApiResult<T>.FromUnavailable() : ApiResult<T>.FromValue(value);
            }
            catch (Exception ex) when (ex is HttpRequestException or TimeoutException or TaskCanceledException or JsonException or NotSupportedException or ExecutionRejectedException)
            {
                // Logged without the request URL: for image searches it carries the API key.
                _logger.LogWarning("Manebooru request to {Path} failed: {Reason}", path, ex.Message);
                return ApiResult<T>.FromUnavailable();
            }
        }

        private sealed class ApiResult<T>
        {
            public BooruStatus Status { get; private init; }

            public int? HttpCode { get; private init; }

            public T Value { get; private init; }

            public static ApiResult<T> FromValue(T value) => new() { Status = BooruStatus.Ok, Value = value };

            public static ApiResult<T> FromHttpError(int code) => new() { Status = BooruStatus.HttpError, HttpCode = code };

            public static ApiResult<T> FromUnavailable() => new() { Status = BooruStatus.Unavailable };
        }
    }
}
