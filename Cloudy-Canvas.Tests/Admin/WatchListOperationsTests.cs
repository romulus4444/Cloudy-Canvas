namespace Cloudy_Canvas.Tests.Admin
{
    using System;
    using System.Linq;
    using Cloudy_Canvas.Admin;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Settings;
    using Xunit;

    public class WatchListOperationsTests
    {
        private static ServerSettings WithWatchList(params string[] terms)
        {
            var settings = new ServerSettings();
            settings.WatchList.AddRange(terms);
            return settings;
        }

        // ---- add -----------------------------------------------------------------------------------------------------

        [Fact]
        public void AddingATermStoresItAndSaysSo()
        {
            var settings = WithWatchList();

            var result = WatchListOperations.Run("add", "breasts", settings);

            Assert.Equal(new[] { "breasts" }, settings.WatchList);
            Assert.Equal("Added `breasts` to the watchlist.", result.Message);
            Assert.Equal("watchlist: add `breasts` <SUCCESS>", result.LogText);
            Assert.True(result.Changed);
            Assert.True(result.LogToFile);
        }

        [Fact]
        public void ACommaSeparatedListAddsEachTermLowerCasedAndTrimmed()
        {
            var settings = WithWatchList();

            var result = WatchListOperations.Run("add", "  Breasts ,NUDITY,  twilight sparkle ,,", settings);

            Assert.Equal(new[] { "breasts", "nudity", "twilight sparkle" }, settings.WatchList);
            Assert.Equal("Added `breasts`, `nudity`, and `twilight sparkle` to the watchlist.", result.Message);
        }

        [Fact]
        public void TermsAlreadyOnTheListAreReportedAndNotAddedTwice()
        {
            var settings = WithWatchList("breasts");

            var result = WatchListOperations.Run("add", "BREASTS", settings);

            Assert.Equal(new[] { "breasts" }, settings.WatchList);
            Assert.Equal("All terms entered are already on the watchlist.", result.Message);
            Assert.Equal("watchlist: add <FAIL> BREASTS", result.LogText);
            Assert.False(result.Changed);
            Assert.False(result.LogToFile);
        }

        [Fact]
        public void SomeNewAndSomeKnownTermsSaveTheNewOnesAndNameTheKnownOnes()
        {
            var settings = WithWatchList("breasts");

            var result = WatchListOperations.Run("add", "nudity, breasts", settings);

            Assert.Equal(new[] { "breasts", "nudity" }, settings.WatchList);
            Assert.Equal("Added `nudity` to the watchlist, and the watchlist already contained `breasts`.", result.Message);
            Assert.Equal("watchlist: add `nudity` <FAIL> `breasts`", result.LogText);
            Assert.True(result.Changed);
            Assert.False(result.LogToFile); // a partial add is logged to the console only, as before
        }

        [Fact]
        public void ARepeatWithinOneCommandIsAddedOnceAndReported()
        {
            var settings = WithWatchList();

            var result = WatchListOperations.Run("add", "nudity, nudity", settings);

            Assert.Equal(new[] { "nudity" }, settings.WatchList);
            Assert.Equal("Added `nudity` to the watchlist, and the watchlist already contained `nudity`.", result.Message);
        }

        [Fact]
        public void AnEntryEditedIntoUpperCaseByHandStillCountsAsPresent()
        {
            var settings = WithWatchList("Breasts");

            var result = WatchListOperations.Run("add", "breasts", settings);

            Assert.Equal(new[] { "Breasts" }, settings.WatchList);
            Assert.False(result.Changed);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(" , ,, ")]
        [InlineData(null)]
        public void AddingNothingIsAnErrorNotASuccessfulAddOfNothing(string typed)
        {
            var settings = WithWatchList("breasts");

            var result = WatchListOperations.Run("add", typed, settings);

            Assert.Equal("You must specify a term to add.", result.Message);
            Assert.Equal("watchlist: add <FAIL>", result.LogText);
            Assert.False(result.Changed);
            Assert.Equal(new[] { "breasts" }, settings.WatchList);
        }

        // ---- remove --------------------------------------------------------------------------------------------------

        [Fact]
        public void RemovingATermTakesItOffTheList()
        {
            var settings = WithWatchList("breasts", "nudity");

            var result = WatchListOperations.Run("remove", "breasts", settings);

            Assert.Equal(new[] { "nudity" }, settings.WatchList);
            Assert.Equal("Removed `breasts` from the watchlist.", result.Message);
            Assert.Equal("watchlist: remove breasts <SUCCESS>", result.LogText);
            Assert.True(result.Changed);
            Assert.True(result.LogToFile);
        }

        [Theory]
        [InlineData("BREASTS")]
        [InlineData("  breasts  ")]
        public void RemovingIgnoresCaseAndSurroundingSpaces(string typed)
        {
            var settings = WithWatchList("breasts");

            var result = WatchListOperations.Run("remove", typed, settings);

            Assert.Empty(settings.WatchList);
            Assert.StartsWith("Removed `", result.Message);
        }

        [Fact]
        public void RemovingATermThatIsNotThereChangesNothing()
        {
            var settings = WithWatchList("breasts");

            var result = WatchListOperations.Run("remove", "nudity", settings);

            Assert.Equal(new[] { "breasts" }, settings.WatchList);
            Assert.Equal("`nudity` was not on the watchlist.", result.Message);
            Assert.Equal("watchlist: remove nudity <FAIL>", result.LogText);
            Assert.False(result.Changed);
            Assert.False(result.LogToFile);
        }

        [Fact]
        public void EveryCopyOfADuplicatedEntryIsRemoved()
        {
            var settings = WithWatchList("breasts", "nudity", "breasts");

            WatchListOperations.Run("remove", "breasts", settings);

            Assert.Equal(new[] { "nudity" }, settings.WatchList);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void RemovingNothingIsAnError(string typed)
        {
            var settings = WithWatchList("breasts");

            var result = WatchListOperations.Run("remove", typed, settings);

            Assert.Equal("You must specify a term to remove.", result.Message);
            Assert.False(result.Changed);
            Assert.Equal(new[] { "breasts" }, settings.WatchList);
        }

        // ---- get and clear -------------------------------------------------------------------------------------------

        [Fact]
        public void GetListsTheTermsInCode()
        {
            var result = WatchListOperations.Run("get", string.Empty, WithWatchList("breasts", "nudity"));

            Assert.Equal($"__Watchlist Terms:__{Environment.NewLine}`breasts`, `nudity`", result.Message);
            Assert.Equal("watchlist: get", result.LogText);
            Assert.False(result.Changed);
        }

        [Fact]
        public void GetSaysWhenTheListIsEmpty()
        {
            var result = WatchListOperations.Run("get", string.Empty, WithWatchList());

            Assert.Equal($"__Watchlist Terms:__{Environment.NewLine}The watchlist is currently empty.", result.Message);
        }

        [Fact]
        public void ClearEmptiesTheList()
        {
            var settings = WithWatchList("breasts", "nudity");

            var result = WatchListOperations.Run("clear", string.Empty, settings);

            Assert.Empty(settings.WatchList);
            Assert.Equal("Watchlist cleared", result.Message);
            Assert.Equal("watchlist: clear", result.LogText);
            Assert.True(result.Changed);
            Assert.True(result.LogToFile);
        }

        // ---- subcommands ---------------------------------------------------------------------------------------------

        [Fact]
        public void NoSubcommandIsAnError()
        {
            var result = WatchListOperations.Run(string.Empty, string.Empty, WithWatchList("breasts"));

            Assert.Equal("You must specify a subcommand.", result.Message);
            Assert.Equal("watchlist: <FAIL>", result.LogText);
            Assert.False(result.Changed);
        }

        [Theory]
        [InlineData("ADD")]
        [InlineData("delete")]
        [InlineData("list")]
        public void AnUnknownSubcommandIsAnErrorAndChangesNothing(string command)
        {
            var settings = WithWatchList("breasts");

            var result = WatchListOperations.Run(command, "nudity", settings);

            Assert.Equal("Invalid subcommand", result.Message);
            Assert.Equal($"watchlist: {command} <FAIL>", result.LogText);
            Assert.False(result.Changed);
            Assert.Equal(new[] { "breasts" }, settings.WatchList);
        }

        // ---- the words of a list -------------------------------------------------------------------------------------

        [Theory]
        [InlineData("`a`", "a")]
        [InlineData("`a` and `b`", "a", "b")]
        [InlineData("`a`, `b`, and `c`", "a", "b", "c")]
        [InlineData("`a`, `b`, `c`, and `d`", "a", "b", "c", "d")]
        [InlineData("", new string[0])]
        public void TermsAreJoinedTheWayASentenceWould(string expected, params string[] terms)
        {
            Assert.Equal(expected, WatchListOperations.Quoted(terms));
        }

        // ---- together with the watchlist check -----------------------------------------------------------------------

        [Fact]
        public void ATermAddedHereIsCaughtByTheWatchlistCheck()
        {
            var settings = WithWatchList();
            WatchListOperations.Run("add", "Twilight Sparkle, breasts", settings);

            Assert.Equal("breasts", BadlistHelper.CheckWatchList("safe, (breasts)", settings));
            Assert.Equal("twilight sparkle", BadlistHelper.CheckWatchList("TWILIGHT SPARKLE", settings));

            WatchListOperations.Run("remove", "breasts", settings);
            Assert.Equal(string.Empty, BadlistHelper.CheckWatchList("breasts", settings));
        }
    }
}
