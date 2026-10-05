namespace Cloudy_Canvas.Tests
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Discord;
    using Xunit;

    /// <summary>
    /// PRIVACY.md makes promises about what the bot asks Discord for and how it handles messages. These tests hold the code to the ones
    /// that can be checked, so a change that makes the policy untrue fails here until the policy is updated first.
    /// </summary>
    public class PrivacyPolicyTests
    {
        private static string RepositoryFile(string name, [CallerFilePath] string testFile = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFile), "..", name));

        [Fact]
        public void TheBotAsksDiscordForExactlyTheIntentsThePolicyNames()
        {
            // Guilds, Guild Messages, Direct Messages and Message Content: no Server Members, no Presence, nothing else.
            var expected = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent;

            Assert.Equal(expected, ServiceCollectionExtensions.ClientConfig().GatewayIntents);
        }

        [Fact]
        public void TheBotKeepsNoCacheOfMessages()
        {
            Assert.Equal(0, ServiceCollectionExtensions.ClientConfig().MessageCacheSize);
        }

        [Fact]
        public void ThePolicyNamesEveryIntentTheBotUses()
        {
            var policy = File.ReadAllText(RepositoryFile("PRIVACY.md"), Encoding.UTF8);

            foreach (var phrase in new[] { "Guilds, Guild Messages, Direct Messages and Message Content", "does not request the Server Members or Presence intents", "does not keep a message cache" })
            {
                Assert.Contains(phrase, policy, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void ThePolicyIsLinkedFromTheReadmeAndHasADate()
        {
            var readme = File.ReadAllText(RepositoryFile("README.md"), Encoding.UTF8);
            var policy = File.ReadAllText(RepositoryFile("PRIVACY.md"), Encoding.UTF8);

            Assert.Contains("(PRIVACY.md)", readme, StringComparison.Ordinal);
            Assert.Matches(@"Last updated: \d{1,2} [A-Z][a-z]+ \d{4}", policy);
        }
    }
}
