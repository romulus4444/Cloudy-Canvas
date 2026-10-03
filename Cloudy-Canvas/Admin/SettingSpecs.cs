namespace Cloudy_Canvas.Admin
{
    using System;
    using System.Collections.Generic;
    using Cloudy_Canvas.Settings;

    /// <summary>A server setting that holds one channel (the admin channel, an alert channel, ...), with the wording used to talk about it.</summary>
    /// <param name="Key">The name in the command, e.g. "watchchannel".</param>
    /// <param name="NotSet">Reply when it isn't set.</param>
    /// <param name="GetFormat">Reply showing the channel; {0} is the channel mention.</param>
    /// <param name="SetFormat">Reply after setting it; {0} is the channel mention.</param>
    /// <param name="ClearFormat">Reply after clearing it back to the admin channel; null when the setting can't be cleared.</param>
    /// <param name="UpdatesGuildList">True for the admin channel, which is also recorded in the list of servers' admin channels.</param>
    public sealed record ChannelSetting(
        string Key,
        Func<ServerSettings, ulong> Get,
        Action<ServerSettings, ulong> Set,
        string NotSet,
        string GetFormat,
        string SetFormat,
        string ClearFormat = null,
        bool UpdatesGuildList = false);

    /// <summary>A server setting that holds one role, with the wording used to talk about it.</summary>
    /// <param name="Key">The name in the command, e.g. "watchrole".</param>
    /// <param name="NotSet">Reply when it isn't set.</param>
    /// <param name="GetFormat">Reply showing the role; {0} is the role mention.</param>
    /// <param name="SetFormat">Reply after setting it; {0} is the role mention.</param>
    /// <param name="ClearMessage">Reply after clearing it (it may mention another setting); null when the setting can't be cleared.</param>
    public sealed record RoleSetting(
        string Key,
        Func<ServerSettings, ulong> Get,
        Action<ServerSettings, ulong> Set,
        string NotSet,
        string GetFormat,
        string SetFormat,
        Func<ServerSettings, string> ClearMessage = null);

    /// <summary>A server setting that is a list of ids (ignored channels, ignored roles, allowed users), with the wording used to talk about it.</summary>
    /// <param name="Key">The name in the command, e.g. "ignorerole".</param>
    /// <param name="Noun">What one entry is: "channel", "role" or "user".</param>
    /// <param name="Mention">Turns an id into a mention.</param>
    /// <param name="Header">First line of the list.</param>
    /// <param name="Empty">Reply when the list is empty.</param>
    /// <param name="AddedFormat">Reply after adding; {0} is the mention.</param>
    /// <param name="RemovedFormat">Reply after removing; {0} is the mention.</param>
    /// <param name="InvalidFormat">Reply when the name given matches nothing; {0} is what was typed.</param>
    /// <param name="Cleared">Reply after clearing the list.</param>
    public sealed record ListSetting(
        string Key,
        string Noun,
        Func<ServerSettings, List<ulong>> List,
        Func<ulong, string> Mention,
        string Header,
        string Empty,
        string AddedFormat,
        string RemovedFormat,
        string InvalidFormat,
        string Cleared);

    /// <summary>How channels, roles and users are written into a message.</summary>
    public static class Mentions
    {
        public static string Channel(ulong id) => $"<#{id}>";

        public static string Role(ulong id) => $"<@&{id}>";

        public static string User(ulong id) => $"<@{id}>";
    }
}
