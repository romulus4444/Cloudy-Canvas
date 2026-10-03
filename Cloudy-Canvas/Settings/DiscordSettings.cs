namespace Cloudy_Canvas.Settings
{
    using System.Collections.Generic;

    public class DiscordSettings
    {
        public string token { get; set; }

        /// <summary>
        /// When set, this character is the command prefix in every server, instead of each server's own prefix. For running a development
        /// copy of the bot next to the production one, so they don't both answer the same commands. Leave unset in production.
        /// </summary>
        public char? PrefixOverride { get; set; }

        /// <summary>Discord user ids allowed to use the owner-only <c>broadcast</c> command. Empty disables it.</summary>
        public List<ulong> BroadcastUserIds { get; set; } = new();
    }
}
