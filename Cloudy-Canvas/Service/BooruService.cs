namespace Cloudy_Canvas.Service
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Settings;
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

        public async Task<Tuple<long, bool, List<string>>> GetImageByIdAsync(long imageId)
        {
            //GET	/api/v1/json/images/:image_id
            const long searchResult = -1;
            var emptyList = new List<string>();
            var returnResult = new Tuple<long, bool, List<string>>(searchResult, false, emptyList);
            var safeQuery = "id:" + imageId + ", safe";
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/images")
                .SetQueryParams(new { key = _settings.token, q = safeQuery })
                .SetQueryParams(new { key = _settings.token })
                .GetAsync()
                .ReceiveJson();
            if (results.total <= 0)
            {
                return returnResult;
            }

            bool spoiler = results.images[0].spoilered;
            var spoilerList = await GetSpoilerList(results.images[0].tag_ids);
            returnResult = new Tuple<long, bool, List<string>>(results.images[0].id, spoiler, spoilerList);
            return (returnResult);
        }

        public async Task<Tuple<long, long, bool, List<string>>> GetRandomImageByQueryAsync(string query)
        {
            //GET	/api/v1/json/search/images?q=safe
            long searchResult = -1;
            long numberOfResults = 0;
            var emptyList = new List<string>();
            var returnResult = new Tuple<long, long, bool, List<string>>(searchResult, numberOfResults, false, emptyList);
            var safeQuery = query + ", safe";
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/images")
                .SetQueryParams(new { key = _settings.token, q = safeQuery, per_page = 1 })
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
                .SetQueryParams(new { key = _settings.token, q = query, per_page = 1, page })
                .GetAsync()
                .ReceiveJson();
            searchResult = results.images[0].id;
            bool spoiler = results.images[0].spoilered;
            var spoilerList = await GetSpoilerList(results.images[0].tag_ids);
            returnResult = new Tuple<long, long, bool, List<string>>(searchResult, numberOfResults, spoiler, spoilerList);
            return (returnResult);
        }

        public async Task<Tuple<long, long, bool, List<string>>> GetFirstRecentImageByQueryAsync(string query)
        {
            //GET	/api/v1/json/search/images?q=safe
            long searchResult = -1;
            long numberOfResults = 0;
            var emptyList = new List<string>();
            var returnResult = new Tuple<long, long, bool, List<string>>(searchResult, numberOfResults, false, emptyList);
            var safeQuery = query + ", safe";
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/images")
                .SetQueryParams(new { key = _settings.token, q = safeQuery, per_page = 1, sf = "first_seen_at", sd = "desc" })
                .GetAsync()
                .ReceiveJson();
            numberOfResults = results.total;
            if (numberOfResults <= 0)
            {
                return returnResult;
            }

            searchResult = results.images[0].id;
            bool spoiler = results.images[0].spoilered;
            var spoilerList = await GetSpoilerList(results.images[0].tag_ids);
            returnResult = new Tuple<long, long, bool, List<string>>(searchResult, numberOfResults, spoiler, spoilerList);
            return (returnResult);
        }

        public async Task<List<Tuple<long, string>>> GetSpoilerTagsAsync()
        {
            //GET	/api/v1/json/filters/user
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/filters/user")
                .SetQueryParams(new { key = _settings.token })
                .GetAsync()
                .ReceiveJson();
            var tagIds = results.filters[0].spoilered_tag_ids;
            var output = await GetTagNamesAsync(tagIds);
            var combinedList = new List<Tuple<long, string>>();
            for (var x = 0; x < tagIds.Count; x++)
            {
                var combinedTag = new Tuple<long, string>(tagIds[x], output[x]);
                combinedList.Add(combinedTag);
            }

            await FileHelper.WriteSpoilerListToFileAsync(combinedList);
            return combinedList;
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

        public async Task<Tuple<List<string>, bool, List<string>>> GetImageTagsIdAsync(long imageId)
        {
            //GET	/api/v1/json/images/:image_id
            var emptyTagList = new List<string>();
            var emptySpoilerList = new List<string>();
            var returnResult = new Tuple<List<string>, bool, List<string>>(emptyTagList, false, emptySpoilerList);
            var safeQuery = "id:" + imageId + ", safe";
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/images")
                .SetQueryParams(new { key = _settings.token, q = safeQuery })
                .SetQueryParams(new { key = _settings.token })
                .GetAsync()
                .ReceiveJson();
            if (results.total <= 0)
            {
                return returnResult;
            }

            bool spoiler = results.images[0].spoilered;
            List<object> tagIds = results.images[0].tag_ids;
            List<object> tagNames = results.images[0].tags;
            var spoilerList = await GetSpoilerList(tagIds);
            var tagStrings = new List<string>();
            for (var x = 0; x < tagNames.Count; x++)
            {
                if (!spoilerList.Contains(tagNames[x].ToString()))
                {
                    tagStrings.Add(tagNames[x].ToString());
                }
            }

            if (tagIds.Count != tagNames.Count)
            {
                return returnResult;
            }

            returnResult = new Tuple<List<string>, bool, List<string>>(tagStrings, spoiler, spoilerList);
            return (returnResult);
        }

        private static async Task<List<string>> GetSpoilerList(List<object> tagIds)
        {
            var tagList = new List<string>();
            var spoilerTagIdList = await FileHelper.GetSpoilerTagIdListFromFileAsync();
            foreach (var tagId in tagIds)
            {
                foreach (var (spoilerTagId, spoilerTagName) in spoilerTagIdList)
                {
                    if (spoilerTagId == (long)tagId)
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
                var results = await _settings.url
                    .AppendPathSegments("/api/v1/json/filters/user")
                    .SetQueryParams(new { key = _settings.token })
                    .GetAsync()
                    .ReceiveJson();
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
