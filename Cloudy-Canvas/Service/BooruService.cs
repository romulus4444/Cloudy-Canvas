namespace Cloudy_Canvas.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;
    using Flurl;
    using Flurl.Http;
    using Microsoft.Extensions.Options;

    public class BooruService
    {
        private readonly ManebooruSettings _settings;

        public BooruService(IOptions<ManebooruSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<Tuple<long, bool, List<string>>> GetImageByIdAsync(long imageId, ServerSettings settings)
        {
            //GET	/api/v1/json/images/:image_id
            const long searchResult = -1;
            var emptyList = new List<string>();
            var returnResult = new Tuple<long, bool, List<string>>(searchResult, false, emptyList);
            var safeQuery = "id:" + imageId + ", safe";
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/images")
                .SetQueryParams(new { key = _settings.token, q = safeQuery, filter_id = settings.filterId })
                .GetAsync()
                .ReceiveJson();
            if (results.total <= 0)
            {
                return returnResult;
            }

            bool spoiler = results.images[0].spoilered;
            var spoilerList = await CheckSpoilerListAsync(results.images[0].tag_ids, settings);
            returnResult = new Tuple<long, bool, List<string>>(results.images[0].id, spoiler, spoilerList);
            return (returnResult);
        }

        public async Task<Tuple<long, long, bool, List<string>>> GetRandomImageByQueryAsync(string query, ServerSettings settings)
        {
            //GET	/api/v1/json/search/images?q=safe
            long searchResult = -1;
            long numberOfResults = 0;
            var emptyList = new List<string>();
            var returnResult = new Tuple<long, long, bool, List<string>>(searchResult, numberOfResults, false, emptyList);
            var safeQuery = query + ", safe";
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/images")
                .SetQueryParams(new { key = _settings.token, q = safeQuery, per_page = 1, filter_id = settings.filterId })
                .GetAsync()
                .ReceiveJson();
            numberOfResults = results.total;
            if (numberOfResults <= 0)
            {
                return returnResult;
            }

            var page = new Random().Next((int)numberOfResults) + 1;
            results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/images")
                .SetQueryParams(new { key = _settings.token, q = query, per_page = 1, page, filter_id = settings.filterId })
                .GetAsync()
                .ReceiveJson();
            searchResult = results.images[0].id;
            bool spoiler = results.images[0].spoilered;
            var spoilerList = await CheckSpoilerListAsync(results.images[0].tag_ids, settings);
            returnResult = new Tuple<long, long, bool, List<string>>(searchResult, numberOfResults, spoiler, spoilerList);
            return (returnResult);
        }

        public async Task<Tuple<long, long, bool, List<string>>> GetFirstRecentImageByQueryAsync(string query, ServerSettings settings)
        {
            //GET	/api/v1/json/search/images?q=safe
            long searchResult = -1;
            long numberOfResults = 0;
            var emptyList = new List<string>();
            var returnResult = new Tuple<long, long, bool, List<string>>(searchResult, numberOfResults, false, emptyList);
            var safeQuery = query + ", safe";
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/images")
                .SetQueryParams(new { key = _settings.token, q = safeQuery, per_page = 1, sf = "first_seen_at", sd = "desc", filter_id = settings.filterId })
                .GetAsync()
                .ReceiveJson();
            numberOfResults = results.total;
            if (numberOfResults <= 0)
            {
                return returnResult;
            }

            searchResult = results.images[0].id;
            bool spoiler = results.images[0].spoilered;
            var spoilerList = await CheckSpoilerListAsync(results.images[0].tag_ids, settings);
            returnResult = new Tuple<long, long, bool, List<string>>(searchResult, numberOfResults, spoiler, spoilerList);
            return (returnResult);
        }

        public async Task<long> GetFeaturedImageIdAsync()
        {
            //GET	/api/v1/json/images/featured
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/images/featured")
                .GetAsync()
                .ReceiveJson();
            var output = results.image.id;
            return output;
        }

        public async Task<Tuple<List<string>, bool, List<string>>> GetImageTagsIdAsync(long imageId, ServerSettings settings)
        {
            //GET	/api/v1/json/images/:image_id
            var emptyTagList = new List<string>();
            var emptySpoilerList = new List<string>();
            var returnResult = new Tuple<List<string>, bool, List<string>>(emptyTagList, false, emptySpoilerList);
            var safeQuery = "id:" + imageId + ", safe";
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/images")
                .SetQueryParams(new { key = _settings.token, q = safeQuery, filter_id = settings.filterId })
                .GetAsync()
                .ReceiveJson();
            if (results.total <= 0)
            {
                return returnResult;
            }

            bool spoiler = results.images[0].spoilered;
            List<long> tagIds = results.images[0].tag_ids;
            List<string> tagNames = results.images[0].tags;
            var spoilerList = await CheckSpoilerListAsync(tagIds, settings);
            var tagStrings = new List<string>();
            foreach (var tagName in tagNames)
            {
                if (!spoilerList.Contains(tagName))
                {
                    tagStrings.Add(tagName);
                }
            }

            if (tagIds.Count != tagNames.Count)
            {
                return returnResult;
            }

            returnResult = new Tuple<List<string>, bool, List<string>>(tagStrings, spoiler, spoilerList);
            return (returnResult);
        }

        public async Task RefreshListsAsync(SocketCommandContext context)
        {
            await GetSpoilerTagsAsync(context);
            await GetHiddenTagsAsync(context);
        }

        private async Task GetSpoilerTagsAsync(SocketCommandContext context)
        {
            //GET	/api/v1/json/filters/user
            var settings = await FileHelper.LoadServerSettings(context);
            var results = await _settings.url
                .AppendPathSegments($"/api/v1/json/filters/{settings.filterId}")
                .SetQueryParams(new { key = _settings.token })
                .GetAsync()
                .ReceiveJson();
            var tagIds = results.filter.spoilered_tag_ids;
            var output = await GetTagNamesAsync(tagIds);
            settings.spoilerList = output;
            await FileHelper.SaveServerSettingsAsync(settings, context);
        }

        private async Task GetHiddenTagsAsync(SocketCommandContext context)
        {
            //GET	/api/v1/json/filters/user
            var settings = await FileHelper.LoadServerSettings(context);
            var hiddenTagResults = await _settings.url
                .AppendPathSegments($"/api/v1/json/filter/{settings.filterId}")
                .SetQueryParams(new { key = _settings.token })
                .GetAsync()
                .ReceiveJson();
            var hiddenTagIds = hiddenTagResults.filter.hidden_tag_ids;
            var output = await GetTagNamesAsync(hiddenTagIds);
            var combinedList = new List<Tuple<long, string>>();
            for (var x = 0; x < hiddenTagIds.Count; x++)
            {
                var combinedTag = new Tuple<long, string>(hiddenTagIds[x], output[x]);
                combinedList.Add(combinedTag);
            }

            //get tag aliases
            var aliasedTagList = new List<Tuple<long, string>>();
            foreach (var (HiddenTagId, _) in combinedList)
            {
                var aliasedTagResults = await _settings.url
                    .AppendPathSegments("/api/v1/json/search/tags")
                    .SetQueryParams(new { q = $"id:{HiddenTagId}" })
                    .GetAsync()
                    .ReceiveJson();
                var combinedTagNames = aliasedTagResults.tags[0].aliases;
                foreach (var combinedTagName in combinedTagNames)
                {
                    var decodedString = WebUtility.UrlDecode(combinedTagName.ToString());
                    var aliasedTagNameResultsName = await _settings.url
                        .AppendPathSegments("/api/v1/json/search/tags")
                        .SetQueryParams(new { q = $"name:{decodedString}" })
                        .GetAsync()
                        .ReceiveJson();
                    var total = aliasedTagNameResultsName.total;
                    long combinedTagId;
                    if (total > 0)
                    {
                        combinedTagId = aliasedTagNameResultsName.tags[0].id;
                        var combinedTag = new Tuple<long, string>(combinedTagId, decodedString.ToString());
                        aliasedTagList.Add(combinedTag);
                    }
                    else
                    {
                        var aliasedTagNameResultsSlug = await _settings.url
                            .AppendPathSegments("/api/v1/json/search/tags")
                            .SetQueryParams(new { q = $"slug:{decodedString}" })
                            .GetAsync()
                            .ReceiveJson();
                        combinedTagId = aliasedTagNameResultsSlug.tags[0].id;
                        var combinedTag = new Tuple<long, string>(combinedTagId, aliasedTagNameResultsSlug.tags[0].name.ToString());
                        aliasedTagList.Add(combinedTag);
                    }
                }
            }

            //get tag implications
            var impliedTagList = new List<Tuple<long, string>>();
            foreach (var (HiddenTagId, _) in combinedList)
            {
                var impliedTagResults = await _settings.url
                    .AppendPathSegments("/api/v1/json/search/tags")
                    .SetQueryParams(new { q = $"id:{HiddenTagId}" })
                    .GetAsync()
                    .ReceiveJson();
                var combinedTagNames = impliedTagResults.tags[0].implied_by_tags;
                foreach (var combinedTagName in combinedTagNames)
                {
                    var decodedString = WebUtility.UrlDecode(combinedTagName.ToString());
                    var impliedTagNameResultsName = await _settings.url
                        .AppendPathSegments("/api/v1/json/search/tags")
                        .SetQueryParams(new { q = $"name:{decodedString}" })
                        .GetAsync()
                        .ReceiveJson();
                    var total = impliedTagNameResultsName.total;
                    long combinedTagId;
                    if (total > 0)
                    {
                        combinedTagId = impliedTagNameResultsName.tags[0].id;
                    }
                    else
                    {
                        var impliedTagNameResultsSlug = await _settings.url
                            .AppendPathSegments("/api/v1/json/search/tags")
                            .SetQueryParams(new { q = $"slug:{combinedTagName}" })
                            .GetAsync()
                            .ReceiveJson();
                        combinedTagId = impliedTagNameResultsSlug.tags[0].id;
                    }

                    string decode = decodedString.ToString();
                    string decolon = decodedString.ToString();
                    if (decode.Contains("-colon-"))
                    {
                        decolon = decode.Replace("-colon-", ":");
                    }

                    var combinedTag = new Tuple<long, string>(combinedTagId, decolon);
                    impliedTagList.Add(combinedTag);
                }
            }

            foreach (var aliasedTag in aliasedTagList)
            {
                combinedList.Add(aliasedTag);
            }

            foreach (var impliedTag in impliedTagList)
            {
                combinedList.Add(impliedTag);
            }

            var outputList = new List<string>();
            foreach (var (_, name) in combinedList)
            {
                outputList.Add(name);
            }

            var dedupedList = outputList.Distinct().ToList();
            settings.redList.list = dedupedList;
            await FileHelper.SaveServerSettingsAsync(settings, context);
        }

        private async Task<List<string>> CheckSpoilerListAsync(List<long> tagIds, ServerSettings settings)
        {
            var tagList = new List<string>();
            foreach (var tagId in tagIds)
            {
                var tagName = await GetTagNameByIdAsync(tagId);
                foreach (var spoilerTagName in settings.spoilerList)
                {
                    if (tagName == spoilerTagName)
                    {
                        tagList.Add(spoilerTagName);
                    }
                }
            }

            return tagList;
        }

        private async Task<List<string>> GetTagNamesAsync(List<object> tagIdList)
        {
            var tagNameList = new List<string>();
            foreach (var tagId in tagIdList)
            {
                var name = await GetTagNameByIdAsync((long)tagId);
                tagNameList.Add(name);
            }

            return tagNameList;
        }

        private async Task<string> GetTagNameByIdAsync(long id)
        {
            //GET	/api/v1/json/search/tags
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/tags")
                .SetQueryParams(new { q = $"id:{id}" })
                .GetAsync()
                .ReceiveJson();
            var output = results.tags[0].name.ToString();
            return output;
        }
    }
}
