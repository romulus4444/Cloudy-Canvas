namespace Cloudy_Canvas.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            var spoilerList = CheckSpoilerList(results.images[0].tag_ids, settings);
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
            var spoilerList = CheckSpoilerList(results.images[0].tag_ids, settings);
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
            var spoilerList = CheckSpoilerList(results.images[0].tag_ids, settings);
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
            List<object> tagIds = results.images[0].tag_ids;
            List<object> tagNames = results.images[0].tags;
            var spoilerList = CheckSpoilerList(tagIds, settings);
            var tagStrings = new List<string>();
            foreach (var tagName in tagNames)
            {
                if (!spoilerList.Contains(tagName.ToString()))
                {
                    tagStrings.Add(tagName.ToString());
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

        private static List<string> CheckSpoilerList(List<object> tagIds, ServerSettings settings)
        {
            var tagList = new List<string>();
            foreach (long tagId in tagIds)
            {
                foreach (var (spoilerTagId, spoilerTagName) in settings.spoilerList)
                {
                    if (tagId == spoilerTagId)
                    {
                        tagList.Add(spoilerTagName);
                    }
                }
            }

            return tagList;
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

        private async Task GetSpoilerTagsAsync(SocketCommandContext context)
        {
            //GET	/api/v1/json/filters/user
            var settings = await FileHelper.LoadServerSettings(context);
            var results = await _settings.url
                .AppendPathSegments($"/api/v1/json/filters/{settings.filterId}")
                .GetAsync()
                .ReceiveJson();
            var tagIds = results.filter.spoilered_tag_ids;
            var tagNames = await GetTagNamesAsync(tagIds);
            var combinedTags = new List<Tuple<long, string>>();
            for (var x = 0; x < tagIds.Count; x++)
            {
                var newItem = new Tuple<long, string>((long)tagIds[x], tagNames[x].ToString());
                combinedTags.Add(newItem);
            }

            settings.spoilerList = combinedTags;
            await FileHelper.SaveServerSettingsAsync(settings, context);
        }

        private async Task GetHiddenTagsAsync(SocketCommandContext context)
        {
            //GET	/api/v1/json/filters/user
            var settings = await FileHelper.LoadServerSettings(context);
            var hiddenTagResults = await _settings.url
                .AppendPathSegments($"/api/v1/json/filters/{settings.filterId}")
                .GetAsync()
                .ReceiveJson();
            var hiddenTagIds = hiddenTagResults.filter.hidden_tag_ids;
            var tagIds = new List<object>();
            foreach (var hiddenTagId in hiddenTagIds)
            {
                tagIds.Add(hiddenTagId);
                var furtherTagResults = await _settings.url
                    .AppendPathSegments("/api/v1/json/search/tags")
                    .SetQueryParams(new { q = $"id:{hiddenTagId}" })
                    .GetAsync()
                    .ReceiveJson();
                var aliasedTagSlugs = furtherTagResults.tags[0].aliases;
                var impliedTagSlugs = furtherTagResults.tags[0].implied_by_tags;

                foreach (var aliasedTagSlug in aliasedTagSlugs)
                {
                    string decode = aliasedTagSlug.ToString();
                    decode = decode.Replace("%", "%25");
                    var aliasedTagResults = await _settings.url
                        .AppendPathSegments($"/api/v1/json/tags/{decode}")
                        .GetAsync()
                        .ReceiveJson();
                    var aliasedTagId = aliasedTagResults.tag.id;
                    tagIds.Add(aliasedTagId);
                }

                foreach (var impliedTagSlug in impliedTagSlugs)
                {
                    string decode = impliedTagSlug.ToString();
                    decode = decode.Replace("%", "%25");
                    var impliedTagResults = await _settings.url
                        .AppendPathSegments($"/api/v1/json/tags/{decode}")
                        .GetAsync()
                        .ReceiveJson();
                    var impliedTagId = impliedTagResults.tag.id;
                    tagIds.Add(impliedTagId);
                }
            }

            var combinedList = new List<Tuple<long, string>>();
            var tagNames = await GetTagNamesAsync(tagIds);
            for (var x = 0; x < tagIds.Count; x++)
            {
                var combinedTag = new Tuple<long, string>((long)tagIds[x], tagNames[x]);
                combinedList.Add(combinedTag);
            }

            var dedupedList = combinedList.Distinct().ToList();
            settings.redList = dedupedList;
            await FileHelper.SaveServerSettingsAsync(settings, context);
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
    }
}
