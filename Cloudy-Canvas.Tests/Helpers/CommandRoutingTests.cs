namespace Cloudy_Canvas.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Settings;
    using Xunit;

    public class CommandRoutingTests
    {
        private static readonly Func<string, bool> KnowsPickAndHelp = text => text.StartsWith("pick", StringComparison.Ordinal) || text == "help";

        private static string Route(
            string content,
            ServerPreloadedSettings settings = null,
            bool prefixed = true,
            bool mention = false,
            bool bot = false,
            Func<string, bool> isCommand = null)
        {
            return CommandRouting.Resolve(prefixed, mention, bot, content, settings ?? new ServerPreloadedSettings(), isCommand ?? KnowsPickAndHelp);
        }

        [Theory]
        [InlineData(";pick cute", "pick cute")]
        [InlineData(";PICK cute", "pick cute")]
        [InlineData(";  help", "help")]
        public void AKnownCommandIsRunAsTheNormalisedText(string content, string expected)
        {
            Assert.Equal(expected, Route(content));
        }

        [Fact]
        public void AMessageWithoutThePrefixIsNotACommand()
        {
            Assert.Null(Route("pick cute", prefixed: false));
        }

        [Theory]
        [InlineData(";nonsense")]
        [InlineData(";nonsense with arguments")]
        [InlineData(";admin filter get")] // not known to this stand-in
        public void AnUnknownCommandGetsNoAnswerAtAll(string content)
        {
            Assert.Null(Route(content));
        }

        [Fact]
        public void TheCommandCheckSeesTheTextAfterAliasesAreExpanded()
        {
            var seen = new List<string>();
            var settings = new ServerPreloadedSettings();
            settings.Aliases["cute"] = "pick cute";

            var result = Route(";cute", settings, isCommand: text =>
            {
                seen.Add(text);
                return true;
            });

            Assert.Equal("pick cute", result);
            Assert.Equal(new[] { "pick cute" }, seen);
        }

        [Fact]
        public void AnAliasForAnUnknownCommandIsStillSilent()
        {
            var settings = new ServerPreloadedSettings();
            settings.Aliases["x"] = "nonsense";

            Assert.Null(Route(";x", settings));
        }

        [Theory]
        [InlineData(";")]
        [InlineData(";   ")]
        public void OnlyThePrefixIsABlankMessage(string content)
        {
            Assert.Equal("<blank message>", Route(content));
            Assert.Equal(CommandRouting.BlankMessage, Route(content));
        }

        [Fact]
        public void ABlankMessageIsAnsweredWithoutAskingWhetherItIsACommand()
        {
            Assert.Equal("<blank message>", Route(";", isCommand: _ => throw new InvalidOperationException("must not be asked")));
        }

        [Theory]
        [InlineData("<@123456789012345678>")]
        [InlineData("<@123456789012345678> pick cute")]
        [InlineData("<@!123456789012345678> nonsense")]
        public void AMentionOfTheBotIsAnsweredWhateverFollowsIt(string content)
        {
            Assert.Equal("<mention>", Route(content, mention: true, isCommand: _ => throw new InvalidOperationException("must not be asked")));
            Assert.Equal(CommandRouting.Mention, Route(content, mention: true));
        }

        [Fact]
        public void AnotherBotIsIgnoredUnlessTheServerAsksToListenToBots()
        {
            Assert.Null(Route(";pick cute", bot: true));
            Assert.Null(Route("<@1>", mention: true, bot: true));
            Assert.Null(Route(";", bot: true));

            var listening = new ServerPreloadedSettings { ListenToBots = true };
            Assert.Equal("pick cute", Route(";pick cute", listening, bot: true));
            Assert.Equal("<mention>", Route("<@1>", listening, mention: true, bot: true));
        }

        [Fact]
        public void APersonIsNeverIgnoredForBeingAPerson()
        {
            var deaf = new ServerPreloadedSettings { ListenToBots = false };

            Assert.Equal("pick cute", Route(";pick cute", deaf, bot: false));
        }
    }
}
