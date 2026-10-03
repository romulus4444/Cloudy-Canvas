namespace Cloudy_Canvas.Settings
{
    using System.Collections.Concurrent;

    public class AllPreloadedSettings
    {
        public AllPreloadedSettings()
        {
            Settings = new ConcurrentDictionary<ulong, ServerPreloadedSettings>();
            GuildList = new ConcurrentDictionary<ulong, ulong>();
        }

        // Shared by every gateway handler and command, so these must tolerate concurrent access.
        public ConcurrentDictionary<ulong, ServerPreloadedSettings> Settings { get; set; }
        public ConcurrentDictionary<ulong, ulong> GuildList { get; set; }
    }
}
