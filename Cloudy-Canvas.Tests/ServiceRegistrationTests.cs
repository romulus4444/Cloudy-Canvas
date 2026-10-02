namespace Cloudy_Canvas.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;
    using Discord.WebSocket;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Xunit;

    /// <summary>
    /// Dependency injection problems (a missing registration, a constructor asking for something that isn't registered) otherwise
    /// only show up at runtime, the first time a command runs. These tests build the real container and check it.
    /// </summary>
    public class ServiceRegistrationTests
    {
        private sealed class StubLifetime : IHostApplicationLifetime
        {
            public CancellationToken ApplicationStarted => CancellationToken.None;

            public CancellationToken ApplicationStopping => CancellationToken.None;

            public CancellationToken ApplicationStopped => CancellationToken.None;

            public void StopApplication()
            {
            }
        }

        private static ServiceProvider BuildProvider()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>("DiscordSettings:token", "not-a-real-token"),
            }).Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IHostApplicationLifetime, StubLifetime>();
            services.AddCloudyCanvas(config, new AllPreloadedSettings());

            // Validates every registration, including the hosted services, and that no singleton depends on a scoped service.
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        }

        [Fact]
        public void TheContainerIsValid()
        {
            using var provider = BuildProvider();

            Assert.NotEmpty(provider.GetServices<IHostedService>());
        }

        [Fact]
        public void EveryCommandModuleCanBeConstructed()
        {
            using var provider = BuildProvider();
            var modules = typeof(Program).Assembly.GetTypes()
                .Where(t => typeof(ModuleBase<SocketCommandContext>).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            Assert.True(modules.Count >= 5, $"expected the command modules to be found, found {modules.Count}");
            foreach (var module in modules)
            {
                var instance = ActivatorUtilities.CreateInstance(provider, module);
                Assert.NotNull(instance);
            }
        }

        [Fact]
        public void SingletonsAreSharedAndTheDiscordClientIsCreatedOnce()
        {
            using var provider = BuildProvider();

            Assert.Same(provider.GetRequiredService<DiscordSocketClient>(), provider.GetRequiredService<DiscordSocketClient>());
            Assert.Same(provider.GetRequiredService<CommandService>(), provider.GetRequiredService<CommandService>());
            Assert.Same(provider.GetRequiredService<AllPreloadedSettings>(), provider.GetRequiredService<AllPreloadedSettings>());
            Assert.Same(provider.GetRequiredService<CooldownService>(), provider.GetRequiredService<CooldownService>());
        }

        [Fact]
        public void TheBooruServiceGetsItsConfiguredClient()
        {
            using var provider = BuildProvider();

            var booru = provider.GetRequiredService<BooruService>();

            Assert.NotNull(booru);
        }
    }
}
