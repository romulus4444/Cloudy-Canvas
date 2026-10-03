namespace Cloudy_Canvas.Tests.Helpers
{
    using System;
    using System.Linq;
    using Cloudy_Canvas.Helpers;
    using Xunit;

    public class HelpTextTests
    {
        private static readonly string AdminSection = string.Join(
            Environment.NewLine,
            "**Admin Module:**",
            "`;setup ...`",
            "`;admin ...`",
            "`;watchlist ...`",
            "`;log ...`",
            "`;echo ...`",
            "`;setprefix ...`",
            "`;listentobots ...`",
            "`;safemode ...`",
            "`;alias ...`",
            "`;getsettings`",
            "`;refreshlists`",
            string.Empty);

        [Fact]
        public void AdminsSeeTheOverviewExactlyAsItAlwaysWas()
        {
            var expected = string.Join(
                Environment.NewLine,
                "**__All Commands:__**",
                "**Booru Module:**",
                "`;pick ...`",
                "`;pickrecent ...`",
                "`;id ...`",
                "`;tags ...`",
                "`;featured`",
                "`;getspoilers`",
                "`;report ...`",
                AdminSection + "**Info Module:**",
                "`;origin`",
                "`;about`",
                string.Empty,
                "Use `;help <command>` for more details on a particular command.");

            Assert.Equal(expected, HelpText.Overview(';', true));
        }

        [Fact]
        public void OthersSeeTheSameOverviewWithoutTheAdminSection()
        {
            var admin = HelpText.Overview(';', true);
            var everyone = HelpText.Overview(';', false);

            Assert.Equal(admin.Replace(AdminSection, string.Empty), everyone);
            Assert.DoesNotContain("Admin", everyone);
            foreach (var command in HelpText.ListedAdminCommands())
            {
                Assert.DoesNotContain($"`;{command}", everyone);
            }

            // Everything else is still there.
            Assert.Contains("**Booru Module:**", everyone);
            Assert.Contains("`;pick ...`", everyone);
            Assert.Contains("`;report ...`", everyone);
            Assert.Contains("**Info Module:**", everyone);
            Assert.Contains("`;about`", everyone);
        }

        [Fact]
        public void TheOverviewUsesTheServersPrefix()
        {
            var text = HelpText.Overview('!', true);

            Assert.Contains("`!pick ...`", text);
            Assert.Contains("`!setup ...`", text);
            Assert.Contains("Use `!help <command>`", text);
            Assert.DoesNotContain(";", text);
        }

        [Theory]
        [InlineData("setup")]
        [InlineData("admin")]
        [InlineData("watchlist")]
        [InlineData("log")]
        [InlineData("echo")]
        [InlineData("setprefix")]
        [InlineData("listentobots")]
        [InlineData("safemode")]
        [InlineData("alias")]
        [InlineData("getsettings")]
        [InlineData("refreshlists")]
        [InlineData("broadcast")]
        [InlineData("ADMIN")]
        public void AdminTopicsAreRecognised(string topic)
        {
            Assert.True(HelpText.IsAdminTopic(topic));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("pick")]
        [InlineData("pickrecent")]
        [InlineData("id")]
        [InlineData("tags")]
        [InlineData("featured")]
        [InlineData("getspoilers")]
        [InlineData("report")]
        [InlineData("origin")]
        [InlineData("about")]
        [InlineData("help")]
        [InlineData("nonsense")]
        public void OtherTopicsAreNotAdminOnly(string topic)
        {
            Assert.False(HelpText.IsAdminTopic(topic));
        }

        [Fact]
        public void EveryCommandListedInTheAdminSectionIsAnAdminTopic()
        {
            foreach (var command in HelpText.ListedAdminCommands())
            {
                Assert.True(HelpText.IsAdminTopic(command), $"{command} is listed under Admin Module but its help isn't hidden from non-admins");
            }
        }
    }
}
