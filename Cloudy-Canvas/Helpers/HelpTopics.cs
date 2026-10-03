namespace Cloudy_Canvas.Helpers
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>Where a command is listed in the overview that ";help" gives.</summary>
    public enum HelpSection
    {
        /// <summary>Not listed, but still has an answer (or is deliberately kept out of the help).</summary>
        None,
        Booru,
        Admin,
        Info,
    }

    /// <summary>A command that ";help &lt;name&gt;" can be asked about.</summary>
    /// <param name="Name">The command, as typed after the prefix.</param>
    /// <param name="Section">Which part of the overview lists it.</param>
    /// <param name="AdminOnly">True if only bot admins may use it; its help is then shown only to admins.</param>
    /// <param name="Listing">How it appears in the overview ("pick ..." when it takes arguments), or null if it is not listed.</param>
    /// <param name="Text">What the help says, one entry per line; {p} stands for the server's prefix. Null if there is nothing to say.</param>
    public sealed record HelpTopic(string Name, HelpSection Section, bool AdminOnly, string Listing, IReadOnlyList<string> Text);

    /// <summary>One thing an admin can do to a setting: ";admin &lt;setting&gt; &lt;action&gt; &lt;arguments&gt;".</summary>
    public sealed record AdminActionHelp(string Action, string Arguments, string Description);

    /// <summary>The help for one ";admin &lt;setting&gt;" command group. A test checks these against the actions the bot really has.</summary>
    public sealed record AdminSettingHelp(string Name, string Summary, IReadOnlyList<AdminActionHelp> Actions);

    /// <summary>
    /// Everything ";help" can say, as data. HelpText turns it into replies and into the overview, and tests compare it with the real
    /// commands, so a command without help (or help for a command that is gone) is caught by the build rather than by a user.
    /// </summary>
    public static class HelpTopics
    {
        public static readonly IReadOnlyList<AdminSettingHelp> AdminSettings = new[]
        {
            new AdminSettingHelp(
                "filter",
                "Manages the active filter.",
                new[]
                {
                    new AdminActionHelp("get", "", "Gets the current active filter."),
                    new AdminActionHelp("set", "<filter ID>", "Sets the active filter to <Filter ID>. Validates that the filter is useable by the bot."),
                }),
            new AdminSettingHelp(
                "adminchannel",
                "Manages the admin channel.",
                new[]
                {
                    new AdminActionHelp("get", "", "Gets the current admin channel."),
                    new AdminActionHelp("set", "<channel>", "Sets the admin channel to <channel>. Accepts a channel ping or plain text."),
                }),
            new AdminSettingHelp(
                "adminrole",
                "Manages the admin role.",
                new[]
                {
                    new AdminActionHelp("get", "", "Gets the current admin role."),
                    new AdminActionHelp("set", "<role>", "Sets the admin role to <role>. Accepts a role ping or plain text."),
                }),
            new AdminSettingHelp(
                "filterchannel",
                "Manages the list of channel-specific filters. NOTE: red and watch list checks are disabled for any channels on this list!",
                new[]
                {
                    new AdminActionHelp("get", "", "Gets the current list of channel-specific filters."),
                    new AdminActionHelp("add", "<channel> <filterId>", "Sets <channel> to use filter #<filterId>. Validates the filter first. Accepts a channel ping or plain text."),
                    new AdminActionHelp("remove", "<channel>", "Removes <channel> from the list of channel-specific filters. This channel will now use the default server filter. Accepts a channel ping or plain text."),
                    new AdminActionHelp("clear", "", "Clears the list of channel-specific filters. All channels will use the default server filter."),
                }),
            new AdminSettingHelp(
                "ignorechannel",
                "Manages the list of channels to ignore commands from.",
                new[]
                {
                    new AdminActionHelp("get", "", "Gets the current list of ignored channels."),
                    new AdminActionHelp("add", "<channel>", "Adds <channel> to the list of ignored channels. Accepts a channel ping or plain text."),
                    new AdminActionHelp("remove", "<channel>", "Removes <channel> from the list of ignored channels. Accepts a channel ping or plain text."),
                    new AdminActionHelp("clear", "", "Clears the list of ignored channels."),
                }),
            new AdminSettingHelp(
                "ignorerole",
                "Manages the list of roles to ignore commands from.",
                new[]
                {
                    new AdminActionHelp("get", "", "Gets the current list of ignored roles."),
                    new AdminActionHelp("add", "<role>", "Adds <role> to the list of ignored roles. Accepts a role ping or plain text."),
                    new AdminActionHelp("remove", "<role>", "Removes <role> from the list of ignored roles. Accepts a role ping or plain text."),
                    new AdminActionHelp("clear", "", "Clears the list of ignored roles."),
                }),
            new AdminSettingHelp(
                "allowuser",
                "Manages the list of users to allow commands from.",
                new[]
                {
                    new AdminActionHelp("get", "", "Gets the current list of allowed users."),
                    new AdminActionHelp("add", "<user>", "Adds <user> to the list of allowed users. Accepts a user ping or plain text."),
                    new AdminActionHelp("remove", "<user>", "Removes <user> from the list of allowed users. Accepts a user ping or plain text."),
                    new AdminActionHelp("clear", "", "Clears the list of allowed users."),
                }),
            new AdminSettingHelp(
                "watchchannel",
                "Manages the watch alert channel.",
                new[]
                {
                    new AdminActionHelp("get", "", "Gets the current watch alert channel."),
                    new AdminActionHelp("set", "<channel>", "Sets the watch alert channel to <channel>. Accepts a channel ping or plain text."),
                    new AdminActionHelp("clear", "", "Resets the watch alert channel to the current admin channel."),
                }),
            new AdminSettingHelp(
                "watchrole",
                "Manages the watch alert role.",
                new[]
                {
                    new AdminActionHelp("get", "", "Gets the current watch alert role."),
                    new AdminActionHelp("set", "<role>", "Sets the watch alert role to <role> and turns pinging on. Accepts a role ping or plain text."),
                    new AdminActionHelp("clear", "", "Resets the watch alert role to no role and turns pinging off."),
                }),
            new AdminSettingHelp(
                "reportchannel",
                "Manages the report alert channel.",
                new[]
                {
                    new AdminActionHelp("get", "", "Gets the current report alert channel."),
                    new AdminActionHelp("set", "<channel>", "Sets the report alert channel to <channel>. Accepts a channel ping or plain text."),
                    new AdminActionHelp("clear", "", "Resets the report alert channel to the current admin channel."),
                }),
            new AdminSettingHelp(
                "reportrole",
                "Manages the report alert role.",
                new[]
                {
                    new AdminActionHelp("get", "", "Gets the current report alert role."),
                    new AdminActionHelp("set", "<role>", "Sets the report alert role to <role> and turns pinging on. Accepts a role ping or plain text."),
                    new AdminActionHelp("clear", "", "Resets the report alert role to no role and turns pinging off."),
                }),
            new AdminSettingHelp(
                "logchannel",
                "Manages the log post channel.",
                new[]
                {
                    new AdminActionHelp("get", "", "Gets the current log post channel."),
                    new AdminActionHelp("set", "<channel>", "Sets the log post channel to <channel>. Accepts a channel ping or plain text."),
                    new AdminActionHelp("clear", "", "Resets the log post channel to the current admin channel."),
                }),
        };

        public static readonly IReadOnlyList<HelpTopic> All = new[]
        {
            new HelpTopic(
                "pick",
                HelpSection.Booru,
                false,
                "pick ...",
                new[]
                {
                    "`{p}pick <query>`",
                    "Posts a random image from a Manebooru <query>, if it is available. Each different search term in the query is separated by a comma. If results include any spoilered tags, the post is made in `||` spoiler bars.",
                }),
            new HelpTopic(
                "pickrecent",
                HelpSection.Booru,
                false,
                "pickrecent ...",
                new[]
                {
                    "`{p}pickrecent <query>`",
                    "Posts the most recently posted image from a Manebooru <query>, if it is available. Each different search term in the query is separated by a comma. If results include any spoilered tags, the post is made in `||` spoiler bars.",
                }),
            new HelpTopic(
                "id",
                HelpSection.Booru,
                false,
                "id ...",
                new[]
                {
                    "`{p}id <number>`",
                    "Posts Image #<number> from Manebooru, if it is available. If the image includes spoilered tags, the post is made in `||` spoiler bars.",
                }),
            new HelpTopic(
                "tags",
                HelpSection.Booru,
                false,
                "tags ...",
                new[]
                {
                    "`{p}tags <number>`",
                    "Posts the list of tags on Image <number> from Manebooru, if it is available, including identifying any tags that are spoilered.",
                }),
            new HelpTopic(
                "featured",
                HelpSection.Booru,
                false,
                "featured",
                new[]
                {
                    "`{p}featured`",
                    "Posts the current Featured Image from Manebooru.",
                }),
            new HelpTopic(
                "getspoilers",
                HelpSection.Booru,
                false,
                "getspoilers",
                new[]
                {
                    "`{p}getspoilers`",
                    "Posts a list of currently spoilered tags.",
                }),
            new HelpTopic(
                "report",
                HelpSection.Booru,
                false,
                "report ...",
                new[]
                {
                    "`{p}report <id> <reason>`",
                    "Alerts the admins about image #<id> with an optional <reason> for the admins to see. Only use this for images that violate the server rules!",
                }),
            new HelpTopic(
                "setup",
                HelpSection.Admin,
                true,
                "setup ...",
                new[]
                {
                    "`{p}setup <filter ID> <admin channel> <admin role>`",
                    "*Only a server administrator may use this command.*",
                    "Initial bot setup. Sets <filter ID> as the public Manebooru filter to use, <admin channel> for important admin output messages, and <admin role> as users who are allowed to use admin module commands. Validates that <Filter ID> is useable and if not, uses Filter 175.",
                }),
            new HelpTopic("admin", HelpSection.Admin, true, "admin ...", AdminOverview()),
            new HelpTopic(
                "watchlist",
                HelpSection.Admin,
                true,
                "watchlist ...",
                new[]
                {
                    "**__{p}watchlist Commands:__**",
                    "*Only users with the specified admin role may use these commands.*",
                    "Manages the list of terms users are unable to search for.",
                    "`{p}watchlist add <term>` Add <term> to the watchlist. <term> may be a comma-separated list.",
                    "`{p}watchlist remove <term>` Removes <term> from the watchlist.",
                    "`{p}watchlist get` Gets the current list of watchlisted terms.",
                    "`{p}watchlist clear` Clears the watchlist of all terms.",
                }),
            new HelpTopic(
                "log",
                HelpSection.Admin,
                true,
                "log ...",
                new[]
                {
                    "`{p}log <channel> <date>`",
                    "*Only users with the specified admin role may use this command.*",
                    "Posts the log file from <channel> and <date> into the admin channel. Accepts a channel ping or plain text. <date> must be formatted as YYYY-MM-DD.",
                }),
            new HelpTopic(
                "echo",
                HelpSection.Admin,
                true,
                "echo ...",
                new[]
                {
                    "`{p}echo <channel> <message>`",
                    "*Only users with the specified admin role may use this command.*",
                    "Posts <message> to a valid <channel>. If <channel> is invalid, posts to the current channel instead. Accepts a channel ping or plain text.",
                }),
            new HelpTopic(
                "setprefix",
                HelpSection.Admin,
                true,
                "setprefix ...",
                new[]
                {
                    "`{p}setprefix <prefix>`",
                    "*Only users with the specified admin role may use this command.*",
                    "Sets the prefix in front of commands to listen for to <prefix>. Accepts a single punctuation or symbol character (not @, #, <, > or a backtick).",
                }),
            new HelpTopic(
                "listentobots",
                HelpSection.Admin,
                true,
                "listentobots ...",
                new[]
                {
                    "`{p}listentobots <pos/neg>`",
                    "*Only users with the specified admin role may use this command.*",
                    "Toggles whether or not to run commands posted by other bots. Accepts y/n, yes/no, on/off, or true/false.",
                }),
            new HelpTopic(
                "safemode",
                HelpSection.Admin,
                true,
                "safemode ...",
                new[]
                {
                    "`{p}safemode <pos/neg>`",
                    "*Only users with the specified admin role may use this command.*",
                    "Toggles whether or not to automatically append `safe` to all booru queries. This overrides any channel-specific filters! Accepts y/n, yes/no, on/off, or true/false.",
                }),
            new HelpTopic(
                "alias",
                HelpSection.Admin,
                true,
                "alias ...",
                new[]
                {
                    "**__{p}alias Commands:__**",
                    "*Only users with the specified admin role may use these commands.*",
                    "Manages the list of command aliases.",
                    "`{p}alias add <short> <long>` Sets <short> as an alias of <long>. If a command starts with <short>, <short> is replaced with <long> and the command is then processed normally. Do not include prefixes in <short> or <long>. Example: `{p}alias cute pick cute` sets `{p}cute` to run `{p}pick cute` instead. To use an alias that includes spaces, surround the entire <short> term with \"\" quotes. If an alias for <short> already exists, it replaces the previous value of <long> with the new one.",
                    "`{p}alias remove <short>` Removes <short> as an alias for anything.",
                    "`{p}alias get` Gets the current list of aliases.",
                    "`{p}alias clear` Clears all aliases.",
                }),
            new HelpTopic(
                "getsettings",
                HelpSection.Admin,
                true,
                "getsettings",
                new[]
                {
                    "`{p}getsettings`",
                    "*Only users with the specified admin role may use this command.*",
                    "Posts the settings file to the log channel. This includes the watchlist.",
                }),
            new HelpTopic(
                "refreshlists",
                HelpSection.Admin,
                true,
                "refreshlists",
                new[]
                {
                    "`{p}refreshlists`",
                    "*Only users with the specified admin role may use this command.*",
                    "Rebuilds the spoiler list from the current active filter. This may take several minutes depending on how many tags are in there.",
                }),
            new HelpTopic(
                "origin",
                HelpSection.Info,
                false,
                "origin",
                new[]
                {
                    "`{p}origin`",
                    "Posts the origin of Manebooru's cute kirin mascot and the namesake of this bot, Cloudy Canvas.",
                }),
            new HelpTopic(
                "about",
                HelpSection.Info,
                false,
                "about",
                new[]
                {
                    "`{p}about` Information about this bot.",
                }),
            new HelpTopic(
                "help",
                HelpSection.None,
                false,
                null,
                new[]
                {
                    "<:sweetiegrump:642466824696627200>",
                }),
            new HelpTopic("broadcast", HelpSection.None, true, null, null),
        };

        private static List<string> AdminOverview()
        {
            var lines = new List<string>
            {
                "**__{p}admin Commands:__**",
                "*Only users with the specified admin role may use these commands*",
            };
            lines.AddRange(AdminSettings.Select(setting => $"`{{p}}admin {setting.Name} ...`"));
            lines.Add("");
            lines.Add("Use `{p}help admin <command>` for more details on a particular command.");
            return lines;
        }
    }
}
