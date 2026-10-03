namespace Cloudy_Canvas.Tests.Helpers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using Cloudy_Canvas.Helpers;
    using Xunit;

    /// <summary>
    /// What ";help" says, word for word. The expected text lives in help-snapshot.txt so that a change to the help shows up as a readable
    /// diff of that file in review, and so that restructuring how the help is stored can't change a word of it by accident.
    /// To accept an intended change, run the tests once with the environment variable UPDATE_SNAPSHOTS=1 and commit the new file.
    /// </summary>
    public class HelpSnapshotTests
    {
        private static readonly string[] AdminSettings =
        {
            "filter", "adminchannel", "adminrole", "filterchannel", "ignorechannel", "ignorerole", "allowuser", "watchchannel", "watchrole",
            "reportchannel", "reportrole", "logchannel",
        };

        private static readonly string[] Topics =
        {
            string.Empty, "pick", "pickrecent", "id", "tags", "featured", "getspoilers", "report", "setup", "watchlist", "log", "echo", "setprefix",
            "listentobots", "safemode", "alias", "getsettings", "refreshlists", "origin", "about", "help", "broadcast", "nonsense",
        };

        // A ";" straight in front of a word is the default prefix left in the text; a ";" in a sentence is just punctuation.
        private static readonly Regex DefaultPrefixedCommand = new(";[A-Za-z]", RegexOptions.CultureInvariant);

        private static string SourceDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path);

        private static string Render()
        {
            var text = new StringBuilder();

            void Add(bool isAdmin, string command, string subCommand)
            {
                var reply = HelpText.Reply(';', isAdmin, command, subCommand);
                text.Append("=== ").Append(isAdmin ? "admin" : "member").Append(": help ").Append(command).Append(' ').Append(subCommand).AppendLine();
                text.AppendLine(reply.Replace("\r\n", "\n"));
                text.AppendLine();
            }

            foreach (var topic in Topics)
            {
                Add(true, topic, string.Empty);
            }

            foreach (var subTopic in new[] { string.Empty }.Concat(AdminSettings).Append("nonsense"))
            {
                Add(true, "admin", subTopic);
            }

            // What someone who isn't an admin is told (and, more to the point, not told).
            foreach (var topic in new[] { string.Empty, "pick", "setup", "admin", "broadcast", "ADMIN", "nonsense" })
            {
                Add(false, topic, string.Empty);
            }

            Add(false, "admin", "filter");
            return text.ToString().Replace("\r\n", "\n");
        }

        [Fact]
        public void TheHelpSaysExactlyWhatItAlwaysSaid()
        {
            var actual = Render();
            var snapshot = Path.Combine(SourceDirectory(), "help-snapshot.txt");
            if (Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1")
            {
                File.WriteAllText(snapshot, actual, new UTF8Encoding(false));
                return;
            }

            var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Helpers", "help-snapshot.txt"), Encoding.UTF8).Replace("\r\n", "\n");
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EveryAnswerUsesTheServersPrefix()
        {
            foreach (var topic in Topics)
            {
                Assert.DoesNotMatch(DefaultPrefixedCommand, HelpText.Reply('!', true, topic, string.Empty));
            }

            foreach (var setting in AdminSettings)
            {
                Assert.DoesNotMatch(DefaultPrefixedCommand, HelpText.Reply('!', true, "admin", setting));
            }
        }
    }
}
