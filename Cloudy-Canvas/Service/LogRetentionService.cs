namespace Cloudy_Canvas.Service
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Settings;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>Periodically deletes old per-channel log files when LogRetention:RetentionDays is set.</summary>
    public class LogRetentionService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        private readonly ILogger<LogRetentionService> _logger;
        private readonly LogRetentionSettings _settings;

        public LogRetentionService(ILogger<LogRetentionService> logger, IOptions<LogRetentionSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_settings.RetentionDays <= 0)
            {
                return;
            }

            _logger.LogInformation("Deleting log files older than {Days} days", _settings.RetentionDays);
            var serversDirectory = Path.Combine(DevSettings.RootPath, "servers");
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var deleted = LogCleaner.DeleteOldLogs(serversDirectory, _settings.RetentionDays, DateTime.UtcNow);
                    if (deleted > 0)
                    {
                        _logger.LogInformation("Deleted {Count} log files older than {Days} days", deleted, _settings.RetentionDays);
                    }

                    await Task.Delay(Interval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }
    }
}
