namespace Cloudy_Canvas.Tests.Helpers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Xunit;

    public sealed class LogCleanerTests : IDisposable
    {
        private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        private readonly string _servers = Path.Combine(Path.GetTempPath(), "cloudy-canvas-clean-" + Guid.NewGuid().ToString("N"), "servers");

        public LogCleanerTests()
        {
            Directory.CreateDirectory(Path.Combine(_servers, "111", "222"));
        }

        public void Dispose()
        {
            Directory.Delete(Path.GetDirectoryName(_servers)!, true);
        }

        private string Make(string relative, int daysOld)
        {
            var path = Path.Combine(_servers, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "x");
            File.SetLastWriteTimeUtc(path, Now.AddDays(-daysOld));
            return path;
        }

        [Fact]
        public void OnlyLogsOlderThanTheRetentionAreDeleted()
        {
            var old = Make(Path.Combine("111", "222", "2026-06-01.log"), 120);
            var recent = Make(Path.Combine("111", "222", "2026-09-25.log"), 6);
            var edge = Make(Path.Combine("111", "333", "edge.log"), 89);

            var deleted = LogCleaner.DeleteOldLogs(_servers, 90, Now);

            Assert.Equal(1, deleted);
            Assert.False(File.Exists(old));
            Assert.True(File.Exists(recent));
            Assert.True(File.Exists(edge));
        }

        [Fact]
        public void SettingsAndOtherFilesAreNeverDeleted()
        {
            var settings = Make(Path.Combine("111", "settings.conf"), 1000);
            var notes = Make(Path.Combine("111", "notes.txt"), 1000);
            var backup = Make(Path.Combine("111", "settings.conf.corrupt-20260101000000"), 1000);

            Assert.Equal(0, LogCleaner.DeleteOldLogs(_servers, 30, Now));

            Assert.True(File.Exists(settings));
            Assert.True(File.Exists(notes));
            Assert.True(File.Exists(backup));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void ZeroOrNegativeRetentionKeepsEverything(int days)
        {
            var old = Make("111/222/ancient.log", 5000);

            Assert.Equal(0, LogCleaner.DeleteOldLogs(_servers, days, Now));
            Assert.True(File.Exists(old));
        }

        [Fact]
        public void AMissingDirectoryIsFine()
        {
            Assert.Equal(0, LogCleaner.DeleteOldLogs(Path.Combine(_servers, "does-not-exist"), 30, Now));
        }
    }
}
