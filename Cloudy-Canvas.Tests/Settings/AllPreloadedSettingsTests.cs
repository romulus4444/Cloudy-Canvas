namespace Cloudy_Canvas.Tests.Settings
{
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Settings;
    using Newtonsoft.Json;
    using Xunit;

    public class AllPreloadedSettingsTests
    {
        [Fact]
        public void ExistingSettingsFilesStillLoad()
        {
            // The exact shape written by earlier versions, when these were plain dictionaries.
            const string json = @"{
  ""Settings"": {
    ""123"": { ""Name"": ""Guild A"", ""Prefix"": ""!"", ""ListenToBots"": true, ""Aliases"": { ""cute"": ""pick cute"" } }
  },
  ""GuildList"": { ""123"": 456 }
}";

            var loaded = JsonConvert.DeserializeObject<AllPreloadedSettings>(json);

            Assert.NotNull(loaded);
            Assert.Equal('!', loaded.Settings[123].Prefix);
            Assert.True(loaded.Settings[123].ListenToBots);
            Assert.Equal("pick cute", loaded.Settings[123].Aliases["cute"]);
            Assert.Equal(456UL, loaded.GuildList[123]);
        }

        [Fact]
        public void SerialisedShapeIsUnchanged()
        {
            var settings = new AllPreloadedSettings();
            settings.Settings[7] = new ServerPreloadedSettings { Name = "Seven" };
            settings.GuildList[7] = 8;

            var json = JsonConvert.SerializeObject(settings);
            var roundTrip = JsonConvert.DeserializeObject<AllPreloadedSettings>(json);

            Assert.Contains("\"7\"", json);
            Assert.Equal("Seven", roundTrip.Settings[7].Name);
            Assert.Equal(8UL, roundTrip.GuildList[7]);
        }

        [Fact]
        public async Task ConcurrentUpdatesDoNotLoseOrCorruptEntries()
        {
            var settings = new AllPreloadedSettings();

            await Task.WhenAll(Enumerable.Range(1, 200).Select(i => Task.Run(() =>
            {
                settings.Settings[(ulong)i] = new ServerPreloadedSettings { Name = $"Guild {i}" };
                settings.GuildList[(ulong)i] = (ulong)i * 10;
            })));

            Assert.Equal(200, settings.Settings.Count);
            Assert.Equal(200, settings.GuildList.Count);
        }
    }
}
