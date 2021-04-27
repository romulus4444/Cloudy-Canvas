namespace Cloudy_Canvas.Settings
{
    using System.Collections.Generic;

    public class ServerPreloadedSettings
    {
        public string name { get; set; }
        public char prefix { get; set; }
        public bool listenToBots { get; set; }
        public List<Dictionary<string, string>> aliases { get; set; }

        public ServerPreloadedSettings()
        {
            name = "";
            prefix = ';';
            listenToBots = false;
            aliases = new List<Dictionary<string, string>>();
        }
    }
}
