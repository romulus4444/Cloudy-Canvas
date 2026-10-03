namespace Cloudy_Canvas.Tests.Helpers
{
    using System;
    using System.Linq;
    using Cloudy_Canvas.Helpers;
    using Xunit;

    public class LogLineTests
    {
        private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        private static readonly string NewLine = Environment.NewLine;
        private static readonly LogSource Guild = new(false, "pinkie", 3, "The Guild", 1, "general", 2);
        private static readonly LogSource Dm = new(true, "pinkie", 3, string.Empty, 0, string.Empty, 0);

        private static string Separator(int codePoint) => ((char)codePoint).ToString();

        // ---- the format of a line (as it has always been) ------------------------------------------------------------

        [Fact]
        public void AConsoleLineForAServerNamesTheServerChannelAndUser()
        {
            Assert.Equal("[2026-10-01T12:00:00] server: The Guild (1) #general (2) @pinkie (3), pick cute, total: 5", LogLine.Format(Guild, "pick cute, total: 5", Now));
        }

        [Fact]
        public void AConsoleLineForADirectMessageSaysSo()
        {
            Assert.Equal("[2026-10-01T12:00:00] DM with @pinkie (3), pick cute", LogLine.Format(Dm, "pick cute", Now));
        }

        [Fact]
        public void AFileEntryLeavesOutWhatTheFileHeaderSays()
        {
            Assert.Equal($"[2026-10-01T12:00:00] @pinkie (3), pick cute{NewLine}", LogLine.Format(Guild, "pick cute", Now, fileEntry: true));
            Assert.Equal($"[2026-10-01T12:00:00] pick cute{NewLine}", LogLine.Format(Dm, "pick cute", Now, fileEntry: true));
        }

        [Fact]
        public void TheFileHeaderSaysOnlyWhereTheLogIsFrom()
        {
            Assert.Equal($"server: The Guild (1) #general (2){NewLine}", LogLine.Format(Guild, "ignored", Now, header: true));
            Assert.Equal($"DM with @pinkie (3){NewLine}", LogLine.Format(Dm, "ignored", Now, header: true));
        }

        [Fact]
        public void TheTimeIsShownTheSameWhateverTheCulture()
        {
            var previous = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("ar-SA");

                Assert.StartsWith("[2026-10-01T12:00:00] ", LogLine.Format(Guild, "x", Now));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }
        }

        // ---- a user can't write extra lines into the log -------------------------------------------------------------

        [Fact]
        public void AMessageWithLineBreaksCannotForgeAnotherEntry()
        {
            var forged = "pick cute\n[2026-10-01T00:00:00] server: The Guild (1) #general (2) @admin (999), echo: all is well <SUCCESS>";

            var entry = LogLine.Format(Guild, forged, Now, fileEntry: true);
            var console = LogLine.Format(Guild, forged, Now);

            Assert.Single(entry.Split(NewLine, StringSplitOptions.RemoveEmptyEntries));
            Assert.DoesNotContain('\n', console);
            Assert.DoesNotContain('\r', console);
            Assert.Contains("pick cute\\n[2026-10-01T00:00:00]", entry);
        }

        [Fact]
        public void EveryKindOfLineBreakIsWrittenOut()
        {
            var message = "a\r\nb\rc\nd" + Separator(0x2028) + "e" + Separator(0x2029) + "f" + Separator(0x85) + "g";

            var escaped = LogLine.Escape(message);

            Assert.Equal("a\\r\\nb\\rc\\nd\\u2028e\\u2029f\\u0085g", escaped);
        }

        [Fact]
        public void OtherControlCharactersAreWrittenOutToo()
        {
            Assert.Equal("tab\\there", LogLine.Escape("tab\there"));
            Assert.Equal("nul\\u0000esc\\u001b", LogLine.Escape("nul" + Separator(0) + "esc" + Separator(0x1b)));
            Assert.Equal("del\\u007f", LogLine.Escape("del" + Separator(0x7f)));
        }

        [Fact]
        public void NoCharacterAtAllCanPutAControlOrLineBreakIntoAnEscapedLine()
        {
            for (var code = 0; code <= 0xFFFF; code++)
            {
                var escaped = LogLine.Escape("<" + Separator(code) + ">");

                Assert.True(escaped.All(c => !char.IsControl(c) && c != (char)0x2028 && c != (char)0x2029), $"U+{code:X4} survived");
            }
        }

        [Theory]
        [InlineData("pick twilight sparkle, safe")]
        [InlineData("a backslash \\ and a \"quote\" and `ticks`")]
        [InlineData("café ❤ \U0001F984 日本語")]
        [InlineData("created_at.gte:2026-10-01, (a || b)")]
        [InlineData("")]
        public void OrdinaryTextIsNotChanged(string text)
        {
            Assert.Equal(text, LogLine.Escape(text));
        }

        [Fact]
        public void NothingToEscapeReturnsTheSameString()
        {
            var text = "pick cute";

            Assert.Same(text, LogLine.Escape(text));
        }

        [Fact]
        public void NullBecomesEmpty()
        {
            Assert.Equal(string.Empty, LogLine.Escape(null));
            Assert.Equal("[2026-10-01T12:00:00] DM with @pinkie (3), ", LogLine.Format(Dm, null, Now));
        }

        [Fact]
        public void NamesFromDiscordAreMadeSafeAsWell()
        {
            var source = new LogSource(false, "evil\nname", 3, "guild\nname", 1, "chan\nnel", 2);

            var line = LogLine.Format(source, "x", Now);

            Assert.DoesNotContain('\n', line);
            Assert.Equal("[2026-10-01T12:00:00] server: guild\\nname (1) #chan\\nnel (2) @evil\\nname (3), x", line);
        }

        [Fact]
        public void OnlyTheLineEndOfAFileEntryIsAnActualLineBreak()
        {
            var entry = LogLine.Format(Guild, "a\nb", Now, fileEntry: true);
            var header = LogLine.Format(Guild, "a\nb", Now, header: true);

            Assert.EndsWith(NewLine, entry);
            Assert.EndsWith(NewLine, header);
            Assert.Equal(1, entry.Split(NewLine).Length - 1);
            Assert.Equal(1, header.Split(NewLine).Length - 1);
        }
    }
}
