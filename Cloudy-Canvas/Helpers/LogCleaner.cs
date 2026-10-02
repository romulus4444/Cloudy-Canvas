namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.IO;

    public static class LogCleaner
    {
        /// <summary>
        /// Deletes the per-channel <c>*.log</c> files under <paramref name="serversDirectory"/> that were last written more than
        /// <paramref name="retentionDays"/> days before <paramref name="now"/>. Nothing else (such as settings files) is touched.
        /// A retention of zero or less keeps everything. Returns the number of files deleted.
        /// </summary>
        public static int DeleteOldLogs(string serversDirectory, int retentionDays, DateTime now)
        {
            if (retentionDays <= 0 || !Directory.Exists(serversDirectory))
            {
                return 0;
            }

            var cutoff = now.ToUniversalTime().AddDays(-retentionDays);
            var deleted = 0;
            foreach (var file in Directory.EnumerateFiles(serversDirectory, "*.log", SearchOption.AllDirectories))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < cutoff)
                    {
                        File.Delete(file);
                        deleted++;
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // In use or not allowed right now; it will be picked up on the next pass.
                }
            }

            return deleted;
        }
    }
}
