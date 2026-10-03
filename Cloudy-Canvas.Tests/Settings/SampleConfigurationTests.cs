namespace Cloudy_Canvas.Tests.Settings
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using Cloudy_Canvas.Settings;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Xunit;

    /// <summary>
    /// appsettings.sample.json is what people copy to configure the bot. A key that doesn't match a setting is silently ignored by the
    /// configuration system, so these tests catch typos and keep the sample in step with the settings classes.
    /// </summary>
    public class SampleConfigurationTests
    {
        private static string SamplePath => Path.Combine(AppContext.BaseDirectory, "appsettings.sample.json");

        private static IConfiguration Load()
        {
            return new ConfigurationBuilder().AddJsonFile(SamplePath, optional: false).Build();
        }

        [Fact]
        public void TheSampleBindsToTheRealSettingsWithSafeDefaults()
        {
            var config = Load();

            var discord = config.GetSection("DiscordSettings").Get<DiscordSettings>();
            Assert.Equal(string.Empty, discord.token);
            Assert.Empty(discord.BroadcastUserIds);
            Assert.Null(discord.PrefixOverride); // "" means no override

            Assert.Equal("botsettings", config.GetSection("Storage").Get<StorageSettings>().RootPath);
            Assert.Equal("https://manebooru.art/", config.GetSection("ManebooruSettings").Get<ManebooruSettings>().url);
            Assert.Equal(0, config.GetSection("LogRetention").Get<LogRetentionSettings>().RetentionDays);
        }

        [Fact]
        public void EveryKeyInTheSampleIsARealSetting()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(SamplePath));
            var known = new (string Section, Type Settings)[]
            {
                ("DiscordSettings", typeof(DiscordSettings)),
                ("Storage", typeof(StorageSettings)),
                ("ManebooruSettings", typeof(ManebooruSettings)),
                ("LogRetention", typeof(LogRetentionSettings)),
            };

            foreach (var section in document.RootElement.EnumerateObject())
            {
                var match = known.SingleOrDefault(k => k.Section == section.Name);
                Assert.True(match.Settings != null, $"'{section.Name}' in the sample is not a configuration section the bot reads");

                var properties = match.Settings.GetProperties().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var key in section.Value.EnumerateObject())
                {
                    Assert.True(properties.Contains(key.Name), $"'{section.Name}:{key.Name}' in the sample is not a property of {match.Settings.Name}");
                }
            }
        }

        [Fact]
        public void TheSampleSurvivesTheBotsOwnValidation()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IHostApplicationLifetime, NoLifetime>();
            services.AddCloudyCanvas(Load(), new AllPreloadedSettings());
            using var provider = services.BuildServiceProvider();

            Assert.Equal("botsettings", provider.GetRequiredService<IOptions<StorageSettings>>().Value.RootPath);
            Assert.Null(provider.GetRequiredService<IOptions<DiscordSettings>>().Value.PrefixOverride);
        }

        private sealed class NoLifetime : IHostApplicationLifetime
        {
            public System.Threading.CancellationToken ApplicationStarted => default;

            public System.Threading.CancellationToken ApplicationStopping => default;

            public System.Threading.CancellationToken ApplicationStopped => default;

            public void StopApplication()
            {
            }
        }
    }
}
