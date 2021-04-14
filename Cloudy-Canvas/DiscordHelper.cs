namespace Cloudy_Canvas
{
    using System.Linq;
    using System.Threading.Tasks;
    using Discord.Commands;

    public static class DiscordHelper
    {
        private const ulong Me = 828682017868218445;

        public static async Task<ulong> CheckIfChannelExistsAsync(string channelName, SocketCommandContext context)
        {
            var me = context.Guild.GetUser(Me);
            foreach (var channel in context.Guild.TextChannels)
            {
                if (channel.Name == channelName && channel.Users.Contains(me))
                {
                    return channel.Id;
                }
            }

            await Task.CompletedTask;
            return 0;
        }
        public static async Task<ulong> CheckIfChannelExistsAsync(ulong channelId, SocketCommandContext context)
        {
            var me = context.Guild.GetUser(828682017868218445);
            foreach (var channel in context.Guild.TextChannels)
            {
                if (channel.Id == channelId && channel.Users.Contains(me))
                {
                    return channel.Id;
                }
            }

            await Task.CompletedTask;
            return 0;
        }
    }
}
