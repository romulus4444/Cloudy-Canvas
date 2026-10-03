namespace Cloudy_Canvas.Tests
{
    using System.Collections.Generic;
    using System.Threading;
    using Cloudy_Canvas.Settings;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    /// <summary>Builds the bot's real dependency injection container from in-memory configuration.</summary>
    internal static class TestServices
    {
        public static ServiceProvider Build(params (string Key, string Value)[] settings)
        {
            var values = new List<KeyValuePair<string, string>> { new("DiscordSettings:token", "not-a-real-token") };
            foreach (var (key, value) in settings)
            {
                values.Add(new KeyValuePair<string, string>(key, value));
            }

            var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IHostApplicationLifetime, StubLifetime>();
            services.AddCloudyCanvas(config, new AllPreloadedSettings());
            return services.BuildServiceProvider();
        }

        private sealed class StubLifetime : IHostApplicationLifetime
        {
            public CancellationToken ApplicationStarted => CancellationToken.None;

            public CancellationToken ApplicationStopping => CancellationToken.None;

            public CancellationToken ApplicationStopped => CancellationToken.None;

            public void StopApplication()
            {
            }
        }
    }
}
