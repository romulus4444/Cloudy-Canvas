namespace Cloudy_Canvas.Service
{
    using Discord.Commands;

    public class LoggingService
    {
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
    }
}
