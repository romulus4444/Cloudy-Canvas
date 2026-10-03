namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    /// <summary>What the help command shows, and who it shows it to. The words themselves are in <see cref="HelpTopics"/>.</summary>
    public static class HelpText
    {
        private static readonly Dictionary<string, HelpTopic> TopicsByName = HelpTopics.All.ToDictionary(topic => topic.Name, StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, AdminSettingHelp> AdminSettingsByName =
            HelpTopics.AdminSettings.ToDictionary(setting => setting.Name, StringComparer.OrdinalIgnoreCase);

        /// <summary>The command names listed in the admin section of the overview (without their "..." placeholders).</summary>
        public static IEnumerable<string> ListedAdminCommands()
        {
            return Listed(HelpSection.Admin).Select(topic => topic.Name);
        }

        /// <summary>
        /// True if <paramref name="topic"/> is a command only bot admins can use. Their help is shown only to admins; anyone else is
        /// answered as if the command didn't exist, so the admin tooling isn't advertised to people who can't use it.
        /// </summary>
        public static bool IsAdminTopic(string topic)
        {
            return topic != null && TopicsByName.TryGetValue(topic, out var found) && found.AdminOnly;
        }

        /// <summary>
        /// The answer to ";help &lt;command&gt; &lt;subCommand&gt;". Admin commands are described only to admins; anyone else is told
        /// they are admin-only without being told anything about them.
        /// </summary>
        public static string Reply(char prefix, bool isAdmin, string command, string subCommand)
        {
            if (string.IsNullOrEmpty(command))
            {
                return Overview(prefix, isAdmin);
            }

            if (!isAdmin && IsAdminTopic(command))
            {
                // Don't show admin commands to people who can't use them; state that these are only available to admins.
                return $"Only available to bot admins. Use {prefix}help for a list of available commands.";
            }

            if (!TopicsByName.TryGetValue(command, out var topic) || topic.Text == null)
            {
                return $"Invalid command. Use `{prefix}help` for a list of available commands.";
            }

            if (topic.Name == "admin" && !string.IsNullOrEmpty(subCommand))
            {
                return AdminSettingReply(prefix, subCommand);
            }

            return Render(prefix, topic.Text);
        }

        /// <summary>The overview of all commands. The Admin Module section is only included for admins.</summary>
        public static string Overview(char prefix, bool isAdmin)
        {
            var newLine = Environment.NewLine;
            var text = new StringBuilder(string.Create(CultureInfo.InvariantCulture, $"**__All Commands:__**{newLine}**Booru Module:**{newLine}"));
            AppendCommands(text, prefix, HelpSection.Booru);
            if (isAdmin)
            {
                text.Append(CultureInfo.InvariantCulture, $"**Admin Module:**{newLine}");
                AppendCommands(text, prefix, HelpSection.Admin);
            }

            text.Append(CultureInfo.InvariantCulture, $"**Info Module:**{newLine}");
            AppendCommands(text, prefix, HelpSection.Info);
            text.Append(CultureInfo.InvariantCulture, $"{newLine}Use `{prefix}help <command>` for more details on a particular command.");
            return text.ToString();
        }

        private static IEnumerable<HelpTopic> Listed(HelpSection section)
        {
            return HelpTopics.All.Where(topic => topic.Section == section && topic.Listing != null);
        }

        private static void AppendCommands(StringBuilder text, char prefix, HelpSection section)
        {
            foreach (var topic in Listed(section))
            {
                text.Append(CultureInfo.InvariantCulture, $"`{prefix}{topic.Listing}`{Environment.NewLine}");
            }
        }

        private static string AdminSettingReply(char prefix, string setting)
        {
            if (!AdminSettingsByName.TryGetValue(setting, out var found))
            {
                return $"Invalid subcommand. Use `{prefix}help admin` for a list of available subcommands.";
            }

            return Render(prefix, AdminSettingLines(found));
        }

        /// <summary>The lines of the help for one ";admin &lt;setting&gt;" group, with {p} standing for the prefix.</summary>
        public static IReadOnlyList<string> AdminSettingLines(AdminSettingHelp setting)
        {
            var lines = new List<string> { $"__{{p}}admin {setting.Name} Commands:__", $"*{setting.Summary}*" };
            lines.AddRange(setting.Actions.Select(action =>
                $"`{{p}}admin {setting.Name} {action.Action}{(action.Arguments.Length > 0 ? " " + action.Arguments : string.Empty)}` {action.Description}"));
            return lines;
        }

        /// <summary>The lines of a help text, with the server's prefix filled in, as one message.</summary>
        private static string Render(char prefix, IEnumerable<string> lines)
        {
            return string.Join(Environment.NewLine, lines).Replace("{p}", prefix.ToString(), StringComparison.Ordinal);
        }
    }
}
