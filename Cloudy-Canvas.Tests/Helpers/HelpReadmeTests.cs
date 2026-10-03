namespace Cloudy_Canvas.Tests.Helpers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Cloudy_Canvas.Helpers;
    using Xunit;

    /// <summary>
    /// The README's command reference is written from the same table as ";help". If the table changes and the README isn't regenerated,
    /// this fails; run the tests once with UPDATE_SNAPSHOTS=1 to rewrite the generated parts of the README, and commit the result.
    /// </summary>
    public class HelpReadmeTests
    {
        private static string ReadmePath([CallerFilePath] string testFile = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFile), "..", "..", "README.md"));

        [Fact]
        public void TheReadmeReferenceIsUpToDate()
        {
            var path = ReadmePath();
            var readme = File.ReadAllText(path, Encoding.UTF8).Replace("\r\n", "\n");
            var updated = ReadmeReference.Update(readme);

            if (Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1")
            {
                File.WriteAllText(path, updated, new UTF8Encoding(false));
                return;
            }

            Assert.True(readme == updated, "The README's command reference doesn't match HelpTopics.cs. Run the tests with UPDATE_SNAPSHOTS=1 and commit README.md.");
        }

        [Fact]
        public void TheReadmeHasAGeneratedRegionForEverySectionOfTheHelp()
        {
            var readme = File.ReadAllText(ReadmePath(), Encoding.UTF8);

            foreach (var section in new[] { "booru", "admin", "info" })
            {
                Assert.Contains($"<!-- BEGIN GENERATED: {section}.", readme);
                Assert.Contains($"<!-- END GENERATED: {section} -->", readme);
            }
        }

        [Fact]
        public void EveryDocumentedCommandAppearsInTheReadme()
        {
            var readme = ReadmeReference.Update(File.ReadAllText(ReadmePath(), Encoding.UTF8).Replace("\r\n", "\n"));

            // Every command and every admin setting the help knows about is in the README, except the one kept out of all documentation.
            foreach (var topic in HelpTopics.All.Where(topic => topic.Text != null && topic.Section != HelpSection.None && topic.Name != "admin"))
            {
                Assert.Contains($"`;{topic.Name}", readme);
            }

            foreach (var setting in HelpTopics.AdminSettings)
            {
                foreach (var action in setting.Actions)
                {
                    Assert.Contains($"`;admin {setting.Name} {action.Action}", readme);
                }
            }

            Assert.DoesNotContain("broadcast", readme, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void NoMessageTheHelpSendsIsTooLongForDiscord()
        {
            // Discord refuses a message over 2000 characters; the help would then silently fail to arrive.
            foreach (var isAdmin in new[] { true, false })
            {
                foreach (var topic in HelpTopics.All.Select(topic => topic.Name).Prepend(string.Empty))
                {
                    Assert.True(HelpText.Reply(';', isAdmin, topic, string.Empty).Length <= 2000, $"help {topic}");
                }
            }

            foreach (var setting in HelpTopics.AdminSettings)
            {
                Assert.True(HelpText.Reply(';', true, "admin", setting.Name).Length <= 2000, $"help admin {setting.Name}");
            }
        }
    }
}
