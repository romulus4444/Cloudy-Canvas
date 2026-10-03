namespace Cloudy_Canvas.Helpers
{
    using System;
    using Cloudy_Canvas.Settings;

    /// <summary>What the bot does with a message once it is known to start with the prefix or a mention of the bot.</summary>
    public static class CommandRouting
    {
        /// <summary>Stands in for a message that is only the prefix.</summary>
        public const string BlankMessage = "<blank message>";

        /// <summary>Stands in for a message that starts by mentioning the bot.</summary>
        public const string Mention = "<mention>";

        /// <summary>
        /// Decides what command text to run for a message, or null if the bot should stay silent.
        /// </summary>
        /// <param name="startsWithPrefixOrMention">True if the message begins with the server's prefix or with a mention of the bot.</param>
        /// <param name="startsWithMention">True if it begins with a mention of the bot.</param>
        /// <param name="authorIsBot">True if another bot sent it.</param>
        /// <param name="content">The whole message, prefix included.</param>
        /// <param name="settings">The server's settings (aliases, whether to listen to other bots).</param>
        /// <param name="isCommand">Tells whether some text is a command the bot knows.</param>
        public static string Resolve(bool startsWithPrefixOrMention, bool startsWithMention, bool authorIsBot, string content, ServerPreloadedSettings settings, Func<string, bool> isCommand)
        {
            if (!startsWithPrefixOrMention)
            {
                return null;
            }

            if (!settings.ListenToBots && authorIsBot)
            {
                return null;
            }

            // A mention gets its own reply whatever follows it; it is not run as a command.
            if (startsWithMention)
            {
                return Mention;
            }

            var parsed = DiscordHelper.ResolveAliases(content, settings);
            if (parsed.Length == 0)
            {
                return BlankMessage;
            }

            // Stay quiet on unknown commands so the bot doesn't answer every message that happens to start with the prefix
            // (other bots on the server often share it).
            return isCommand(parsed) ? parsed : null;
        }
    }
}
