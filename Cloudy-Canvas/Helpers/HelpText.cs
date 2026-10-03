namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.Collections.Generic;
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

        /// <summary>The overview of all commands. The Admin Module section is only included for admins.</summary>
        public static string Overview(char prefix, bool isAdmin)
        {
            var newLine = Environment.NewLine;
            var text = new StringBuilder($"**__All Commands:__**{newLine}**Booru Module:**{newLine}");
            AppendCommands(text, prefix, BooruCommands);
            if (isAdmin)
            {
                text.Append($"**Admin Module:**{newLine}");
                AppendCommands(text, prefix, AdminCommands);
            }

            text.Append($"**Info Module:**{newLine}");
            AppendCommands(text, prefix, InfoCommands);
            text.Append($"{newLine}Use `{prefix}help <command>` for more details on a particular command.");
            return text.ToString();
        }

        private static void AppendCommands(StringBuilder text, char prefix, IEnumerable<string> commands)
        {
            foreach (var command in commands)
            {
                text.Append($"`{prefix}{command}`{Environment.NewLine}");
            }
        }
    }
}
