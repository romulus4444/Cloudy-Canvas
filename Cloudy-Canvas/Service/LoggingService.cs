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
            if (file)
            {
                await AppendToFileAsync(message, context);
            }

            var logMessage = PrepareMessageForLogging(message, context);
            _logger.LogInformation("{Entry}", logMessage);
        }

        private static string PrepareMessageForLogging(string message, SocketCommandContext context, bool fileEntry = false, bool header = false)
        {
            var logMessage = "";
            if (!header)
            {
                logMessage += $"[{DateTime.UtcNow:s}] ";
            }

            if (context.IsPrivate)
            {
                if (!fileEntry)
                {
                    logMessage += $"DM with @{context.User.Username} ({context.User.Id})";
                }

                if (!fileEntry && !header)
                {
                    logMessage += ", ";
                }

                if (!header)
                {
                    logMessage += message;
                }
            }
            else
            {
                if (!fileEntry)
                {
                    logMessage += $"server: {context.Guild.Name} ({context.Guild.Id}) #{context.Channel.Name} ({context.Channel.Id})";
                }

                if (!fileEntry && !header)
                {
                    logMessage += " ";
                }

                if (!header)
                {
                    logMessage += $"@{context.User.Username} ({context.User.Id}), ";
                    logMessage += message;
                }
            }

            if (fileEntry || header)
            {
                logMessage += Environment.NewLine;
            }

            return logMessage;
        }

        private static async Task AppendToFileAsync(string message, SocketCommandContext context)
        {
            var filepath = FileHelper.SetUpFilepath(FilePathType.Channel, "<date>", "log", context);
            var header = PrepareMessageForLogging(message, context, false, true);
            var entry = PrepareMessageForLogging(message, context, true);
            await LogFile.AppendAsync(filepath, header, entry);
        }
    }
}
