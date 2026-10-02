namespace Cloudy_Canvas.Tests.Helpers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Xunit;

    public sealed class LogFileTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "cloudy-canvas-logs-" + Guid.NewGuid().ToString("N"));

        public LogFileTests()
        {
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            Directory.Delete(_dir, true);
        }

        [Fact]
        public async Task FirstEntryGetsTheHeaderAndLaterOnesDoNot()
        {
            var path = Path.Combine(_dir, "2026-10-01.log");

            await LogFile.AppendAsync(path, "HEADER\n", "one\n");
            await LogFile.AppendAsync(path, "HEADER\n", "two\n");

            Assert.Equal("HEADER\none\ntwo\n", File.ReadAllText(path));
        }

        [Fact]
        public async Task ConcurrentAppendsAllLandWithOneHeaderAndNoErrors()
        {
            var path = Path.Combine(_dir, "busy.log");

            await Task.WhenAll(Enumerable.Range(0, 100).Select(i => Task.Run(() => LogFile.AppendAsync(path, "HEADER\n", $"entry {i}\n"))));

            var lines = File.ReadAllLines(path);
            Assert.Equal(101, lines.Length);
            Assert.Equal("HEADER", lines[0]);
            Assert.Equal(1, lines.Count(l => l == "HEADER"));
            Assert.Equal(100, lines.Skip(1).Distinct().Count()); // every entry present exactly once, none torn
        }
    }
}
