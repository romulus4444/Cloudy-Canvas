namespace Cloudy_Canvas
{
    using System.Collections.Generic;
    using System.IO;
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

        public static string ConvertChannelPingToName(string channelPing, SocketCommandContext context)
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

        public static string ConvertRolePingToNameAsync(string rolePing, SocketCommandContext context)
        {
            var id = ConvertRolePingToId(rolePing);
            if (id <= 0)
            {
                return "<ERROR> Invalid role";
            }

            var role = context.Guild.GetRole(id);
            return role == null ? "<ERROR> Invalid role" : role.Name;
        }

        public static async Task<List<ulong>> GetIgnoredRolesAsync(SocketCommandContext context)
        {
            var filename = FileHelper.SetUpFilepath(FilePathType.Server, "IgnoredRoles", "txt", context);
            if (!File.Exists(filename))
            {
                return new List<ulong>();
            }

            var roleList = await File.ReadAllLinesAsync(filename);
            var roleIdList = new List<ulong>();
            foreach (var role in roleList)
            {
                var one = role.Split("> @", 2)[0].Substring(3);
                roleIdList.Add(ulong.Parse(one));
            }

            return roleIdList;
        }

        public static async Task<bool> DoesUserHaveAdminRoleAsync(SocketCommandContext context)
        {
            if (context.IsPrivate)
            {
                return true;
            }

            var adminRole = await GetAdminRoleAsync(context);
            return context.Guild.GetUser(context.User.Id).Roles.Any(x => x.Id == adminRole);
        }

        public static async Task<ulong> GetAdminRoleAsync(SocketCommandContext context)
        {
            var setting = await FileHelper.GetSetting("adminrole", context);
            ulong roleId = 0;
            if (setting.Contains("<ERROR>") || !(setting.Contains("<@&") && setting.Contains(">")))
            {
                return roleId;
            }

            var split = setting.Split("<@&", 2)[1].Split('>', 2)[0];
            roleId = ulong.Parse(split);
            return roleId;
        }

        public static async Task<bool> CanUserRunThisCommandAsync(SocketCommandContext context)
        {
            if (context.IsPrivate)
            {
                return true;
            }

            var ignoredRoles = await GetIgnoredRolesAsync(context);
            var ignoredChannels = await GetIgnoredChannelsAsync(context);
            foreach (var ignoredChannel in ignoredChannels)
            {
                if (context.Channel.Id == ignoredChannel)
                {
                    return false;
                }
            }

            foreach (var ignoredRole in ignoredRoles)
            {
                if (context.Guild.GetUser(context.User.Id).Roles.Any(x => x.Id == ignoredRole))
                {
                    return false;
                }
            }

            return true;
        }

        public static async Task<List<ulong>> GetIgnoredChannelsAsync(SocketCommandContext context)
        {
            var filename = FileHelper.SetUpFilepath(FilePathType.Server, "IgnoredChannels", "txt", context);
            if (!File.Exists(filename))
            {
                return new List<ulong>();
            }

            var channelList = await File.ReadAllLinesAsync(filename);
            var channelIdList = new List<ulong>();
            foreach (var channel in channelList)
            {
                var one = channel.Split("> #", 2)[0].Substring(2);
                channelIdList.Add(ulong.Parse(one));
            }

            return channelIdList;
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
