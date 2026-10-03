namespace Cloudy_Canvas.Tests.Settings
{
    using System;
    using System.IO;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Settings;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Xunit;

    public class StorageSettingsTests
    {
        [Fact]
        public void TheDefaultIsTheFolderTheBotAlwaysUsed()
        {
            Assert.Equal("botsettings", new StorageSettings().RootPath);
            Assert.Equal("botsettings", StorageSettings.DefaultRootPath);
            using var provider = TestServices.Build();

            Assert.Equal("botsettings", provider.GetRequiredService<IOptions<StorageSettings>>().Value.RootPath);
        }

        [Theory]
        [InlineData("devsettings")]
        [InlineData("/var/lib/cloudy-canvas/data")]
        [InlineData("C:\\Bots\\Cloudy\\data")]
        public void TheRootPathComesFromConfiguration(string path)
        {
            using var provider = TestServices.Build(("Storage:RootPath", path));

            Assert.Equal(path, provider.GetRequiredService<IOptions<StorageSettings>>().Value.RootPath);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ABlankRootPathIsRejected(string path)
        {
            using var provider = TestServices.Build(("Storage:RootPath", path));

            var error = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<StorageSettings>>().Value);
            Assert.Contains("Storage:RootPath", error.Message);
        }
    }

    // FileHelper.RootPath is process-wide state, so the tests that change it must not run alongside others that read it.
    [CollectionDefinition("FileHelper root path", DisableParallelization = true)]
    public class FileHelperRootPathCollection
    {
    }

    [Collection("FileHelper root path")]
    public sealed class FileHelperRootPathTests : IDisposable
    {
        private readonly string _original = FileHelper.RootPath;
        private readonly string _temp = Path.Combine(Path.GetTempPath(), "cloudy-canvas-root-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            FileHelper.UseRootPath(_original);
            if (Directory.Exists(_temp))
            {
                Directory.Delete(_temp, true);
            }
        }

        [Fact]
        public void FilesAreKeptUnderTheConfiguredRoot()
        {
            FileHelper.UseRootPath(_temp);

            var presettings = FileHelper.SetUpFilepath(FilePathType.Root, "preloadedsettings", "conf");

            Assert.Equal(Path.Join(_temp, "preloadedsettings.conf"), presettings);
            Assert.True(Directory.Exists(_temp)); // created on demand, as before
        }

        [Fact]
        public void ServerFilesGoInAServersFolderUnderTheRoot()
        {
            FileHelper.UseRootPath(_temp);

            var path = FileHelper.SetUpFilepath(FilePathType.Server, "settings", "conf");

            Assert.Equal(Path.Join(_temp, "servers", "settings.conf"), path);
        }

        [Fact]
        public void ChangingTheRootChangesWhereNewFilesGo()
        {
            var first = Path.Combine(_temp, "one");
            var second = Path.Combine(_temp, "two");

            FileHelper.UseRootPath(first);
            var a = FileHelper.SetUpFilepath(FilePathType.Root, "x", "conf");
            FileHelper.UseRootPath(second);
            var b = FileHelper.SetUpFilepath(FilePathType.Root, "x", "conf");

            Assert.StartsWith(first, a);
            Assert.StartsWith(second, b);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(null)]
        public void ABlankRootIsRefusedAndTheOldOneKept(string root)
        {
            FileHelper.UseRootPath(_temp);

            Assert.Throws<ArgumentException>(() => FileHelper.UseRootPath(root));

            Assert.Equal(_temp, FileHelper.RootPath);
        }
    }
}
