namespace Cloudy_Canvas
{
    using System.Threading.Tasks;
    using Discord.Commands;

    public static class DiscordHelper
    {
        public static async Task<ulong> CheckIfChannelExistsAsync(string channelName, SocketCommandContext context)
        {
            foreach (var channel in context.Guild.TextChannels)
            {
                if (channel.Name == channelName)
                {
                    return channel.Id;
                }
            }

            await Task.CompletedTask;
            return 0;
        }
    }
}
