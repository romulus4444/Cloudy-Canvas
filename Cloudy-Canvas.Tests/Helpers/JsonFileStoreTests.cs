namespace Cloudy_Canvas.Tests.Helpers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Settings;
    using Newtonsoft.Json;
    using Xunit;

    public sealed class JsonFileStoreTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "cloudy-canvas-tests-" + Guid.NewGuid().ToString("N"));

        public JsonFileStoreTests()
        {
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            Directory.Delete(_dir, true);
        }

        private string PathFor(string name = "settings.conf") => Path.Combine(_dir, name);

        [Fact]
        public async Task MissingFileIsCreatedWithDefaults()
        {
            var store = new JsonFileStore<ServerSettings>(true);

            var settings = await store.LoadAsync(PathFor());

            Assert.Equal(175, settings.DefaultFilterId);
            Assert.True(File.Exists(PathFor()));
            Assert.Equal(175, JsonConvert.DeserializeObject<ServerSettings>(File.ReadAllText(PathFor())).DefaultFilterId);
        }

        [Fact]
        public async Task EveryLoadReturnsTheSameSharedInstance()
        {
            var store = new JsonFileStore<ServerSettings>(true);

            var first = await store.LoadAsync(PathFor());
            var second = await store.LoadAsync(PathFor());

            Assert.Same(first, second);
        }

        [Fact]
        public async Task SavedChangesAreWrittenAndSurviveANewStore()
        {
            var store = new JsonFileStore<ServerSettings>(true);
            var settings = await store.LoadAsync(PathFor());
            settings.Name = "Test Guild";
            settings.WatchList.Add("breasts");

            await store.SaveAsync(PathFor(), settings);

            var reloaded = await new JsonFileStore<ServerSettings>(true).LoadAsync(PathFor());
            Assert.Equal("Test Guild", reloaded.Name);
            Assert.Equal(new[] { "breasts" }, reloaded.WatchList);
            Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
        }

        [Fact]
        public async Task SavingDoesNotCauseTheNextLoadToReplaceTheInstance()
        {
            var store = new JsonFileStore<ServerSettings>(true);
            var settings = await store.LoadAsync(PathFor());
            settings.Name = "Changed";
            await store.SaveAsync(PathFor(), settings);

            Assert.Same(settings, await store.LoadAsync(PathFor()));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("null")]
        [InlineData("{ \"Name\": \"Half writ")] // truncated by a crash
        [InlineData("not json at all")]
        [InlineData("{ \"DefaultFilterId\": \"abc\" }")] // wrong type
        public async Task CorruptFileIsMovedAsideAndReplacedByDefaults(string contents)
        {
            await File.WriteAllTextAsync(PathFor(), contents);
            var store = new JsonFileStore<ServerSettings>(true);

            var settings = await store.LoadAsync(PathFor());

            Assert.NotNull(settings);
            Assert.Equal(175, settings.DefaultFilterId);
            var backups = Directory.GetFiles(_dir, "settings.conf.corrupt-*");
            Assert.Single(backups);
            Assert.Equal(contents, File.ReadAllText(backups[0]));
            Assert.Equal(175, JsonConvert.DeserializeObject<ServerSettings>(File.ReadAllText(PathFor())).DefaultFilterId);
        }

        [Fact]
        public async Task FileEditedByHandIsReloadedWhenEnabled()
        {
            var store = new JsonFileStore<ServerSettings>(true);
            await store.LoadAsync(PathFor());

            File.WriteAllText(PathFor(), "{ \"Name\": \"Edited by hand\" }");
            File.SetLastWriteTimeUtc(PathFor(), DateTime.UtcNow.AddMinutes(5));

            Assert.Equal("Edited by hand", (await store.LoadAsync(PathFor())).Name);
        }

        [Fact]
        public async Task FileEditedByHandIsIgnoredWhenReloadIsDisabled()
        {
            var store = new JsonFileStore<ServerSettings>(false);
            var original = await store.LoadAsync(PathFor());

            File.WriteAllText(PathFor(), "{ \"Name\": \"Edited by hand\" }");
            File.SetLastWriteTimeUtc(PathFor(), DateTime.UtcNow.AddMinutes(5));

            Assert.Same(original, await store.LoadAsync(PathFor()));
        }

        [Fact]
        public async Task FileDeletedWhileRunningIsRecreated()
        {
            var store = new JsonFileStore<ServerSettings>(true);
            await store.LoadAsync(PathFor());
            File.Delete(PathFor());

            var settings = await store.LoadAsync(PathFor());

            Assert.NotNull(settings);
            Assert.True(File.Exists(PathFor()));
        }

        [Fact]
        public async Task ConcurrentSavesAndLoadsNeverExposeAPartialFile()
        {
            var store = new JsonFileStore<ServerSettings>(true);
            var settings = await store.LoadAsync(PathFor());
            var path = PathFor();

            var writers = Enumerable.Range(0, 40).Select(i => Task.Run(async () =>
            {
                settings.Name = new string('x', 1000 + i); // big enough that a torn write would be visible
                await store.SaveAsync(path, settings);
            }));

            // Readers go straight to the file, bypassing the store, the way a crash or another process would see it.
            // A read can legitimately fail for a moment while the file is being replaced; what must never happen is
            // reading successfully and getting a partial document.
            var readers = Enumerable.Range(0, 40).Select(_ => Task.Run(() =>
            {
                string text;
                try
                {
                    text = File.ReadAllText(path);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    return;
                }

                Assert.NotNull(JsonConvert.DeserializeObject<ServerSettings>(text));
            }));

            await Task.WhenAll(writers.Concat(readers));

            Assert.NotNull(JsonConvert.DeserializeObject<ServerSettings>(File.ReadAllText(path)));
            Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
        }

        [Fact]
        public async Task ConcurrentLoadsOfAMissingFileAllGetOneInstance()
        {
            var store = new JsonFileStore<ServerSettings>(true);

            var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => store.LoadAsync(PathFor())));

            Assert.Single(results.Distinct());
        }

        [Fact]
        public async Task DifferentFilesAreIndependent()
        {
            var store = new JsonFileStore<ServerSettings>(true);

            var a = await store.LoadAsync(PathFor("a.conf"));
            var b = await store.LoadAsync(PathFor("b.conf"));
            a.Name = "A";

            Assert.NotSame(a, b);
            Assert.Equal(string.Empty, b.Name);
        }
    }
}
