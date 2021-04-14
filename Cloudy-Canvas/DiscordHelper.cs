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
            var me = await context.Channel.GetUserAsync(Me);
            if (context.IsPrivate)
            {
                return 0;
            }

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
            var me = await context.Channel.GetUserAsync(Me);
            if (context.IsPrivate)
            {
                return 0;
            }

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

        public static async Task<ulong> GetChannelIdIfAccessAsync(string channelName, SocketCommandContext context)
        {
            var id = ConvertChannelPingToId(channelName);
            if (id > 0)
            {
                return await CheckIfChannelExistsAsync(id, context);
            }
            return await CheckIfChannelExistsAsync(channelName, context);
        }

        public static async Task<string> ConvertChannelPingToNameAsync(string channelPing, SocketCommandContext context)
        {
            var id = ConvertChannelPingToId(channelPing);
            if (id <= 0)
            {
                return "<ERROR> Invalid channel";
            }
            var channel = context.Guild.GetTextChannel(id);
            return channel == null ? "<ERROR> Invalid channel" : channel.Name;
        }

        public static ulong ConvertChannelPingToId(string channelPing)
        {
            if (!channelPing.Contains("<#") || !channelPing.Contains(">"))
            {
                return 0;
            }
            var frontTrim = channelPing.Substring(2);
            var trim = frontTrim.Split('>', 2)[0];
            return ulong.Parse(trim);

        }
    }
}
