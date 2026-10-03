namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;
    using Discord;
    using Discord.WebSocket;
    using Serilog;

    public static class DiscordHelper
    {
        private static readonly MemberRoleCache MemberCache = new(TimeSpan.FromSeconds(30));

        /// <summary>
        /// Resolves a channel given as a mention, a bare id or a name to the id of a channel in this server that the bot can see (0 if none).
        /// A text made only of digits that isn't the id of a channel is still tried as a channel name.
        /// </summary>
        public static Task<ulong> GetChannelIdIfAccessAsync(string channelName, SocketCommandContext context)
        {
            var mention = ConvertChannelPingToId(channelName);
            if (mention > 0)
            {
                return Task.FromResult(CheckIfChannelExists(mention, context));
            }

            var bareId = ParseSnowflake(channelName);
            if (bareId > 0)
            {
                var found = CheckIfChannelExists(bareId, context);
                if (found > 0)
                {
                    return Task.FromResult(found);
                }
            }

            return Task.FromResult(CheckIfChannelExists(channelName, context));
        }

        /// <summary>
        /// Resolves a role given as a mention, a bare id or a name to the id of a role in this server (0 if none).
        /// A text made only of digits that isn't the id of a role is still tried as a role name.
        /// </summary>
        public static ulong GetRoleId(string roleName, SocketCommandContext context)
        {
            var mention = ConvertRolePingToId(roleName);
            if (mention > 0)
            {
                return CheckIfRoleExists(mention, context);
            }

            var bareId = ParseSnowflake(roleName);
            if (bareId > 0)
            {
                var found = CheckIfRoleExists(bareId, context);
                if (found > 0)
                {
                    return found;
                }
            }

            return CheckIfRoleExists(roleName, context);
        }

        public static async Task<bool> IsBotAdminAsync(SocketCommandContext context, ServerSettings settings)
        {
            if (context.IsPrivate)
            {
                return true;
            }

            if (context.User is not SocketGuildUser)
            {
                return false;
            }

            var member = await GetGuildMemberAsync(context);
            return AccessPolicy.IsBotAdmin(member.RoleIds, member.Permissions, settings);
        }

        public static async Task<bool> CanUserRunCommandsAsync(SocketCommandContext context, ServerSettings settings)
        {
            if (context.IsPrivate)
            {
                return true;
            }

            if (context.User is not SocketGuildUser guildUser)
            {
                return false;
            }

            var member = await GetGuildMemberAsync(context);
            return AccessPolicy.CanRunCommands(guildUser.Id, context.Channel.Id, member.RoleIds, settings);
        }

        /// <summary>
        /// The member's current roles and permissions, straight from Discord (cached for 30 seconds).
        /// The member held in the socket cache can't be trusted for this: the bot doesn't have the GuildMembers intent, so it is never
        /// told when a member's roles change, and Discord.Net keeps whatever roles it first saw. A role granted or revoked after the
        /// member first spoke (an ignored role, the bot's admin role) would otherwise not count until the bot restarted.
        /// If Discord can't be reached this falls back to the cached member rather than failing the command.
        /// </summary>
        public static async Task<GuildMember> GetGuildMemberAsync(SocketCommandContext context)
        {
            var guild = context.Guild;
            var cachedUser = (SocketGuildUser)context.User;
            var now = DateTime.UtcNow;
            if (MemberCache.TryGet(guild.Id, cachedUser.Id, now, out var known))
            {
                return known;
            }

            try
            {
                var fresh = await context.Client.Rest.GetGuildUserAsync(guild.Id, cachedUser.Id);
                if (fresh != null)
                {
                    var roleIds = fresh.RoleIds.ToList();
                    var permissions = guild.OwnerId == cachedUser.Id
                        ? GuildPermissions.All
                        : AccessPolicy.CombinePermissions(
                            roleIds.Select(id => guild.GetRole(id)?.Permissions.RawValue ?? 0).Append(guild.EveryoneRole.Permissions.RawValue));
                    var member = new GuildMember(roleIds, permissions);
                    MemberCache.Set(guild.Id, cachedUser.Id, now, member);
                    return member;
                }
            }
            catch (Exception ex)
            {
                // Not cached, so the next command tries Discord again.
                Log.Warning(ex, "Could not fetch the current roles of {User} in {Guild}; using the cached ones", cachedUser.Id, guild.Id);
            }

            return new GuildMember(cachedUser.Roles.Select(role => role.Id).ToList(), cachedUser.GuildPermissions);
        }

        public static async Task<ulong> GetUserIdAsync(string userName, SocketCommandContext context)
        {
            var userId = ConvertUserPingToId(userName);
            if (userId > 0)
            {
                return userId;
            }

            // A bare id is taken as it is, the same as a mention: the member may not be cached, or may have left (and still need removing).
            var bareId = ParseSnowflake(userName);
            if (bareId > 0)
            {
                return bareId;
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
            var command = split[0].ToLowerInvariant();
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

        private static ulong CheckIfChannelExists(string channelName, SocketCommandContext context)
        {
            var name = channelName.Trim().TrimStart('#');
            return FindChannelCloudyCanBeSeen(context, channel => string.Equals(channel.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static ulong CheckIfChannelExists(ulong channelId, SocketCommandContext context)
        {
            return FindChannelCloudyCanBeSeen(context, channel => channel.Id == channelId);
        }

        /// <summary>
        /// The id of the first matching text channel in the server that the bot is able to see, or 0. This asks Discord's permission
        /// model directly; the old check looked for the bot among every member of every channel, which is slow in large servers
        /// and unreliable when the member cache is incomplete.
        /// </summary>
        private static ulong FindChannelCloudyCanBeSeen(SocketCommandContext context, Func<SocketTextChannel, bool> matches)
        {
            var me = context.Guild?.CurrentUser;
            if (context.IsPrivate || me == null)
            {
                return 0;
            }

            // Channel names aren't unique (the same name can exist in several categories), so keep looking past one the bot can't see.
            foreach (var channel in context.Guild.TextChannels)
            {
                if (matches(channel) && me.GetPermissions(channel).ViewChannel)
                {
                    return channel.Id;
                }
            }

            return 0;
        }

        /// <summary>
        /// Returns the id when the text is a bare Discord id (what you get from "Copy ID" with Developer Mode on): 17 to 20 digits.
        /// Anything else, including short numbers that are more likely a name or a typo, gives 0.
        /// </summary>
        public static ulong ParseSnowflake(string text)
        {
            var trimmed = text?.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.Length is < 17 or > 20 || !trimmed.All(char.IsAsciiDigit))
            {
                return 0;
            }

            return ulong.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : 0;
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
            if (!trimmed.StartsWith(prefix, StringComparison.Ordinal) || !trimmed.EndsWith('>'))
            {
                return 0;
            }

            var digits = trimmed[prefix.Length..^1];
            return ulong.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : 0;
        }

        private static ulong CheckIfRoleExists(string roleName, SocketCommandContext context)
        {
            if (context.IsPrivate)
            {
                return 0;
            }

            var name = roleName.Trim().TrimStart('@');
            foreach (var role in context.Guild.Roles)
            {
                if (string.Equals(role.Name.TrimStart('@'), name, StringComparison.OrdinalIgnoreCase))
                {
                    return role.Id;
                }
            }

            return 0;
        }

        private static ulong CheckIfRoleExists(ulong roleId, SocketCommandContext context)
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
