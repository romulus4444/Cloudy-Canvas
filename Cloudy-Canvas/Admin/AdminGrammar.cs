namespace Cloudy_Canvas.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>What an admin said and how the bot answers it when it isn't a command it knows: the text, and the line to log.</summary>
    public sealed record AdminReply(string Message, string LogText);

    /// <summary>
    /// The shape of the ";admin &lt;setting&gt; &lt;action&gt;" commands, and the answers for a missing or unrecognised part. A test compares
    /// this table with the commands of the real module in both directions, so the two cannot drift apart.
    /// </summary>
    public static class AdminGrammar
    {
        private static readonly string[] GetSet = { "get", "set" };
        private static readonly string[] GetSetClear = { "get", "set", "clear" };
        private static readonly string[] GetAddRemoveClear = { "get", "add", "remove", "clear" };

        /// <summary>Every setting an admin can manage, with the actions each one supports.</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Actions =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["filter"] = GetSet,
                ["adminchannel"] = GetSet,
                ["adminrole"] = GetSet,
                ["ignorechannel"] = GetAddRemoveClear,
                ["filterchannel"] = GetAddRemoveClear,
                ["ignorerole"] = GetAddRemoveClear,
                ["allowuser"] = GetAddRemoveClear,
                ["watchchannel"] = GetSetClear,
                ["watchrole"] = GetSetClear,
                ["reportchannel"] = GetSetClear,
                ["reportrole"] = GetSetClear,
                ["logchannel"] = GetSetClear,
            };

        /// <summary>
        /// Answers an ";admin ..." command that no specific command took: nothing at all, an unknown setting, a setting without an action,
        /// an unknown action, or a known action whose arguments couldn't be understood.
        /// </summary>
        /// <param name="rest">Everything typed after "admin".</param>
        public static AdminReply Explain(string rest)
        {
            var words = (rest ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                return new AdminReply("You need to specify an admin command.", "admin: <FAIL>");
            }

            var setting = words[0];
            if (!Actions.TryGetValue(setting, out var actions))
            {
                return new AdminReply($"Invalid command `{setting}`", $"admin: {setting} <FAIL>");
            }

            if (words.Length == 1)
            {
                return new AdminReply("You must specify a subcommand.", $"admin: {setting} <FAIL>");
            }

            var action = words[1];
            if (!actions.Contains(action, StringComparer.OrdinalIgnoreCase))
            {
                return new AdminReply($"Invalid command {action}", $"admin: {setting} {action} <FAIL>");
            }

            return new AdminReply(
                $"I couldn't understand those arguments for `{setting} {action}`. Use the help command to see how it works.",
                $"admin: {setting} {action} <FAIL>");
        }
    }
}
