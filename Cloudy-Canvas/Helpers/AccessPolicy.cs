namespace Cloudy_Canvas.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using Cloudy_Canvas.Settings;
    using Discord;

    /// <summary>
    /// Who may use the bot, decided purely from a member's roles and the server's settings. Kept free of Discord connections so it
    /// can be unit tested.
    /// </summary>
    public static class AccessPolicy
    {
        /// <summary>
        /// Whether a user may run commands in a channel. In order: the bot's admin role and the allowed-user list are never ignored;
        /// otherwise an ignored channel or any ignored role means "no"; everyone else is allowed.
        /// </summary>
        public static bool CanRunCommands(ulong userId, ulong channelId, IReadOnlyCollection<ulong> roleIds, ServerSettings settings)
        {
            if (settings.AdminRole != 0 && roleIds.Contains(settings.AdminRole))
            {
                return true;
            }

            if (settings.AllowedUsers.Contains(userId))
            {
                return true;
            }

            if (settings.IgnoredChannels.Contains(channelId))
            {
                return false;
            }

            return !settings.IgnoredRoles.Any(roleIds.Contains);
        }

        /// <summary>
        /// Whether a member may use the bot's admin commands. Server administrators always can. Until an admin role has been set only
        /// members who can manage the server count; after that it takes the admin role.
        /// </summary>
        public static bool IsBotAdmin(IReadOnlyCollection<ulong> roleIds, GuildPermissions permissions, ServerSettings settings)
        {
            if (permissions.Administrator)
            {
                return true;
            }

            return settings.AdminRole == 0 ? permissions.ManageGuild : roleIds.Contains(settings.AdminRole);
        }

        /// <summary>A member's server-wide permissions: the union of @everyone's and each of their roles'. Administrator implies all.</summary>
        public static GuildPermissions CombinePermissions(IEnumerable<ulong> rawPermissionValues)
        {
            ulong combined = 0;
            foreach (var raw in rawPermissionValues)
            {
                combined |= raw;
            }

            return new GuildPermissions(combined);
        }
    }

    /// <summary>A member's current roles and permissions.</summary>
    public sealed record GuildMember(IReadOnlyCollection<ulong> RoleIds, GuildPermissions Permissions);
}
