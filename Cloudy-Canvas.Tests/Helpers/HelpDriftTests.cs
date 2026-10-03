namespace Cloudy_Canvas.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Admin;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Modules;
    using Discord.Commands;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// The help is data (HelpTopics) that has to describe the bot's real commands. These tests compare the two in both directions, so
    /// adding a command without help, leaving help for a command that is gone, or changing who may use a command without changing who sees
    /// its help, fails the build instead of confusing a user.
    /// </summary>
    public class HelpDriftTests
    {
        private static readonly Lazy<ServiceProvider> Provider = new(() => TestServices.Build());

        /// <summary>Each command a user can type, by its first word, and whether it is restricted. Placeholder commands like "&lt;mention&gt;" are left out.</summary>
        private static async Task<Dictionary<string, bool>> RealCommandsAsync()
        {
            var commands = new CommandService();
            await commands.AddModulesAsync(typeof(Program).Assembly, Provider.Value);
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var command in commands.Modules.SelectMany(module => module.Commands))
            {
                var word = command.Aliases.First().Split(' ')[0];
                if (word.StartsWith('<'))
                {
                    continue;
                }

                var preconditions = command.Preconditions.ToList();
                for (var module = command.Module; module != null; module = module.Parent)
                {
                    preconditions.AddRange(module.Preconditions);
                }

                var restricted = preconditions.Any(p => p is RequireBotAdminAttribute or RequireBroadcastUserAttribute or RequireUserPermissionAttribute);
                result[word] = result.TryGetValue(word, out var already) ? already && restricted : restricted;
            }

            return result;
        }

        [Fact]
        public async Task EveryCommandHasAHelpTopic()
        {
            var real = await RealCommandsAsync();

            var missing = real.Keys.Where(command => HelpTopics.All.All(topic => !string.Equals(topic.Name, command, StringComparison.OrdinalIgnoreCase))).ToList();

            Assert.True(missing.Count == 0, $"commands without a topic in HelpTopics: {string.Join(", ", missing)}");
        }

        [Fact]
        public async Task EveryHelpTopicIsARealCommand()
        {
            var real = await RealCommandsAsync();

            var gone = HelpTopics.All.Where(topic => !real.ContainsKey(topic.Name)).Select(topic => topic.Name).ToList();

            Assert.True(gone.Count == 0, $"help topics for commands that don't exist: {string.Join(", ", gone)}");
        }

        [Fact]
        public async Task ACommandIsAdminOnlyInTheHelpExactlyWhenItIsRestrictedInTheBot()
        {
            var real = await RealCommandsAsync();

            foreach (var topic in HelpTopics.All)
            {
                Assert.True(real[topic.Name] == topic.AdminOnly, $"'{topic.Name}': restricted in the bot = {real[topic.Name]}, AdminOnly in the help = {topic.AdminOnly}");
            }
        }

        [Fact]
        public void TopicNamesAreUniqueAndEachListedTopicHasAnEntryInItsSection()
        {
            Assert.Equal(HelpTopics.All.Count, HelpTopics.All.Select(topic => topic.Name.ToLowerInvariant()).Distinct().Count());

            // Whatever is in a section of the overview is a topic you can ask about, and the other way round.
            foreach (var topic in HelpTopics.All)
            {
                Assert.True((topic.Section == HelpSection.None) == (topic.Listing == null), $"'{topic.Name}' must be listed in a section exactly when it has a listing");
                if (topic.Listing != null)
                {
                    Assert.Equal(topic.Name, topic.Listing.Split(' ')[0]);
                }
            }
        }

        [Fact]
        public void TheAdminHelpDescribesEverySettingTheBotHas()
        {
            Assert.Equal(
                AdminGrammar.Actions.Keys.OrderBy(name => name, StringComparer.Ordinal),
                HelpTopics.AdminSettings.Select(setting => setting.Name).OrderBy(name => name, StringComparer.Ordinal));
        }

        [Fact]
        public void TheAdminHelpDescribesEveryActionOfEverySetting()
        {
            foreach (var setting in HelpTopics.AdminSettings)
            {
                Assert.Equal(
                    AdminGrammar.Actions[setting.Name].OrderBy(action => action, StringComparer.Ordinal),
                    setting.Actions.Select(action => action.Action).OrderBy(action => action, StringComparer.Ordinal));
            }
        }

        [Fact]
        public void TheAdminOverviewListsEveryAdminSetting()
        {
            var overview = HelpText.Reply(';', true, "admin", string.Empty);

            foreach (var setting in AdminGrammar.Actions.Keys)
            {
                Assert.Contains($"`;admin {setting} ...`", overview);
            }
        }

        [Theory]
        [InlineData("PICK", "pick")]
        [InlineData("Setup", "setup")]
        [InlineData("About", "about")]
        public void TopicNamesAreMatchedIgnoringCase(string typed, string canonical)
        {
            Assert.Equal(HelpText.Reply(';', true, canonical, string.Empty), HelpText.Reply(';', true, typed, string.Empty));
        }

        [Fact]
        public void SettingNamesAreMatchedIgnoringCase()
        {
            Assert.Equal(HelpText.Reply(';', true, "admin", "filterchannel"), HelpText.Reply(';', true, "ADMIN", "FilterChannel"));
        }

        [Fact]
        public void AdminHelpIsHiddenFromMembersWhateverTheCase()
        {
            foreach (var typed in new[] { "setup", "SETUP", "Admin", "broadcast", "Broadcast" })
            {
                Assert.StartsWith("Only available to bot admins", HelpText.Reply(';', false, typed, string.Empty));
            }

            Assert.StartsWith("Only available to bot admins", HelpText.Reply(';', false, "admin", "filter"));
        }

        [Fact]
        public void BroadcastIsNeverDescribedNotEvenToAdmins()
        {
            Assert.StartsWith("Invalid command", HelpText.Reply(';', true, "broadcast", string.Empty));
            Assert.DoesNotContain("broadcast", HelpText.Overview(';', true), StringComparison.OrdinalIgnoreCase);
        }
    }
}
