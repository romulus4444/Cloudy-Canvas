namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;
    using Discord.WebSocket;

    public static class DiscordHelper
    {
        public static async Task<ulong> GetChannelIdIfAccessAsync(string channelName, SocketCommandContext context)
        {
            var id = ConvertChannelPingToId(channelName);
            if (id > 0)
            {
                return await CheckIfChannelExistsAsync(id, context);
            }

            return await CheckIfChannelExistsAsync(channelName, context);
        }

        public static ulong GetRoleIdIfAccessAsync(string roleName, SocketCommandContext context)
        {
            var id = ConvertRolePingToId(roleName);
            return id > 0 ? CheckIfRoleExistsAsync(id, context) : CheckIfRoleExistsAsync(roleName, context);
        }

        public static bool DoesUserHaveAdminRoleAsync(SocketCommandContext context, ServerSettings settings)
        {
            if (context.IsPrivate)
            {
                return true;
            }

            if (context.User is not SocketGuildUser user)
            {
                return false;
            }

            // Server administrators can always use the bot's admin commands.
            if (user.GuildPermissions.Administrator)
            {
                return true;
            }

            // Fail closed: until an admin role has been configured, only members who can manage the server count as admins.
            if (settings.AdminRole == 0)
            {
                return user.GuildPermissions.ManageGuild;
            }

            return user.Roles.Any(x => x.Id == settings.AdminRole);
        }

        public static bool CanUserRunThisCommand(SocketCommandContext context, ServerSettings settings)
        {
            if (context.IsPrivate)
            {
                return true;
            }

            if (context.User is not SocketGuildUser guildUser)
            {
                return false;
            }

            if (guildUser.Roles.Any(x => x.Id == settings.AdminRole))
            {
                return true;
            }

            foreach (var allowedUser in settings.AllowedUsers)
            {
                if (context.User.Id == allowedUser)
                {
                    return true;
                }
            }

            foreach (var ignoredChannel in settings.IgnoredChannels)
            {
                if (context.Channel.Id == ignoredChannel)
                {
                    return false;
                }
            }

            foreach (var ignoredRole in settings.IgnoredRoles)
            {
                if (guildUser.Roles.Any(x => x.Id == ignoredRole))
                {
                    return false;
                }
            }

            return true;
        }

        public static async Task<ulong> GeUserIdFromPingOrIfOnlySearchResultAsync(string userName, SocketCommandContext context)
        {
            var userId = ConvertUserPingToId(userName);
            if (userId > 0)
            {
                return userId;
            }

            var userList = await context.Guild.SearchUsersAsync(userName);
            return userList.Count != 1 ? 0 : userList.First().Id;
        }

        /// <summary>
        /// Strips the prefix character from <paramref name="message"/> and expands a command alias at the start of what is left.
        /// An alias only matches a whole leading word (or leading words, for aliases containing spaces) and only the alias itself is
        /// replaced, never text in the arguments. If several aliases match, the longest one wins. The command word is lower-cased.
        /// </summary>
        public static string ResolveAliases(string message, ServerPreloadedSettings settings)
        {
            var rawCommand = message.Length > 0 ? message[1..].TrimStart() : string.Empty;
            string bestShort = null;
            string bestLong = null;
            foreach (var (shortForm, longForm) in settings.Aliases)
            {
                if (string.IsNullOrWhiteSpace(shortForm) || !StartsWithWholeWords(rawCommand, shortForm))
                {
                    continue;
                }

                if (bestShort == null || shortForm.Length > bestShort.Length)
                {
                    bestShort = shortForm;
                    bestLong = longForm;
                }
            }

            if (bestShort != null)
            {
                rawCommand = (bestLong + rawCommand[bestShort.Length..]).TrimStart();
            }

            var split = rawCommand.Split(' ', 2);
            var command = split[0].ToLower();
            if (split.Length > 1)
            {
                command += " " + split[1];
            }

            return command;
        }

        private static bool StartsWithWholeWords(string text, string prefix)
        {
            return text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && (text.Length == prefix.Length || char.IsWhiteSpace(text[prefix.Length]));
        }

        private static async Task<ulong> CheckIfChannelExistsAsync(string channelName, SocketCommandContext context)
        {
            var cloudyCanvas = await context.Channel.GetUserAsync(context.Client.CurrentUser.Id);
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

            return 0;
        }

        private static async Task<ulong> CheckIfChannelExistsAsync(ulong channelId, SocketCommandContext context)
        {
            var cloudyCanvas = await context.Channel.GetUserAsync(context.Client.CurrentUser.Id);
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

            return 0;
        }

        /// <summary>Returns the channel id from a <c>&lt;#id&gt;</c> mention, or 0 if the text is not a valid channel mention.</summary>
        public static ulong ConvertChannelPingToId(string channelPing)
        {
            return ParseMention(channelPing, "<#");
        }

        /// <summary>Returns the user id from a <c>&lt;@id&gt;</c> or legacy <c>&lt;@!id&gt;</c> mention, or 0 if the text is not a valid user mention.</summary>
        public static ulong ConvertUserPingToId(string userPing)
        {
            var id = ParseMention(userPing, "<@!");
            return id > 0 ? id : ParseMention(userPing, "<@");
        }

        private static ulong ParseMention(string text, string prefix)
        {
            if (text == null)
            {
                return 0;
            }

            var trimmed = text.Trim();
            if (!trimmed.StartsWith(prefix) || !trimmed.EndsWith('>'))
            {
                return 0;
            }

            var digits = trimmed[prefix.Length..^1];
            return ulong.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : 0;
        }

        private static ulong CheckIfRoleExistsAsync(string roleName, SocketCommandContext context)
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

            return 0;
        }

        private static ulong CheckIfRoleExistsAsync(ulong roleId, SocketCommandContext context)
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

            return 0;
        }

        /// <summary>Returns the role id from a <c>&lt;@&amp;id&gt;</c> mention, or 0 if the text is not a valid role mention.</summary>
        public static ulong ConvertRolePingToId(string rolePing)
        {
            return ParseMention(rolePing, "<@&");
        }
    }
}
