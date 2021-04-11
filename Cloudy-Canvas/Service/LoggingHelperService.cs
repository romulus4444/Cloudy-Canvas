namespace Cloudy_Canvas.Service
{
    using System.Threading.Tasks;
    using Discord.Commands;
    using Microsoft.Extensions.Logging;

    public class LoggingHelperService
    {
        private readonly ILogger<Worker> _logger;

        public LoggingHelperService(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public string SetUpLogStringPrefix(SocketCommandContext context)
        {
            var prefixString = "";

            if (context.IsPrivate)
            {
                prefixString += "DM with ";
            }
            else
            {
                if (context.Guild != null)
                {
                    prefixString += $"server: {context.Guild.Name} ({context.Guild.Id}), ";
                }

                if (context.Channel != null)
                {
                    prefixString += $"channel: #{context.Channel.Name} ({context.Channel.Id}), ";
                }
            }

            if (context.User != null)
            {
                prefixString += $"user: @{context.User.Username}#{context.User.Discriminator} ({context.User.Id}), ";
            }

            return prefixString;
        }

        public Task Log(string message, SocketCommandContext context)
        {
            _logger.LogInformation(message);
            return Task.CompletedTask;
        }
    }
}
