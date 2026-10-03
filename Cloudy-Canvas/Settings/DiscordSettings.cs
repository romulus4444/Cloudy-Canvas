namespace Cloudy_Canvas.Settings
{
    using System.Collections.Generic;

    public class DiscordSettings
    {
        public string token { get; set; }

        /// <summary>Discord user ids allowed to use the owner-only <c>broadcast</c> command. Empty disables it.</summary>
        public List<ulong> BroadcastUserIds { get; set; } = new();
    }
}
