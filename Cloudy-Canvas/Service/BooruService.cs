namespace Cloudy_Canvas.Service
{
    using System;
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

        public async Task<string> GetImageById(int imageid)
        {
            //GET	/api/v1/json/images/:image_id
            var image = await _settings.url
                .AppendPathSegments("api/v1/json/images", imageid)
                .SetQueryParams(new { key = _settings.token })
                .GetAsync()
                .ReceiveJson();
            return image.image.view_url;
        }

        public async Task<long> GetRandomFirstPageImageByQuery(string query)
        {
            //GET	/api/v1/json/search/images?q=safe
            var safequery = query + ", safe";
            var results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/images")
                .SetQueryParams(new { key = _settings.token, q = query, per_page = 1 })
                .GetAsync()
                .ReceiveJson();
            long total = results.total;
            if (total <= 0)
            {
                return -1;
            }

            var page = new Random().Next((int)total) + 1;
            results = await _settings.url
                .AppendPathSegments("/api/v1/json/search/images")
                .SetQueryParams(new { key = _settings.token, q = query, per_page = 1, page })
                .GetAsync()
                .ReceiveJson();
            //var randImageNumber = new Random().Next((int)total);
            return results.images[0].id;
        }
    }
}
