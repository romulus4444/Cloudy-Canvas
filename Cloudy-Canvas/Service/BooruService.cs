namespace Cloudy_Canvas.Service
{
    using System;
    using System.Threading.Tasks;
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

        public async Task<Tuple<long, bool>> GetImageById(long imageid)
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

        public async Task<Tuple<long, bool>> GetRandomImageByQuery(string query)
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

    }
}
