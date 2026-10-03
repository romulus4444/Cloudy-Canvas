namespace Cloudy_Canvas.Admin
{
    /// <summary>The server settings an admin can manage with the ";admin" commands, and the wording for each.</summary>
    public static class AdminSettings
    {
        public static readonly ChannelSetting AdminChannel = new(
            "adminchannel",
            s => s.AdminChannel,
            (s, id) => s.AdminChannel = id,
            "Admin channel not set yet.",
            "Admin channel is {0}",
            "Admin channel set to {0}",
            UpdatesGuildList: true);

        public static readonly ChannelSetting WatchChannel = new(
            "watchchannel",
            s => s.WatchAlertChannel,
            (s, id) => s.WatchAlertChannel = id,
            "Watch alert channel not set yet.",
            "Watch alerts are being posted in {0}",
            "Watch alert channel set to {0}",
            "Watch alert channel reset to the current admin channel, {0}");

        public static readonly ChannelSetting ReportChannel = new(
            "reportchannel",
            s => s.ReportChannel,
            (s, id) => s.ReportChannel = id,
            "Report alert channel not set yet.",
            "Report alerts are being posted in {0}",
            "Report alert channel set to {0}",
            "Report alert channel reset to the current admin channel, {0}");

        public static readonly ChannelSetting LogChannel = new(
            "logchannel",
            s => s.LogPostChannel,
            (s, id) => s.LogPostChannel = id,
            "Log posting channel not set yet.",
            "Logs are being posted in {0}",
            "Retrieved logs will be sent to {0}",
            "Log post channel reset to the current admin channel, {0}");

        public static readonly RoleSetting AdminRole = new(
            "adminrole",
            s => s.AdminRole,
            (s, id) => s.AdminRole = id,
            "Admin role not set yet.",
            "Admin role is {0}",
            "Admin role set to {0}");

        public static readonly RoleSetting WatchRole = new(
            "watchrole",
            s => s.WatchAlertRole,
            (s, id) => s.WatchAlertRole = id,
            "Watch alert role not set yet.",
            "Watch alerts will ping {0}",
            "Watch alerts will now ping {0}",
            s => $"Watch alerts will not ping anyone now in {Mentions.Channel(s.WatchAlertChannel)}");

        public static readonly RoleSetting ReportRole = new(
            "reportrole",
            s => s.ReportRole,
            (s, id) => s.ReportRole = id,
            "Report alert role not set yet.",
            "Report alerts will ping {0}",
            "Report alerts will now ping {0}",
            s => $"Report alerts will not ping anyone now in {Mentions.Channel(s.ReportChannel)}");

        public static readonly ListSetting IgnoreChannel = new(
            "ignorechannel",
            "channel",
            s => s.IgnoredChannels,
            Mentions.Channel,
            "__Channel Ignore List:__",
            "No channels on ignore list.",
            "Added {0} to ignore list.",
            "Removed {0} from ignore list.",
            "Invalid channel name #{0}.",
            "Ignored channels list cleared.");

        public static readonly ListSetting IgnoreRole = new(
            "ignorerole",
            "role",
            s => s.IgnoredRoles,
            Mentions.Role,
            "__Role Ignore List:__",
            "No roles on ignore list.",
            "Added {0} to ignore list.",
            "Removed {0} from ignore list.",
            "Invalid role name @{0}.",
            "Ignored roles list cleared.");

        public static readonly ListSetting AllowUser = new(
            "allowuser",
            "user",
            s => s.AllowedUsers,
            Mentions.User,
            "__Allowed User List:__",
            "No users on allow list.",
            "Added {0} to allow list.",
            "Removed {0} from allow list.",
            "Invalid user name @{0}.",
            "Allowed users list cleared.");
    }
}
