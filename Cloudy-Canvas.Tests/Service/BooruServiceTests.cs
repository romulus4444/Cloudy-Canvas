namespace Cloudy_Canvas.Tests.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Xunit;

    public class BooruServiceTests
    {
        private const string ImageJson = @"{ ""images"": [ { ""id"": 123, ""spoilered"": true, ""tag_ids"": [1, 2, 3], ""tags"": [""safe"", ""oc"", ""scandal""] } ], ""total"": 57 }";
        private const string NoImagesJson = @"{ ""images"": [], ""total"": 0 }";

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly object _lock = new();

            public List<Uri> Requests { get; } = new();

            public Func<Uri, HttpResponseMessage> Respond { get; set; } = _ => Json("{}");

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                lock (_lock)
                {
                    Requests.Add(request.RequestUri);
                }

                return Task.FromResult(Respond(request.RequestUri));
            }
        }

        private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK, string mediaType = "application/json")
        {
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) };
        }

        private static (BooruService Service, FakeHandler Handler) Create(string token = "secret-key")
        {
            var handler = new FakeHandler();
            var service = new BooruService(
                new HttpClient(handler) { BaseAddress = new Uri("https://booru.test/") },
                Options.Create(new ManebooruSettings { token = token }),
                NullLogger<BooruService>.Instance);
            return (service, handler);
        }

        private static ServerSettings Settings(bool safeMode = true, params SpoilerTag[] spoilers)
        {
            var settings = new ServerSettings { SafeMode = safeMode };
            settings.SpoilerList.AddRange(spoilers);
            return settings;
        }

        private static System.Collections.Specialized.NameValueCollection Query(Uri uri) => HttpUtility.ParseQueryString(uri.Query);

        // ---- image lookups -------------------------------------------------------------------------------------------

        [Fact]
        public async Task ImageByIdMapsTheResponse()
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json(ImageJson);

            var result = await service.GetImageByIdAsync(123, Settings(true, new SpoilerTag(3, "scandal"), new SpoilerTag(1, "safe")), 175);

            Assert.Equal(BooruStatus.Ok, result.Status);
            Assert.False(result.Failed);
            Assert.Equal(123, result.ImageId);
            Assert.Equal(57, result.Total);
            Assert.True(result.Spoilered);
            Assert.Equal(new[] { "safe", "scandal" }, result.SpoilerTags); // in the order the image lists its tags
        }

        [Fact]
        public async Task ImageSearchRequestHasTheExpectedShape()
        {
            var (service, handler) = Create("secret-key");
            handler.Respond = _ => Json(ImageJson);

            await service.GetImageByIdAsync(123, Settings(), 175);

            var uri = Assert.Single(handler.Requests);
            Assert.Equal("/api/v1/json/search/images", uri.AbsolutePath);
            var query = Query(uri);
            Assert.Equal("(id:123), safe", query["q"]); // safe mode wraps the query
            Assert.Equal("175", query["filter_id"]);
            Assert.Equal("1", query["per_page"]);
            Assert.Equal("secret-key", query["key"]);
        }

        [Fact]
        public async Task SafeModeOffSendsTheQueryUnchanged()
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json(ImageJson);

            await service.GetRandomImageByQueryAsync("a || b", Settings(false), 175);

            Assert.Equal("a || b", Query(handler.Requests.Single())["q"]);
        }

        [Fact]
        public async Task NoApiKeyIsSentWhenNoneIsConfigured()
        {
            var (service, handler) = Create(token: "");
            handler.Respond = _ => Json(ImageJson);

            await service.GetImageByIdAsync(1, Settings(), 175);

            Assert.Null(Query(handler.Requests.Single())["key"]);
        }

        [Fact]
        public async Task RandomAndRecentUseTheirSortOrders()
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json(ImageJson);

            await service.GetRandomImageByQueryAsync("cute", Settings(), 175);
            await service.GetFirstRecentImageByQueryAsync("cute", Settings(), 175);

            var random = Query(handler.Requests[0]);
            Assert.Equal("random", random["sf"]);
            Assert.Null(random["sd"]);
            var recent = Query(handler.Requests[1]);
            Assert.Equal("first_seen_at", recent["sf"]);
            Assert.Equal("desc", recent["sd"]);
        }

        [Fact]
        public async Task QueriesAreEncodedSoSpecialCharactersSurvive()
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json(ImageJson);
            const string query = "a&b=c, \"x y\" +\u00e9#, 100% ?";

            await service.GetRandomImageByQueryAsync(query, Settings(false), 175);

            Assert.Equal(query, Query(handler.Requests.Single())["q"]);
        }

        [Fact]
        public async Task NoMatchesIsNotFoundNotAnError()
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json(NoImagesJson);

            var result = await service.GetRandomImageByQueryAsync("nothing", Settings(), 175);

            Assert.Equal(BooruStatus.NotFound, result.Status);
            Assert.False(result.Failed);
            Assert.Equal(0, result.Total);
        }

        [Theory]
        [InlineData(HttpStatusCode.NotFound, 404)]
        [InlineData(HttpStatusCode.BadRequest, 400)]
        [InlineData(HttpStatusCode.TooManyRequests, 429)]
        [InlineData(HttpStatusCode.InternalServerError, 500)]
        [InlineData(HttpStatusCode.ServiceUnavailable, 503)]
        public async Task HttpErrorsReportTheirStatusCode(HttpStatusCode status, int code)
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json("{}", status);

            var result = await service.GetRandomImageByQueryAsync("x", Settings(), 175);

            Assert.Equal(BooruStatus.HttpError, result.Status);
            Assert.Equal(code, result.HttpCode);
            Assert.True(result.Failed);
        }

        public static IEnumerable<object[]> BrokenResponses()
        {
            yield return new object[] { new Func<Uri, HttpResponseMessage>(_ => throw new HttpRequestException("connection refused")) };
            yield return new object[] { new Func<Uri, HttpResponseMessage>(_ => throw new TaskCanceledException("timed out")) };
            yield return new object[] { new Func<Uri, HttpResponseMessage>(_ => throw new TimeoutException()) };
            yield return new object[] { new Func<Uri, HttpResponseMessage>(_ => Json("this is not json")) };
            yield return new object[] { new Func<Uri, HttpResponseMessage>(_ => Json("{ \"images\": [ { \"id\": \"not a number\" } ] }")) };
            yield return new object[] { new Func<Uri, HttpResponseMessage>(_ => Json("null")) };
            yield return new object[] { new Func<Uri, HttpResponseMessage>(_ => Json("<html>Bad gateway</html>", HttpStatusCode.OK, "text/html")) };
        }

        [Theory]
        [MemberData(nameof(BrokenResponses))]
        public async Task NetworkFailuresAndGarbageAreUnavailableNeverNoResults(Func<Uri, HttpResponseMessage> respond)
        {
            var (service, handler) = Create();
            handler.Respond = respond;

            var result = await service.GetRandomImageByQueryAsync("x", Settings(), 175);

            Assert.Equal(BooruStatus.Unavailable, result.Status);
            Assert.True(result.Failed);
            Assert.Null(result.HttpCode);
        }

        [Fact]
        public async Task NullCollectionsInTheResponseAreTolerated()
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json(@"{ ""images"": [ { ""id"": 5, ""spoilered"": false, ""tag_ids"": null, ""tags"": null } ], ""total"": 1 }");

            var result = await service.GetImageByIdAsync(5, Settings(), 175);

            Assert.Equal(BooruStatus.Ok, result.Status);
            Assert.Empty(result.SpoilerTags);
        }

        // ---- featured ------------------------------------------------------------------------------------------------

        [Fact]
        public async Task FeaturedLooksUpTheFeaturedImageThroughTheFilter()
        {
            var (service, handler) = Create();
            handler.Respond = uri => uri.AbsolutePath.EndsWith("/images/featured") ? Json(@"{ ""image"": { ""id"": 9 } }") : Json(ImageJson);

            var result = await service.GetFeaturedImageIdAsync(Settings(), 56027);

            Assert.Equal(BooruStatus.Ok, result.Status);
            Assert.Equal(2, handler.Requests.Count);
            Assert.Null(Query(handler.Requests[0])["key"]);
            var search = Query(handler.Requests[1]);
            Assert.Equal("(id:9), safe", search["q"]);
            Assert.Equal("56027", search["filter_id"]);
        }

        [Fact]
        public async Task FeaturedImageHiddenByTheFilterIsNotFound()
        {
            var (service, handler) = Create();
            handler.Respond = uri => uri.AbsolutePath.EndsWith("/images/featured") ? Json(@"{ ""image"": { ""id"": 9 } }") : Json(NoImagesJson);

            Assert.Equal(BooruStatus.NotFound, (await service.GetFeaturedImageIdAsync(Settings(), 175)).Status);
        }

        [Fact]
        public async Task FeaturedEndpointFailureIsReported()
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json("{}", HttpStatusCode.BadGateway);

            var result = await service.GetFeaturedImageIdAsync(Settings(), 175);

            Assert.Equal(BooruStatus.HttpError, result.Status);
            Assert.Equal(502, result.HttpCode);
            Assert.Single(handler.Requests);
        }

        // ---- tags ----------------------------------------------------------------------------------------------------

        [Fact]
        public async Task TagLookupSeparatesSpoileredTags()
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json(ImageJson);

            var result = await service.GetImageTagsIdAsync(123, Settings(true, new SpoilerTag(3, "scandal")), 175);

            Assert.Equal(BooruStatus.Ok, result.Status);
            Assert.True(result.Spoilered);
            Assert.Equal(new[] { "safe", "oc" }, result.Tags);
            Assert.Equal(new[] { "scandal" }, result.SpoilerTags);
        }

        [Fact]
        public async Task TagLookupWithMismatchedTagListsIsNotFound()
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json(@"{ ""images"": [ { ""id"": 1, ""spoilered"": false, ""tag_ids"": [1, 2], ""tags"": [""safe""] } ], ""total"": 1 }");

            Assert.Equal(BooruStatus.NotFound, (await service.GetImageTagsIdAsync(1, Settings(), 175)).Status);
        }

        // ---- filters -------------------------------------------------------------------------------------------------

        [Fact]
        public async Task CheckFilterReturnsTheFilterId()
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json(@"{ ""filter"": { ""id"": 175 } }");

            var result = await service.CheckFilterAsync(175);

            Assert.Equal(BooruStatus.Ok, result.Status);
            Assert.Equal(175, result.FilterId);
            Assert.Equal("/api/v1/json/filters/175", handler.Requests.Single().AbsolutePath);
        }

        [Theory]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.Forbidden)]
        public async Task UnknownOrPrivateFiltersAreNotFound(HttpStatusCode status)
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json("{}", status);

            Assert.Equal(BooruStatus.NotFound, (await service.CheckFilterAsync(99999999)).Status);
        }

        [Fact]
        public async Task ASiteErrorWhileCheckingAFilterIsNotReportedAsAnInvalidFilter()
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json("{}", HttpStatusCode.InternalServerError);

            var result = await service.CheckFilterAsync(175);

            Assert.Equal(BooruStatus.HttpError, result.Status);
            Assert.Equal(500, result.HttpCode);
        }

        [Fact]
        public async Task AnUnreachableSiteWhileCheckingAFilterIsUnavailable()
        {
            var (service, handler) = Create();
            handler.Respond = _ => throw new HttpRequestException("down");

            Assert.Equal(BooruStatus.Unavailable, (await service.CheckFilterAsync(175)).Status);
        }

        // ---- spoiler list --------------------------------------------------------------------------------------------

        private static string FilterJson(IEnumerable<long> ids) => $@"{{ ""filter"": {{ ""id"": 175, ""spoilered_tag_ids"": [{string.Join(",", ids)}] }} }}";

        private static IEnumerable<long> IdsInTagQuery(Uri uri) =>
            Query(uri)["q"].Split(" || ").Select(part => long.Parse(part["id:".Length..]));

        private static HttpResponseMessage TagsResponse(Uri uri, ISet<long> missing = null)
        {
            var tags = IdsInTagQuery(uri).Where(id => missing == null || !missing.Contains(id)).Select(id => $@"{{ ""id"": {id}, ""name"": ""tag-{id}"" }}");
            return Json($@"{{ ""tags"": [{string.Join(",", tags)}], ""total"": 0 }}");
        }

        [Fact]
        public async Task SpoilerTagNamesAreFetchedInBatchesOfFifty()
        {
            var (service, handler) = Create();
            var ids = Enumerable.Range(1000, 120).Select(i => (long)i).ToList();
            handler.Respond = uri => uri.AbsolutePath.Contains("/filters/") ? Json(FilterJson(ids)) : TagsResponse(uri);

            var result = await service.FetchSpoilerTagsAsync(175);

            Assert.Equal(BooruStatus.Ok, result.Status);
            Assert.Equal(ids, result.Tags.Select(t => t.Id)); // same order as the filter lists them
            Assert.All(result.Tags, tag => Assert.Equal($"tag-{tag.Id}", tag.Name));

            var tagRequests = handler.Requests.Where(u => u.AbsolutePath.EndsWith("/search/tags")).ToList();
            Assert.Equal(3, tagRequests.Count); // 50 + 50 + 20, not 120 requests
            Assert.Equal(new[] { 50, 50, 20 }, tagRequests.Select(u => IdsInTagQuery(u).Count()).OrderByDescending(n => n));
            Assert.All(tagRequests, u => Assert.Equal("50", Query(u)["per_page"]));
        }

        [Fact]
        public async Task AFilterWithoutSpoileredTagsMakesNoTagRequests()
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json(FilterJson(Array.Empty<long>()));

            var result = await service.FetchSpoilerTagsAsync(175);

            Assert.Equal(BooruStatus.Ok, result.Status);
            Assert.Empty(result.Tags);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task DuplicateTagIdsAreCollapsed()
        {
            var (service, handler) = Create();
            handler.Respond = uri => uri.AbsolutePath.Contains("/filters/") ? Json(FilterJson(new long[] { 5, 6, 5 })) : TagsResponse(uri);

            var result = await service.FetchSpoilerTagsAsync(175);

            Assert.Equal(new long[] { 5, 6 }, result.Tags.Select(t => t.Id));
        }

        [Fact]
        public async Task ADeletedTagKeepsAPlaceholderSoImagesAreStillSpoilered()
        {
            var (service, handler) = Create();
            handler.Respond = uri => uri.AbsolutePath.Contains("/filters/") ? Json(FilterJson(new long[] { 1, 2, 3 })) : TagsResponse(uri, new HashSet<long> { 2 });

            var result = await service.FetchSpoilerTagsAsync(175);

            Assert.Equal(BooruStatus.Ok, result.Status);
            Assert.Equal(new[] { "tag-1", "tag #2", "tag-3" }, result.Tags.Select(t => t.Name));
        }

        [Fact]
        public async Task AFailedBatchFailsTheWholeFetchInsteadOfReturningAPartialList()
        {
            var (service, handler) = Create();
            var ids = Enumerable.Range(1, 120).Select(i => (long)i).ToList();
            handler.Respond = uri =>
            {
                if (uri.AbsolutePath.Contains("/filters/"))
                {
                    return Json(FilterJson(ids));
                }

                return IdsInTagQuery(uri).Contains(75) ? Json("{}", HttpStatusCode.InternalServerError) : TagsResponse(uri);
            };

            var result = await service.FetchSpoilerTagsAsync(175);

            Assert.Equal(BooruStatus.HttpError, result.Status);
            Assert.Equal(500, result.HttpCode);
            Assert.Empty(result.Tags);
        }

        [Fact]
        public async Task AnUnknownFilterCannotBeFetched()
        {
            var (service, handler) = Create();
            handler.Respond = _ => Json("{}", HttpStatusCode.NotFound);

            var result = await service.FetchSpoilerTagsAsync(1);

            Assert.Equal(BooruStatus.HttpError, result.Status);
            Assert.Equal(404, result.HttpCode);
        }

        [Fact]
        public void SpoilerTagsAreReportedInTheOrderOfTheImagesTags()
        {
            var settings = Settings(true, new SpoilerTag(30, "c"), new SpoilerTag(10, "a"), new SpoilerTag(20, "b"));

            Assert.Equal(new[] { "b", "c", "a" }, BooruService.FindSpoilerTags(new long[] { 20, 99, 30, 10 }, settings));
        }

        [Fact]
        public void TheFirstEntryWinsWhenASpoilerIdIsListedTwice()
        {
            var settings = Settings(true, new SpoilerTag(1, "first"), new SpoilerTag(1, "second"));

            Assert.Equal(new[] { "first" }, BooruService.FindSpoilerTags(new long[] { 1 }, settings));
        }

        [Fact]
        public void NoTagsMeansNoSpoilers()
        {
            Assert.Empty(BooruService.FindSpoilerTags(null, Settings(true, new SpoilerTag(1, "x"))));
            Assert.Empty(BooruService.FindSpoilerTags(Array.Empty<long>(), Settings()));
        }

        // ---- the real HttpClient pipeline ----------------------------------------------------------------------------

        private static BooruService CreateWithPipeline(FakeHandler handler)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.Configure<ManebooruSettings>(o =>
            {
                o.url = "https://booru.test"; // no trailing slash on purpose
                o.token = "k";
            });
            services.AddBooruClient(o =>
            {
                o.Retry.Delay = TimeSpan.Zero;
                o.Retry.UseJitter = false;
            }).ConfigurePrimaryHttpMessageHandler(() => handler);
            return services.BuildServiceProvider().GetRequiredService<BooruService>();
        }

        [Fact]
        public async Task ATransientServerErrorIsRetried()
        {
            var handler = new FakeHandler();
            var calls = 0;
            handler.Respond = _ => Interlocked.Increment(ref calls) == 1 ? Json("{}", HttpStatusCode.ServiceUnavailable) : Json(ImageJson);
            var service = CreateWithPipeline(handler);

            var result = await service.GetImageByIdAsync(123, Settings(), 175);

            Assert.Equal(BooruStatus.Ok, result.Status);
            Assert.Equal(2, handler.Requests.Count);
            Assert.All(handler.Requests, u => Assert.Equal("booru.test", u.Host));
        }

        [Fact]
        public async Task AServerThatKeepsFailingIsReportedAfterTheRetries()
        {
            var handler = new FakeHandler { Respond = _ => Json("{}", HttpStatusCode.ServiceUnavailable) };
            var service = CreateWithPipeline(handler);

            var result = await service.GetImageByIdAsync(123, Settings(), 175);

            Assert.Equal(BooruStatus.HttpError, result.Status);
            Assert.Equal(503, result.HttpCode);
            Assert.Equal(3, handler.Requests.Count); // the first try and two retries
        }

        [Fact]
        public async Task ANotFoundAnswerIsNotRetried()
        {
            var handler = new FakeHandler { Respond = _ => Json("{}", HttpStatusCode.NotFound) };
            var service = CreateWithPipeline(handler);

            await service.CheckFilterAsync(1);

            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task ConnectionFailuresThatKeepHappeningAreUnavailable()
        {
            var handler = new FakeHandler { Respond = _ => throw new HttpRequestException("no route to host") };
            var service = CreateWithPipeline(handler);

            var result = await service.GetImageByIdAsync(123, Settings(), 175);

            Assert.Equal(BooruStatus.Unavailable, result.Status);
        }

        [Fact]
        public async Task TheClientSendsAnIdentifyingUserAgent()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.Configure<ManebooruSettings>(_ => { });
            string userAgent = null;
            var handler = new FakeHandler();
            handler.Respond = _ => Json(ImageJson);
            services.AddBooruClient().ConfigurePrimaryHttpMessageHandler(() => new UserAgentCapture(handler, ua => userAgent = ua));

            await services.BuildServiceProvider().GetRequiredService<BooruService>().GetImageByIdAsync(1, Settings(), 175);

            Assert.StartsWith("CloudyCanvas/", userAgent);
            Assert.Equal("manebooru.art", handler.Requests.Single().Host); // the default URL applies when none is configured
        }

        private sealed class UserAgentCapture : DelegatingHandler
        {
            private readonly Action<string> _capture;

            public UserAgentCapture(HttpMessageHandler inner, Action<string> capture)
                : base(inner)
            {
                _capture = capture;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _capture(request.Headers.UserAgent.ToString());
                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}
