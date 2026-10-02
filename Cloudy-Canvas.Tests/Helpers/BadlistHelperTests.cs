namespace Cloudy_Canvas.Tests.Helpers
{
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Settings;
    using Xunit;

    public class BadlistHelperTests
    {
        private static ServerSettings WithWatchList(params string[] terms)
        {
            var settings = new ServerSettings();
            settings.WatchList.AddRange(terms);
            return settings;
        }

        [Theory]
        [InlineData("breasts")]
        [InlineData("BREASTS")]
        [InlineData("twilight sparkle, breasts")]
        [InlineData("twilight sparkle,breasts")] // no space after the comma
        [InlineData("twilight sparkle ,  breasts")]
        [InlineData("(breasts)")]
        [InlineData("safe, (breasts)")]
        [InlineData("breasts || twilight sparkle")]
        [InlineData("twilight sparkle||breasts")]
        [InlineData("twilight sparkle && breasts")]
        [InlineData("twilight sparkle AND breasts")]
        [InlineData("twilight sparkle or breasts")]
        [InlineData("-breasts")]
        [InlineData("!breasts")]
        [InlineData("NOT breasts")]
        [InlineData("\"breasts\"")]
        [InlineData("*breasts*")]
        [InlineData("breasts*")]
        [InlineData("*breasts")]
        [InlineData("**breasts**")]
        [InlineData("((twilight sparkle) || (breasts))")]
        public void WatchedTermIsFoundHoweverItIsWritten(string query)
        {
            var result = BadlistHelper.CheckWatchList(query, WithWatchList("breasts"));
            Assert.Equal("breasts", result);
        }

        [Theory]
        [InlineData("twilight sparkle")]
        [InlineData("*")]
        [InlineData("safe, cute")]
        [InlineData("breasts of the rainbow")] // only whole terms match
        [InlineData("")]
        public void UnwatchedQueriesPass(string query)
        {
            Assert.Equal(string.Empty, BadlistHelper.CheckWatchList(query, WithWatchList("breasts")));
        }

        [Theory]
        [InlineData("123")]
        [InlineData("id:123")]
        [InlineData("-id:123")]
        [InlineData("twilight sparkle, id:123")]
        [InlineData("ID:123")]
        public void WatchedImageIdIsFoundInIdQueries(string query)
        {
            Assert.Equal("123", BadlistHelper.CheckWatchList(query, WithWatchList("123")));
        }

        [Fact]
        public void MultiWordAndParenthesisedWatchTermsStillMatch()
        {
            var settings = WithWatchList("black and white", "applejack (g4)");
            Assert.Equal("black and white", BadlistHelper.CheckWatchList("cute, black and white", settings));
            Assert.Equal("applejack (g4)", BadlistHelper.CheckWatchList("applejack (g4), safe", settings));
        }

        [Fact]
        public void EveryMatchedTermIsReportedOnce()
        {
            var settings = WithWatchList("breasts", "nipples");
            var result = BadlistHelper.CheckWatchList("breasts, nipples, breasts", settings);
            Assert.Equal("breasts, nipples", result);
        }

        [Fact]
        public void EmptyWatchListMatchesNothing()
        {
            Assert.Equal(string.Empty, BadlistHelper.CheckWatchList("anything at all", new ServerSettings()));
        }
    }
}
