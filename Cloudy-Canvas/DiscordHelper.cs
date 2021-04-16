namespace Cloudy_Canvas
{
    using System.Linq;
    using System.Threading.Tasks;
    using Discord.Commands;

    public static class DiscordHelper
    {
        private const ulong CloudyCanvasId = 828682017868218445; //Cloudy Canvas's Discord Id

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

        public static async Task<ulong> GetRoleIdIfAccessAsync(string roleName, SocketCommandContext context)
        {
            var id = ConvertRolePingToId(roleName);
            if (id > 0)
            {
                return await CheckIfRoleExistsAsync(id, context);
            }

            return await CheckIfRoleExistsAsync(roleName, context);
        }

        public static async Task<string> ConvertRolePingToNameAsync(string rolePing, SocketCommandContext context)
        {
            var id = ConvertRolePingToId(rolePing);
            if (id <= 0)
            {
                return "<ERROR> Invalid role";
            }

            var role = context.Guild.GetRole(id);
            return role == null ? "<ERROR> Invalid role" : role.Name;
        }

        private static async Task<ulong> CheckIfChannelExistsAsync(string channelName, SocketCommandContext context)
        {
            var cloudyCanvas = await context.Channel.GetUserAsync(CloudyCanvasId);
            if (context.IsPrivate)
            {
                return 0;
            }

            foreach (var channel in context.Guild.TextChannels)
            {
                if (channel.Name == channelName && channel.Users.Contains(cloudyCanvas))
                {
                    return channel.Id;
                }
            }

            await Task.CompletedTask;
            return 0;
        }

        private static async Task<ulong> CheckIfChannelExistsAsync(ulong channelId, SocketCommandContext context)
        {
            var cloudyCanvas = await context.Channel.GetUserAsync(CloudyCanvasId);
            if (context.IsPrivate)
            {
                return 0;
            }

            foreach (var channel in context.Guild.TextChannels)
            {
                if (channel.Id == channelId && channel.Users.Contains(cloudyCanvas))
                {
                    return channel.Id;
                }
            }

            await Task.CompletedTask;
            return 0;
        }

        private static ulong ConvertChannelPingToId(string channelPing)
        {
            if (!channelPing.Contains("<#") || !channelPing.Contains(">"))
            {
                return 0;
            }

            var frontTrim = channelPing.Substring(2);
            var trim = frontTrim.Split('>', 2)[0];
            return ulong.Parse(trim);
        }

        private static async Task<ulong> CheckIfRoleExistsAsync(string roleName, SocketCommandContext context)
        {
            if (context.IsPrivate)
            {
                return 0;
            }

            foreach (var role in context.Guild.Roles)
            {
                if (role.Name == roleName)
                {
                    return role.Id;
                }
            }

            await Task.CompletedTask;
            return 0;
        }

        private static async Task<ulong> CheckIfRoleExistsAsync(ulong roleId, SocketCommandContext context)
        {
            if (context.IsPrivate)
            {
                return 0;
            }

            foreach (var role in context.Guild.Roles)
            {
                if (role.Id == roleId)
                {
                    return role.Id;
                }
            }

            await Task.CompletedTask;
            return 0;
        }

        private static ulong ConvertRolePingToId(string rolePing)
        {
            if (!rolePing.Contains("<@&") || !rolePing.Contains(">"))
            {
                return 0;
            }

            var frontTrim = rolePing.Substring(3);
            var trim = frontTrim.Split('>', 2)[0];
            return ulong.Parse(trim);
        }
    }
}
