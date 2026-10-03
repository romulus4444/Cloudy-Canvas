namespace Cloudy_Canvas.Tests.Helpers
{
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Settings;
    using Xunit;

    public class AliasTests
    {
        private static ServerPreloadedSettings WithAliases(params (string Short, string Long)[] aliases)
        {
            var settings = new ServerPreloadedSettings();
            foreach (var (shortForm, longForm) in aliases)
            {
                settings.Aliases[shortForm] = longForm;
            }

            return settings;
        }

        [Theory]
        [InlineData(";pick cute", "pick cute")]
        [InlineData(";PICK cute", "pick cute")] // the command word is lower-cased, arguments are not
        [InlineData(";Pick Twilight Sparkle", "pick Twilight Sparkle")]
        [InlineData(";  pick cute", "pick cute")]
        [InlineData(";help", "help")]
        [InlineData(";", "")]
        public void WithoutAliasesTheCommandIsJustNormalised(string message, string expected)
        {
            Assert.Equal(expected, DiscordHelper.ResolveAliases(message, new ServerPreloadedSettings()));
        }

        [Fact]
        public void AliasExpandsToItsLongForm()
        {
            var settings = WithAliases(("cute", "pick cute"));
            Assert.Equal("pick cute", DiscordHelper.ResolveAliases(";cute", settings));
            Assert.Equal("pick cute rarity", DiscordHelper.ResolveAliases(";cute rarity", settings));
        }

        [Fact]
        public void AliasMustMatchAWholeWord()
        {
            var settings = WithAliases(("p", "pick cute"), ("cute", "pick cute"));

            // "p" must not hijack "pick", and "cute" must not match the start of "cutest".
            Assert.Equal("pick rarity", DiscordHelper.ResolveAliases(";pick rarity", settings));
            Assert.Equal("cutest rarity", DiscordHelper.ResolveAliases(";cutest rarity", settings));
            Assert.Equal("pick cute", DiscordHelper.ResolveAliases(";p", settings));
        }

        [Fact]
        public void OnlyTheLeadingAliasIsReplaced()
        {
            var settings = WithAliases(("cute", "pick cute"));

            // Occurrences in the arguments are left alone.
            Assert.Equal("pick cute cute, safe", DiscordHelper.ResolveAliases(";cute cute, safe", settings));
            Assert.Equal("id 123 cute", DiscordHelper.ResolveAliases(";id 123 cute", settings));
        }

        [Fact]
        public void AliasMatchingIsCaseInsensitive()
        {
            var settings = WithAliases(("Cute", "pick cute"));
            Assert.Equal("pick cute", DiscordHelper.ResolveAliases(";CUTE", settings));
        }

        [Fact]
        public void AliasesWithSpacesMatchSeveralLeadingWords()
        {
            var settings = WithAliases(("good pony", "pick safe, cute"));
            Assert.Equal("pick safe, cute", DiscordHelper.ResolveAliases(";good pony", settings));
            Assert.Equal("pick safe, cute rarity", DiscordHelper.ResolveAliases(";good pony rarity", settings));
            Assert.Equal("good", DiscordHelper.ResolveAliases(";good", settings));
        }

        [Fact]
        public void TheLongestMatchingAliasWins()
        {
            var settings = WithAliases(("good", "pick good"), ("good pony", "pick cute"));
            Assert.Equal("pick cute", DiscordHelper.ResolveAliases(";good pony", settings));
            Assert.Equal("pick good", DiscordHelper.ResolveAliases(";good", settings));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void EmptyAliasesNeverMatch(string shortForm)
        {
            var settings = WithAliases((shortForm, "pick cute"));
            Assert.Equal("pick rarity", DiscordHelper.ResolveAliases(";pick rarity", settings));
        }

        [Fact]
        public void AliasesAreNotChained()
        {
            var settings = WithAliases(("a", "b"), ("b", "pick cute"));
            Assert.Equal("b", DiscordHelper.ResolveAliases(";a", settings));
        }
    }
}
