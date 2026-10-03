namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>What the help command shows, and who it shows it to.</summary>
    public static class HelpText
    {
        /// <summary>
        /// Commands only bot admins can use. Their help is shown only to admins; anyone else is answered as if the command didn't exist,
        /// so the admin tooling isn't advertised to people who can't use it.
        /// </summary>
        public static readonly IReadOnlyCollection<string> AdminTopics = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "setup", "admin", "watchlist", "log", "echo", "setprefix", "listentobots", "safemode", "alias", "getsettings", "refreshlists",
            "broadcast",
        };

        private static readonly string[] BooruCommands = { "pick ...", "pickrecent ...", "id ...", "tags ...", "featured", "getspoilers", "report ..." };

        private static readonly string[] AdminCommands =
        {
            "setup ...", "admin ...", "watchlist ...", "log ...", "echo ...", "setprefix ...", "listentobots ...", "safemode ...", "alias ...",
            "getsettings", "refreshlists",
        };

        private static readonly string[] InfoCommands = { "origin", "about" };

        /// <summary>The command names listed in the admin section of the overview (without their "..." placeholders).</summary>
        public static IEnumerable<string> ListedAdminCommands()
        {
            foreach (var command in AdminCommands)
            {
                yield return command.Split(' ')[0];
            }
        }

        /// <summary>True if help for <paramref name="topic"/> is only for admins.</summary>
        public static bool IsAdminTopic(string topic)
        {
            return topic != null && ((HashSet<string>)AdminTopics).Contains(topic);
        }

        /// <summary>
        /// The answer to ";help &lt;command&gt; &lt;subCommand&gt;". Admin commands are described only to admins; anyone else is told
        /// they are admin-only without being told anything about them.
        /// </summary>
        public static string Reply(char prefix, bool isAdmin, string command, string subCommand)
        {
            if (!isAdmin && IsAdminTopic(command))
            {
                // Don't show admin commands to people who can't use them; state that these are only available to admins.
                return $"Only available to bot admins. Use {prefix}help for a list of available commands.";
            }

            switch (command)
            {
                case "":
                    return Overview(prefix, isAdmin);
                case "pick":
                    return $"`{prefix}pick <query>`{Environment.NewLine}Posts a random image from a Manebooru <query>, if it is available. Each different search term in the query is separated by a comma. If results include any spoilered tags, the post is made in `||` spoiler bars.";
                case "pickrecent":
                    return $"`{prefix}pickrecent <query>`{Environment.NewLine}Posts the most recently posted image from a Manebooru <query>, if it is available. Each different search term in the query is separated by a comma. If results include any spoilered tags, the post is made in `||` spoiler bars.";
                case "id":
                    return $"`{prefix}id <number>`{Environment.NewLine}Posts Image #<number> from Manebooru, if it is available. If the image includes spoilered tags, the post is made in `||` spoiler bars.";
                case "tags":
                    return $"`{prefix}tags <number>`{Environment.NewLine}Posts the list of tags on Image <number> from Manebooru, if it is available, including identifying any tags that are spoilered.";
                case "featured":
                    return $"`{prefix}featured`{Environment.NewLine}Posts the current Featured Image from Manebooru.";
                case "getspoilers":
                    return $"`{prefix}getspoilers`{Environment.NewLine}Posts a list of currently spoilered tags.";
                case "report":
                    return $"`{prefix}report <id> <reason>`{Environment.NewLine}Alerts the admins about image #<id> with an optional <reason> for the admins to see. Only use this for images that violate the server rules!";
                case "setup":
                    return $"`{prefix}setup <filter ID> <admin channel> <admin role>`{Environment.NewLine}*Only a server administrator may use this command.*{Environment.NewLine}Initial bot setup. Sets <filter ID> as the public Manebooru filter to use, <admin channel> for important admin output messages, and <admin role> as users who are allowed to use admin module commands. Validates that <Filter ID> is useable and if not, uses Filter 175.";
                case "admin":
                    switch (subCommand)
                    {
                        case "":
                            return $"**__{prefix}admin Commands:__**{Environment.NewLine}*Only users with the specified admin role may use these commands*{Environment.NewLine}`{prefix}admin filter ...`{Environment.NewLine}`{prefix}admin adminchannel ...`{Environment.NewLine}`{prefix}admin adminrole ...`{Environment.NewLine}`{prefix}admin filterchannel ...`{Environment.NewLine}`{prefix}admin ignorechannel ...`{Environment.NewLine}`{prefix}admin ignorerole ...`{Environment.NewLine}`{prefix}admin allowuser ...`{Environment.NewLine}`{prefix}admin watchchannel ...`{Environment.NewLine}`{prefix}admin watchrole ...`{Environment.NewLine}`{prefix}admin reportchannel ...`{Environment.NewLine}`{prefix}admin reportrole ...`{Environment.NewLine}`{prefix}admin logchannel ...`{Environment.NewLine}{Environment.NewLine}Use `{prefix}help admin <command>` for more details on a particular command.";
                        case "filter":
                            return $"__{prefix}admin filter Commands:__{Environment.NewLine}*Manages the active filter.*{Environment.NewLine}`{prefix}admin filter get` Gets the current active filter.{Environment.NewLine}`{prefix}admin filter set <filter ID>` Sets the active filter to <Filter ID>. Validates that the filter is useable by the bot.";
                        case "adminchannel":
                            return $"__{prefix}admin adminchannel Commands:__{Environment.NewLine}*Manages the admin channel.*{Environment.NewLine}`{prefix}admin adminchannel get` Gets the current admin channel.{Environment.NewLine}`{prefix}admin adminchannel set <channel>` Sets the admin channel to <channel>. Accepts a channel ping or plain text.";
                        case "adminrole":
                            return $"__{prefix}admin adminrole Commands:__{Environment.NewLine}*Manages the admin role.*{Environment.NewLine}`{prefix}admin adminrole get` Gets the current admin role.{Environment.NewLine}`{prefix}admin adminrole set <role>` Sets the admin role to <role>. Accepts a role ping or plain text.";
                        case "filterchannel":
                            return $"__{prefix}admin filterchannel Commands:__{Environment.NewLine}*Manages the list of channel-specific filters. NOTE: red and watch list checks are disabled for any channels on this list!*{Environment.NewLine}`{prefix}admin filterchannel get` Gets the current list of channel-specific filters.{Environment.NewLine}`{prefix}admin filterchannel add <channel> <filterId>` Sets <channel> to use filter #<filterId>. Validates the filter first. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin filterchannel remove <channel>` Removes <channel> from the list of channel-specific filters. This channel will now use the default server filter. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin filterchannel clear` Clears the list of channel-specific filters. All channels will use the default server filter.";
                        case "ignorechannel":
                            return $"__{prefix}admin ignorechannel Commands:__{Environment.NewLine}*Manages the list of channels to ignore commands from.*{Environment.NewLine}`{prefix}admin ignorechannel get` Gets the current list of ignored channels.{Environment.NewLine}`{prefix}admin ignorechannel add <channel>` Adds <channel> to the list of ignored channels. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin ignorechannel remove <channel>` Removes <channel> from the list of ignored channels. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin ignorechannel clear` Clears the list of ignored channels.";
                        case "ignorerole":
                            return $"__{prefix}admin ignorerole Commands:__{Environment.NewLine}*Manages the list of roles to ignore commands from.*{Environment.NewLine}`{prefix}admin ignorerole get` Gets the current list of ignored roles.{Environment.NewLine}`{prefix}admin ignorerole add <role>` Adds <role> to the list of ignored roles. Accepts a role ping or plain text.{Environment.NewLine}`{prefix}admin ignorerole remove <role>` Removes <role> from the list of ignored roles. Accepts a role ping or plain text.{Environment.NewLine}`{prefix}admin ignorerole clear` Clears the list of ignored roles.";
                        case "allowuser":
                            return $"__{prefix}admin allowuser Commands:__{Environment.NewLine}*Manages the list of users to allow commands from.*{Environment.NewLine}`{prefix}admin allowuser get` Gets the current list of allowed users.{Environment.NewLine}`{prefix}admin allowuser add <user>` Adds <user> to the list of allowed users. Accepts a user ping or plain text.{Environment.NewLine}`{prefix}admin allowuser remove <user>` Removes <user> from the list of allowed users. Accepts a user ping or plain text.{Environment.NewLine}`{prefix}admin allowuser clear` Clears the list of allowed users.";
                        case "watchchannel":
                            return $"__{prefix}admin watchchannel Commands:__{Environment.NewLine}*Manages the watch alert channel.*{Environment.NewLine}`{prefix}admin watchchannel get` Gets the current watch alert channel.{Environment.NewLine}`{prefix}admin watchchannel set <channel>` Sets the watch alert channel to <channel>. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin watchchannel clear` Resets the watch alert channel to the current admin channel.";
                        case "watchrole":
                            return $"__{prefix}admin watchrole Commands:__{Environment.NewLine}*Manages the watch alert role.*{Environment.NewLine}`{prefix}admin watchrole get` Gets the current watch alert role.{Environment.NewLine}`{prefix}admin watchrole set <role>` Sets the watch alert role to <role> and turns pinging on. Accepts a role ping or plain text.{Environment.NewLine}`{prefix}admin watchrole clear` Resets the watch alert role to no role and turns pinging off.";
                        case "reportchannel":
                            return $"__{prefix}admin reportchannel Commands:__{Environment.NewLine}*Manages the report alert channel.*{Environment.NewLine}`{prefix}admin reportchannel get` Gets the current report alert channel.{Environment.NewLine}`{prefix}admin reportchannel set <channel>` Sets the report alert channel to <channel>. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin reportchannel clear` Resets the report alert channel to the current admin channel.";
                        case "reportrole":
                            return $"__{prefix}admin reportrole Commands:__{Environment.NewLine}*Manages the report alert role.*{Environment.NewLine}`{prefix}admin reportrole get` Gets the current report alert role.{Environment.NewLine}`{prefix}admin reportrole set <role>` Sets the report alert role to <role> and turns pinging on. Accepts a role ping or plain text.{Environment.NewLine}`{prefix}admin reportrole clear` Resets the report alert role to no role and turns pinging off.";
                        case "logchannel":
                            return $"__{prefix}admin logchannel Commands:__{Environment.NewLine}*Manages the log post channel.*{Environment.NewLine}`{prefix}admin logchannel get` Gets the current log post channel.{Environment.NewLine}`{prefix}admin logchannel set <channel>` Sets the log post channel to <channel>. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin logchannel clear` Resets the log post channel to the current admin channel.";
                        default:
                            return $"Invalid subcommand. Use `{prefix}help admin` for a list of available subcommands.";
                    }
                case "watchlist":
                    return $"**__{prefix}watchlist Commands:__**{Environment.NewLine}*Only users with the specified admin role may use these commands.*{Environment.NewLine}Manages the list of terms users are unable to search for.{Environment.NewLine}`{prefix}watchlist add <term>` Add <term> to the watchlist. <term> may be a comma-separated list.{Environment.NewLine}`{prefix}watchlist remove <term>` Removes <term> from the watchlist.{Environment.NewLine}`{prefix}watchlist get` Gets the current list of watchlisted terms.{Environment.NewLine}`{prefix}watchlist clear` Clears the watchlist of all terms.";
                case "log":
                    return $"`{prefix}log <channel> <date>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Posts the log file from <channel> and <date> into the admin channel. Accepts a channel ping or plain text. <date> must be formatted as YYYY-MM-DD.";
                case "echo":
                    return $"`{prefix}echo <channel> <message>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Posts <message> to a valid <channel>. If <channel> is invalid, posts to the current channel instead. Accepts a channel ping or plain text.";
                case "setprefix":
                    return $"`{prefix}setprefix <prefix>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Sets the prefix in front of commands to listen for to <prefix>. Accepts a single punctuation or symbol character (not @, #, <, > or a backtick).";
                case "listentobots":
                    return $"`{prefix}listentobots <pos/neg>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Toggles whether or not to run commands posted by other bots. Accepts y/n, yes/no, on/off, or true/false.";
                case "safemode":
                    return $"`{prefix}safemode <pos/neg>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Toggles whether or not to automatically append `safe` to all booru queries. This overrides any channel-specific filters! Accepts y/n, yes/no, on/off, or true/false.";
                case "alias":
                    return $"**__{prefix}alias Commands:__**{Environment.NewLine}*Only users with the specified admin role may use these commands.*{Environment.NewLine}Manages the list of command aliases.{Environment.NewLine}`{prefix}alias add <short> <long>` Sets <short> as an alias of <long>. If a command starts with <short>, <short> is replaced with <long> and the command is then processed normally. Do not include prefixes in <short> or <long>. Example: `{prefix}alias cute pick cute` sets `{prefix}cute` to run `{prefix}pick cute` instead. To use an alias that includes spaces, surround the entire <short> term with \"\" quotes. If an alias for <short> already exists, it replaces the previous value of <long> with the new one.{Environment.NewLine}`{prefix}alias remove <short>` Removes <short> as an alias for anything.{Environment.NewLine}`{prefix}alias get` Gets the current list of aliases.{Environment.NewLine}`{prefix}alias clear` Clears all aliases.";
                case "getsettings":
                    return $"`{prefix}getsettings`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Posts the settings file to the log channel. This includes the watchlist.";
                case "refreshlists":
                    return $"`{prefix}refreshlists`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Rebuilds the spoiler list from the current active filter. This may take several minutes depending on how many tags are in there.";
                case "origin":
                    return $"`{prefix}origin`{Environment.NewLine}Posts the origin of Manebooru's cute kirin mascot and the namesake of this bot, Cloudy Canvas.";
                case "about":
                    return $"`{prefix}about` Information about this bot.";
                case "help":
                    return "<:sweetiegrump:642466824696627200>";
                default:
                    return $"Invalid command. Use `{prefix}help` for a list of available commands.";
            }
        }

        /// <summary>The overview of all commands. The Admin Module section is only included for admins.</summary>
        public static string Overview(char prefix, bool isAdmin)
        {
            var newLine = Environment.NewLine;
            var text = new StringBuilder(string.Create(CultureInfo.InvariantCulture, $"**__All Commands:__**{newLine}**Booru Module:**{newLine}"));
            AppendCommands(text, prefix, BooruCommands);
            if (isAdmin)
            {
                text.Append(CultureInfo.InvariantCulture, $"**Admin Module:**{newLine}");
                AppendCommands(text, prefix, AdminCommands);
            }

            text.Append(CultureInfo.InvariantCulture, $"**Info Module:**{newLine}");
            AppendCommands(text, prefix, InfoCommands);
            text.Append(CultureInfo.InvariantCulture, $"{newLine}Use `{prefix}help <command>` for more details on a particular command.");
            return text.ToString();
        }

        private static void AppendCommands(StringBuilder text, char prefix, IEnumerable<string> commands)
        {
            foreach (var command in commands)
            {
                text.Append(CultureInfo.InvariantCulture, $"`{prefix}{command}`{Environment.NewLine}");
            }
        }
    }
}
