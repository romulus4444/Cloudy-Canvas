namespace Cloudy_Canvas.Helpers
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public static class LogFile
    {
        // Commands run concurrently and log from several tasks at once. Opening one file for append from two of them at the same
        // time fails with a sharing violation on Windows, and two "first entry of the day" writers would both add the header.
        // Log volume is tiny, so a single gate for all log files is simplest.
        private static readonly SemaphoreSlim Gate = new(1, 1);

        /// <summary>Appends <paramref name="entry"/> to the file, preceded by <paramref name="header"/> if the file doesn't exist yet.</summary>
        public static async Task AppendAsync(string path, string header, string entry)
        {
            await Gate.WaitAsync();
            try
            {
                var text = File.Exists(path) ? entry : header + entry;
                await File.AppendAllTextAsync(path, text);
            }
            finally
            {
                Gate.Release();
            }
        }
    }
}
