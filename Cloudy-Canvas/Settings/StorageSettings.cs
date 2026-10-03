namespace Cloudy_Canvas.Settings
{
    public class StorageSettings
    {
        public const string DefaultRootPath = "botsettings";

        /// <summary>
        /// The folder that holds every server's settings and logs. A relative path is relative to the working directory the bot is started in.
        /// Use a different folder (for example "devsettings") to run a development copy of the bot without touching production data.
        /// </summary>
        public string RootPath { get; set; } = DefaultRootPath;
    }
}
