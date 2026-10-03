namespace Cloudy_Canvas.Tests.Helpers
{
    using Cloudy_Canvas.Helpers;
    using Xunit;

    public class DiscordHelperTests
    {
        [Theory]
        [InlineData("<@123456789012345678>", 123456789012345678UL)]
        [InlineData("<@!123456789012345678>", 123456789012345678UL)] // legacy nickname mention
        [InlineData("  <@42>  ", 42UL)]
        public void UserMentionsAreParsed(string text, ulong expected)
        {
            Assert.Equal(expected, DiscordHelper.ConvertUserPingToId(text));
        }

        [Theory]
        [InlineData("plain name")]
        [InlineData("<@>")]
        [InlineData("<@abc>")]
        [InlineData("<@&123>")] // role, not a user
        [InlineData("<@-5>")]
        [InlineData("<@99999999999999999999999999>")] // overflows ulong
        [InlineData("<@123")]
        [InlineData("")]
        [InlineData(null)]
        public void InvalidUserMentionsGiveZeroInsteadOfThrowing(string text)
        {
            Assert.Equal(0UL, DiscordHelper.ConvertUserPingToId(text));
        }

        [Theory]
        [InlineData("<#123>", 123UL)]
        [InlineData("<#99999999999999999999999999>", 0UL)]
        [InlineData("<#abc>", 0UL)]
        [InlineData("general", 0UL)]
        public void ChannelMentionsAreParsedSafely(string text, ulong expected)
        {
            Assert.Equal(expected, DiscordHelper.ConvertChannelPingToId(text));
        }

        [Theory]
        [InlineData("<@&123>", 123UL)]
        [InlineData("<@123>", 0UL)]
        [InlineData("<@&abc>", 0UL)]
        [InlineData("Moderators", 0UL)]
        public void RoleMentionsAreParsedSafely(string text, ulong expected)
        {
            Assert.Equal(expected, DiscordHelper.ConvertRolePingToId(text));
        }

        [Theory]
        [InlineData("221742476153716736", 221742476153716736UL)]
        [InlineData("  221742476153716736  ", 221742476153716736UL)]
        [InlineData("12345678901234567", 12345678901234567UL)] // 17 digits: the oldest ids
        [InlineData("18446744073709551615", 18446744073709551615UL)] // 20 digits: ulong.MaxValue
        public void BareDiscordIdsAreParsed(string text, ulong expected)
        {
            Assert.Equal(expected, DiscordHelper.ParseSnowflake(text));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("1234567890123456")] // 16 digits: too short to be an id, more likely a typo or a name
        [InlineData("5")]
        [InlineData("18446744073709551616")] // 20 digits but overflows ulong
        [InlineData("99999999999999999999")]
        [InlineData("123456789012345678901")] // 21 digits
        [InlineData("22174247615371673x")]
        [InlineData("-22174247615371673")]
        [InlineData("2217424761537167.6")]
        [InlineData("<@221742476153716736>")] // mentions have their own parsers
        [InlineData("221742476153 716736")]
        [InlineData("\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668")] // non-ASCII digits
        public void ThingsThatAreNotBareIdsGiveZero(string text)
        {
            Assert.Equal(0UL, DiscordHelper.ParseSnowflake(text));
        }

        [Theory]
        [InlineData("2024-01-01")]
        [InlineData("123456789012345678")]
        [InlineData("some_channel-1")]
        public void SafePathSegmentsAreAccepted(string segment)
        {
            Assert.True(FileHelper.IsSafePathSegment(segment));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("..")]
        [InlineData(".")]
        [InlineData("../../123456/general/2024-01-01")]
        [InlineData("..\\..\\secret")]
        [InlineData("a/b")]
        [InlineData("a\\b")]
        [InlineData("2024-01-01.log")]
        [InlineData("C:")]
        [InlineData("a b")]
        [InlineData("a\0b")]
        public void PathSegmentsThatCouldEscapeTheDirectoryAreRejected(string segment)
        {
            Assert.False(FileHelper.IsSafePathSegment(segment));
        }

        [Theory]
        [InlineData(';', true)]
        [InlineData('!', true)]
        [InlineData('?', true)]
        [InlineData('$', true)]
        [InlineData('+', true)]
        [InlineData('a', false)]
        [InlineData('Z', false)]
        [InlineData('7', false)]
        [InlineData(' ', false)]
        [InlineData('@', false)]
        [InlineData('#', false)]
        [InlineData('`', false)]
        [InlineData('<', false)]
        [InlineData('>', false)]
        public void PrefixValidation(char prefix, bool expected)
        {
            Assert.Equal(expected, DiscordHelper.IsValidPrefix(prefix));
        }
    }
}
