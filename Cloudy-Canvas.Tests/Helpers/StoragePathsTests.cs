namespace Cloudy_Canvas.Tests.Helpers
{
    using System;
    using System.IO;
    using System.Linq;
    using Cloudy_Canvas.Helpers;
    using Xunit;

    public sealed class StoragePathsTests : IDisposable
    {
        private static readonly DateTime Now = new(2026, 10, 1, 23, 59, 0, DateTimeKind.Utc);
        private static readonly StorageScope Guild = new(UserId: 42, GuildId: 111, ChannelId: 222);
        private static readonly StorageScope DirectMessage = new(UserId: 42, GuildId: null, ChannelId: 333);

        private readonly string _root = Path.Combine(Path.GetTempPath(), "cloudy-canvas-paths-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        private string Build(FilePathType type, string filename, string extension, StorageScope scope = null, string logChannel = "", string date = "")
        {
            return StoragePaths.Build(_root, type, filename, extension, scope, logChannel, date, Now);
        }

        private string Expected(params string[] parts) => Path.Combine(new[] { _root }.Concat(parts).ToArray());

        [Fact]
        public void FilesOfNoServerLiveDirectlyUnderTheRoot()
        {
            Assert.Equal(Expected("preloadedsettings.conf"), Build(FilePathType.Root, "preloadedsettings", "conf"));
        }

        [Fact]
        public void AServersSettingsLiveInItsOwnFolder()
        {
            Assert.Equal(Expected("servers", "111", "settings.conf"), Build(FilePathType.Server, "settings", "conf", Guild));
        }

        [Fact]
        public void ChannelLogsAreNamedByUtcDateInTheChannelsFolder()
        {
            Assert.Equal(Expected("servers", "111", "222", "2026-10-01.log"), Build(FilePathType.Channel, "<date>", "log", Guild));
        }

        [Fact]
        public void AnEmptyFileNameMeansDefault()
        {
            Assert.Equal(Expected("default.txt"), Build(FilePathType.Root, string.Empty, "txt"));
        }

        [Theory]
        [InlineData(FilePathType.Server)]
        [InlineData(FilePathType.Channel)]
        public void DirectMessagesAreKeptPerUserWhateverTheKind(FilePathType type)
        {
            Assert.Equal(Expected("servers", "_userdms", "42", "settings.conf"), Build(type, "settings", "conf", DirectMessage));
        }

        [Fact]
        public void WithoutAScopeAServerFileSitsInTheServersFolder()
        {
            Assert.Equal(Expected("servers", "settings.conf"), Build(FilePathType.Server, "settings", "conf"));
        }

        [Fact]
        public void BuildingAPathCreatesTheFoldersOnTheWay()
        {
            Build(FilePathType.Channel, "<date>", "log", Guild);

            Assert.True(Directory.Exists(Expected("servers", "111", "222")));
        }

        // ---- reading a log back: what an admin typed must stay inside the server's folder ----------------------------

        [Fact]
        public void ALogIsLookedUpByChannelFolderAndDate()
        {
            var path = Build(FilePathType.LogRetrieval, "ignored", "log", Guild, "222", "2026-09-30");

            Assert.Equal(Expected("servers", "111", "222", "2026-09-30.log"), path);
        }

        [Theory]
        [InlineData("..", "2026-10-01")]
        [InlineData("../222", "2026-10-01")]
        [InlineData("..\\222", "2026-10-01")]
        [InlineData("222/..", "2026-10-01")]
        [InlineData("/etc", "2026-10-01")]
        [InlineData("C:\\Windows", "2026-10-01")]
        [InlineData("222", "../../999/222/2026-10-01")]
        [InlineData("222", "..")]
        [InlineData("222", "2026-10-01/../../..")]
        [InlineData("222", "2026-10-01.log")]
        [InlineData("2.2", "2026-10-01")]
        [InlineData("222 ", "2026-10-01")]
        [InlineData("", "2026-10-01")]
        [InlineData("222", "")]
        [InlineData(null, "2026-10-01")]
        [InlineData("222", null)]
        [InlineData("22\u00e9", "2026-10-01")]
        [InlineData("222\0", "2026-10-01")]
        public void ALogLookupThatCouldLeaveTheServersFolderIsRefused(string logChannel, string date)
        {
            Assert.Throws<ArgumentException>(() => Build(FilePathType.LogRetrieval, "ignored", "log", Guild, logChannel, date));
        }

        [Fact]
        public void ARefusedLogLookupCreatesNothingOutsideTheServersFolder()
        {
            Assert.Throws<ArgumentException>(() => Build(FilePathType.LogRetrieval, "ignored", "log", Guild, "../../elsewhere", "2026-10-01"));

            Assert.False(Directory.Exists(Path.Combine(_root, "elsewhere")));
            Assert.False(Directory.Exists(Path.Combine(_root, "servers", "elsewhere")));
            Assert.Empty(Directory.GetDirectories(Expected("servers", "111")));
        }

        [Theory]
        [InlineData("222", "2026-10-01")]
        [InlineData("general-chat", "2026-10-01")]
        [InlineData("a_b", "x-y_z")]
        [InlineData("0", "0")]
        public void EveryAcceptedLogLookupStaysUnderItsServersFolder(string logChannel, string date)
        {
            var path = Build(FilePathType.LogRetrieval, "ignored", "log", Guild, logChannel, date);

            var serverFolder = Path.GetFullPath(Expected("servers", "111")) + Path.DirectorySeparatorChar;
            Assert.StartsWith(serverFolder, Path.GetFullPath(path), StringComparison.Ordinal);
        }

        [Fact]
        public void TwoServersNeverShareAFolder()
        {
            var first = Build(FilePathType.Server, "settings", "conf", new StorageScope(1, 111, 5));
            var second = Build(FilePathType.Server, "settings", "conf", new StorageScope(1, 1111, 5));
            var dm = Build(FilePathType.Server, "settings", "conf", new StorageScope(111, null, 5));

            Assert.Equal(3, new[] { first, second, dm }.Distinct().Count());
        }

        [Fact]
        public void ADirectMessageLogLookupIsNotAnEscapeRouteEither()
        {
            // In a DM the log arguments are not used at all, so whatever was typed can't reach the file system.
            var path = Build(FilePathType.LogRetrieval, "settings", "conf", DirectMessage, "../../x", "../y");

            Assert.Equal(Expected("servers", "_userdms", "42", "settings.conf"), path);
        }
    }
}
