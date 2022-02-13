namespace Cloudy_Canvas.Settings
{
    using System.Collections.Generic;

    public class AllPreloadedSettings
    {
        public AllPreloadedSettings()
        {
            Settings = new Dictionary<ulong, ServerPreloadedSettings>();
            GuildList = new Dictionary<ulong, ulong>();
        }

        public Dictionary<ulong, ServerPreloadedSettings> Settings { get; set; }
        public Dictionary<ulong, ulong> GuildList { get; set; }
    }
}
