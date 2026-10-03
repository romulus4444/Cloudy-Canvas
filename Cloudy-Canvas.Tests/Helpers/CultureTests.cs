namespace Cloudy_Canvas.Tests.Helpers
{
    using System;
    using System.Globalization;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Xunit;

    /// <summary>
    /// The bot's text handling must not depend on the machine's language settings. These run the affected code under cultures that
    /// expose the problems: Thai (Buddhist-calendar years, so "2026" becomes "2569"), Arabic (Saudi Arabia: Hijri calendar and
    /// non-Latin digits) and Turkish (an upper-case "I" lower-cases to a dotless "ı", so "ID" stops matching "id").
    /// </summary>
    public class CultureTests
    {
        private static void Under(string culture, Action test)
        {
            var original = CultureInfo.CurrentCulture;
            var originalUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(culture);
                test();
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
                CultureInfo.CurrentUICulture = originalUi;
            }
        }

        [Theory]
        [InlineData("th-TH")]
        [InlineData("ar-SA")]
        [InlineData("fa-IR")]
        [InlineData("tr-TR")]
        public void MixinsAreAlwaysGregorianAndLatin(string culture)
        {
            Under(culture, () =>
            {
                var mixins = new MixinsService(new MockDateTimeService()); // 2021-04-23 17:20:07

                Assert.Equal(
                    "2021-04-23 2021 04 23 17 20 07",
                    mixins.Transpile("{{today}} {{current_year}} {{current_month}} {{current_day}} {{current_hour}} {{current_minute}} {{current_second}}"));
            });
        }

        [Theory]
        [InlineData("th-TH")]
        [InlineData("ar-SA")]
        [InlineData("fa-IR")]
        public void LogFilesAreNamedWithTheGregorianDate(string culture)
        {
            // ;log looks files up by a yyyy-MM-dd date parsed in the invariant culture, so they have to be written that way.
            Under(culture, () => Assert.Equal("2021-04-23", FileHelper.DateStamp(new DateTime(2021, 4, 23, 17, 20, 7, DateTimeKind.Utc))));
        }

        [Theory]
        [InlineData(";ID 5", "id 5")]
        [InlineData(";PICK Rarity", "pick Rarity")]
        [InlineData(";Featured", "featured")]
        [InlineData(";LISTENTOBOTS", "listentobots")]
        public void CommandsMatchRegardlessOfCase(string message, string expected)
        {
            // Under Turkish rules "ID".ToLower() is "ıd", which no command is called.
            Under("tr-TR", () => Assert.Equal(expected, DiscordHelper.ResolveAliases(message, new ServerPreloadedSettings())));
        }

        [Fact]
        public void TheWatchlistMatchesRegardlessOfCase()
        {
            var settings = new ServerSettings();
            settings.WatchList.Add("image");

            Under("tr-TR", () => Assert.Equal("image", BadlistHelper.CheckWatchList("IMAGE", settings)));
        }

        [Theory]
        [InlineData("th-TH")]
        [InlineData("ar-SA")]
        public void MentionIdsParseTheSameInEveryCulture(string culture)
        {
            Under(culture, () =>
            {
                Assert.Equal(221742476153716736UL, DiscordHelper.ConvertUserPingToId("<@221742476153716736>"));
                Assert.Equal(221742476153716736UL, DiscordHelper.ParseSnowflake("221742476153716736"));
            });
        }
    }
}
