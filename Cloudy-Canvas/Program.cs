namespace Cloudy_Canvas
{
    using System;
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;

    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo
                .Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}").CreateLogger();

            try
            {
                Log.Information("Starting up");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).UseSerilog().UseWindowsService().ConfigureAppConfiguration((hostContext, builder) =>
            {
                if (hostContext.HostingEnvironment.IsDevelopment())
                {
                    builder.AddUserSecrets<Program>();
                }
            }).ConfigureServices((hostContext, services) =>
            {
                var config = hostContext.Configuration;
                services.Configure<DiscordSettings>(config.GetSection(nameof(DiscordSettings)));
                services.Configure<ManebooruSettings>(config.GetSection(nameof(ManebooruSettings)));
                services.AddTransient<BooruService>();
                services.AddTransient<Blacklist.BlacklistService>();
                services.AddSingleton<LoggingService>();
                services.AddSingleton(services);

                services.AddHostedService<Worker>();
            });
    }
}
