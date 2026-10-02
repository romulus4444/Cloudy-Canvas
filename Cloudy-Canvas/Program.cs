namespace Cloudy_Canvas
{
    using System;
    using Cloudy_Canvas.Helpers;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using Serilog.Events;

    public class Program
    {
        public static int Main(string[] args)
        {
            // The HttpClient request log would print the full URL, which includes the Manebooru API key for image searches.
            Log.Logger = new LoggerConfiguration().MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning).Enrich.FromLogContext().WriteTo
                .Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}").CreateLogger();

            try
            {
                Log.Information("Starting up");
                CreateHostBuilder(args).Build().Run();

                // Run() returns normally even when the worker failed; the worker records that in Environment.ExitCode.
                return Environment.ExitCode;
            }
            catch (Exception ex)
            {
                // A non-zero exit code lets systemd / the Windows service manager see the failure (and restart on it).
                Log.Fatal(ex, "Application terminated unexpectedly");
                return 1;
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
                var presettings = FileHelper.LoadAllPresettingsAsync().GetAwaiter().GetResult();
                services.AddCloudyCanvas(hostContext.Configuration, presettings);
            });
    }
}
