namespace Cloudy_Canvas.Settings
{
    public class LogRetentionSettings
    {
        /// <summary>
        /// Per-channel log files older than this many days are deleted. The logs record usernames, user ids and the queries people
        /// run. Zero (the default) keeps them forever.
        /// </summary>
        public int RetentionDays { get; set; }
    }
}
