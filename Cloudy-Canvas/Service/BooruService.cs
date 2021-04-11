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

        public async Task<Tuple<long, bool>> GetImageByIdAsync(long imageid)
        {
            //GET	/api/v1/json/images/:image_id
            var spoiler = false;
            long searchResult = -1;
            var returnResult = new Tuple<long, bool>(searchResult, spoiler);
            var safequery = "id:" + imageid + ", safe";
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/images")
                .SetQueryParams(new { key = _settings.token, q = safequery })
                .SetQueryParams(new { key = _settings.token })
                .GetAsync()
                .ReceiveJson();
            if (results.total <= 0)
            {
                return returnResult;
            }

            spoiler = results.images[0].spoilered;
            returnResult = new Tuple<long, bool>(results.images[0].id, spoiler);
            return (returnResult);
        }

        public async Task<Tuple<long, bool>> GetRandomImageByQueryAsync(string query)
        {
            //GET	/api/v1/json/search/images?q=safe
            var spoiler = false;
            long searchResult = -1;
            var returnResult = new Tuple<long, bool>(searchResult, spoiler);
            var safequery = query + ", safe";
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/images")
                .SetQueryParams(new { key = _settings.token, q = query, per_page = 1 })
                .GetAsync()
                .ReceiveJson();
            long total = results.total;
            if (total <= 0)
            {
                return returnResult;
            }

            var page = new Random().Next((int)total) + 1;
            results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/images")
                .SetQueryParams(new { key = _settings.token, q = query, per_page = 1, page })
                .GetAsync()
                .ReceiveJson();

            spoiler = results.images[0].spoilered;
            returnResult = new Tuple<long, bool>(results.images[0].id, spoiler);
            return (returnResult);
        }

        public async Task<List<string>> GetSpoilerTagsAsync()
        {
            //GET	/api/v1/json/filters/user
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/filters/user")
                .SetQueryParams(new { key = _settings.token })
                .GetAsync()
                .ReceiveJson();
            var tagIds = results.filters[0].spoilered_tag_ids;
            var output = await GetTagNamesAsync(tagIds);
            return output;
        }

        public async Task<List<string>> GetTagNamesAsync(List<object> tagIdList)
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

        public async Task<string> GetTagNameByIdAsync(long id)
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
