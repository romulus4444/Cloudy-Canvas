namespace Cloudy_Canvas.Service
{
    using System;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Discord.Commands;
    using Microsoft.Extensions.Logging;

    public class LoggingService
    {
        private readonly ILogger<LoggingService> _logger;

        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger;
        }

        public async Task Log(string message, SocketCommandContext context, bool file = false)
        {
            var source = SourceOf(context);
            if (file)
            {
                await AppendToFileAsync(message, source, context);
            }

            _logger.LogInformation("{Entry}", LogLine.Format(source, message, DateTime.UtcNow));
        }

        private static LogSource SourceOf(SocketCommandContext context)
        {
            return context.IsPrivate
                ? new LogSource(true, context.User.Username, context.User.Id, string.Empty, 0, string.Empty, 0)
                : new LogSource(false, context.User.Username, context.User.Id, context.Guild.Name, context.Guild.Id, context.Channel.Name, context.Channel.Id);
        }

        private static async Task AppendToFileAsync(string message, LogSource source, SocketCommandContext context)
        {
            var filepath = FileHelper.SetUpFilepath(FilePathType.Channel, "<date>", "log", context);
            var now = DateTime.UtcNow;
            var header = LogLine.Format(source, message, now, false, true);
            var entry = LogLine.Format(source, message, now, true);
            await LogFile.AppendAsync(filepath, header, entry);
        }
    }
}
