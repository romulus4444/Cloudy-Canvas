namespace Cloudy_Canvas.Tests.Settings
{
    using System.Linq;
    using Cloudy_Canvas.Settings;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Xunit;

    public class ServerSettingsTests
    {
        // Exactly the shape earlier versions wrote: tuples serialise as Item1 / Item2.
        private const string LegacyJson = @"{
  ""Name"": ""Legacy Guild"",
  ""DefaultFilterId"": 175,
  ""AdminChannel"": 111,
  ""AdminRole"": 222,
  ""SpoilerList"": [
    { ""Item1"": 1001, ""Item2"": ""spoiler tag one"" },
    { ""Item1"": 1002, ""Item2"": ""artist:someone"" }
  ],
  ""WatchList"": [ ""breasts"" ],
  ""SafeMode"": true,
  ""FilteredChannels"": [
    { ""Item1"": 333, ""Item2"": 56027 }
  ]
}";

        [Fact]
        public void SettingsFilesWrittenByEarlierVersionsStillLoad()
        {
            var settings = JsonConvert.DeserializeObject<ServerSettings>(LegacyJson);

            Assert.Equal("Legacy Guild", settings.Name);
            Assert.Equal(2, settings.SpoilerList.Count);
            Assert.Equal(1001, settings.SpoilerList[0].Id);
            Assert.Equal("spoiler tag one", settings.SpoilerList[0].Name);
            Assert.Equal("artist:someone", settings.SpoilerList[1].Name);
            var filter = Assert.Single(settings.FilteredChannels);
            Assert.Equal(333UL, filter.ChannelId);
            Assert.Equal(56027, filter.FilterId);
            Assert.Equal(new[] { "breasts" }, settings.WatchList);
        }

        [Fact]
        public void SavedFilesUseTheNewPropertyNamesOnly()
        {
            var settings = JsonConvert.DeserializeObject<ServerSettings>(LegacyJson);

            var json = JObject.Parse(JsonConvert.SerializeObject(settings));

            var spoiler = (JObject)json["SpoilerList"][0];
            Assert.Equal(new[] { "Id", "Name" }, spoiler.Properties().Select(p => p.Name).OrderBy(n => n));
            var filter = (JObject)json["FilteredChannels"][0];
            Assert.Equal(new[] { "ChannelId", "FilterId" }, filter.Properties().Select(p => p.Name).OrderBy(n => n));
        }

        [Fact]
        public void NewShapeRoundTrips()
        {
            var settings = new ServerSettings();
            settings.SpoilerList.Add(new SpoilerTag(42, "some tag"));
            settings.FilteredChannels.Add(new ChannelFilter(7, 99));

            var roundTrip = JsonConvert.DeserializeObject<ServerSettings>(JsonConvert.SerializeObject(settings));

            Assert.Equal(42, roundTrip.SpoilerList.Single().Id);
            Assert.Equal("some tag", roundTrip.SpoilerList.Single().Name);
            Assert.Equal(7UL, roundTrip.FilteredChannels.Single().ChannelId);
            Assert.Equal(99, roundTrip.FilteredChannels.Single().FilterId);
        }

        [Fact]
        public void DeconstructionStillWorksForCallers()
        {
            var (id, name) = new SpoilerTag(5, "x");
            var (channel, filter) = new ChannelFilter(6, 7);

            Assert.Equal(5, id);
            Assert.Equal("x", name);
            Assert.Equal(6UL, channel);
            Assert.Equal(7, filter);
        }
    }
}
