namespace Cloudy_Canvas.Settings
{
    using System.Collections.Generic;

    public class AllPreloadedSettings
    {
        public AllPreloadedSettings()
        {
            settings = new Dictionary<ulong, ServerPreloadedSettings>();
        }

        public Dictionary<ulong, ServerPreloadedSettings> settings { get; set; }
    }
}
