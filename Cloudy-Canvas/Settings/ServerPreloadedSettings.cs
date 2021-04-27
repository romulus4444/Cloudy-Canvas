namespace Cloudy_Canvas.Settings
{
    public class ServerPreloadedSettings
    {
        public ServerPreloadedSettings()
        {
            name = "";
            prefix = ';';
            listenToBots = false;
        }

        public string name { get; set; }
        public char prefix { get; set; }
        public bool listenToBots { get; set; }
    }
}
