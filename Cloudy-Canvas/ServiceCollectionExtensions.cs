namespace Cloudy_Canvas
{
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        /// <summary>Registers everything the bot needs. Kept in one place so a test can build and validate the same container.</summary>
        public static IServiceCollection AddCloudyCanvas(this IServiceCollection services, IConfiguration config, AllPreloadedSettings presettings)
        {
            services.Configure<DiscordSettings>(config.GetSection(nameof(DiscordSettings)));
            services.Configure<ManebooruSettings>(config.GetSection(nameof(ManebooruSettings)));
            services.Configure<LogRetentionSettings>(config.GetSection("LogRetention"));
            services.AddOptions<StorageSettings>()
                .Bind(config.GetSection("Storage"))
                .Validate(settings => !string.IsNullOrWhiteSpace(settings.RootPath), "Storage:RootPath must not be empty.")
                .ValidateOnStart();

            services.AddTransient<IDateTimeService, DateTimeService>();
            services.AddTransient<MixinsService>();
            services.AddBooruClient();
            services.AddSingleton<LoggingService>();
            services.AddSingleton<CooldownService>();
            services.AddSingleton(presettings);
            services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent,
            }));
            services.AddSingleton(new CommandService());

            services.AddHostedService<Worker>();
            services.AddHostedService<LogRetentionService>();
            return services;
        }
    }
}
