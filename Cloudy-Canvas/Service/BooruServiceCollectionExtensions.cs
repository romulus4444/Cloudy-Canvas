namespace Cloudy_Canvas.Service
{
    using System;
    using System.Reflection;
    using Cloudy_Canvas.Settings;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Http.Resilience;
    using Microsoft.Extensions.Options;

    public static class BooruServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="BooruService"/> with its own HttpClient: base address from <see cref="ManebooruSettings"/>, an identifying
        /// User-Agent, and the standard resilience pipeline (per-attempt and overall timeouts, retries with backoff for transient
        /// failures, circuit breaker).
        /// </summary>
        public static IHttpClientBuilder AddBooruClient(this IServiceCollection services, Action<HttpStandardResilienceOptions> configureResilience = null)
        {
            var builder = services.AddHttpClient<BooruService>((provider, client) =>
            {
                var settings = provider.GetRequiredService<IOptions<ManebooruSettings>>().Value;
                var url = string.IsNullOrWhiteSpace(settings.url) ? new ManebooruSettings().url : settings.url;
                client.BaseAddress = new Uri(url.EndsWith('/') ? url : url + "/");

                var version = typeof(BooruService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0";
                client.DefaultRequestHeaders.UserAgent.ParseAdd($"CloudyCanvas/{version.Split('+')[0]} (+https://github.com/romulus4444/Cloudy-Canvas)");
            });

            builder.AddStandardResilienceHandler(options =>
            {
                // A Discord user is waiting on the answer, so fail reasonably fast: two quick retries, 10 s per attempt, 30 s overall.
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay = TimeSpan.FromMilliseconds(500);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                configureResilience?.Invoke(options);
            });

            return builder;
        }
    }
}
