namespace Cloudy_Canvas.Tests.Helpers
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Modules;
    using Discord.Commands;
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

        [Fact]
        public void EveryAdminModuleCommandIsAnAdminTopic()
        {
            // Guards the future: a new command added to the admin module must also be added to HelpText.AdminTopics.
            var adminModules = new[] { typeof(AdminModule), typeof(AdminModule.BadlistModule), typeof(AdminModule.LogModule), typeof(AdminSettingsModule) };

            // What a user types first: the group name for commands inside a group ("admin" for ";admin filter get"), else the command's own name.
            var commands = adminModules
                .SelectMany(module => module.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Select(method => (Group: module.GetCustomAttribute<GroupAttribute>()?.Prefix, Command: method.GetCustomAttribute<CommandAttribute>())))
                .Where(entry => entry.Command != null && !(entry.Command.Text ?? string.Empty).StartsWith("<"))
                .Select(entry => entry.Group ?? entry.Command.Text.Split(' ')[0])
                .Distinct()
                .ToList();

            Assert.True(commands.Count >= 11, $"expected to find the admin commands, found {commands.Count}");
            foreach (var command in commands)
            {
                Assert.True(HelpText.IsAdminTopic(command), $"'{command}' is an admin module command, add it to HelpText.AdminTopics so its help is hidden from non-admins");
            }
        }
    }
}
