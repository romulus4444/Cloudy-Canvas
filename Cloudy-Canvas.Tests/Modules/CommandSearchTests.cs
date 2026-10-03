namespace Cloudy_Canvas.Tests.Modules
{
    using System;
    using System.Threading.Tasks;
    using Discord.Commands;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// The worker decides whether a message is a command with CommandService.Search (and stays silent otherwise), using the message as the
    /// worker builds it: the prefix removed and the command word lower-cased. These run that check against all the bot's real modules.
    /// </summary>
    public class CommandSearchTests
    {
        private static readonly Lazy<ServiceProvider> Provider = new(() => TestServices.Build());

        private static async Task<CommandService> BuildAsync()
        {
            var commands = new CommandService();
            await commands.AddModulesAsync(typeof(Program).Assembly, Provider.Value);
            return commands;
        }

        [Theory]
        [InlineData("pick")]
        [InlineData("pick twilight sparkle, safe")]
        [InlineData("pickrecent cute")]
        [InlineData("id 4010266")]
        [InlineData("tags 4010266")]
        [InlineData("featured")]
        [InlineData("getspoilers")]
        [InlineData("report 123 this breaks the rules")]
        [InlineData("help")]
        [InlineData("help admin filterchannel")]
        [InlineData("origin")]
        [InlineData("about")]
        [InlineData("setup 175 #admin Moderators")]
        [InlineData("getsettings")]
        [InlineData("refreshlists")]
        [InlineData("echo #general hello there")]
        [InlineData("setprefix !")]
        [InlineData("listentobots yes")]
        [InlineData("safemode off")]
        [InlineData("alias add cute pick cute")]
        [InlineData("watchlist add breasts")]
        [InlineData("log #general 2026-10-01")]
        [InlineData("broadcast something for every server")]
        [InlineData("admin")]
        [InlineData("admin filter get")]
        [InlineData("admin filter set 175")]
        [InlineData("admin ignorerole add Server Moderators")]
        [InlineData("admin allowuser add 221742476153716736")]
        [InlineData("admin filterchannel add general 56027")]
        [InlineData("admin FILTER GET")]
        [InlineData("admin nonsense")]
        [InlineData("<blank message>")]
        [InlineData("<mention>")]
        public async Task EveryRealCommandIsFound(string typed)
        {
            var commands = await BuildAsync();

            Assert.True(commands.Search(typed).IsSuccess, typed);
        }

        [Theory]
        [InlineData("nonsense")]
        [InlineData("nonsense with arguments")]
        [InlineData("filter get")]
        [InlineData("adminfilter get")]
        [InlineData("pickle")]
        [InlineData("<invalid command>")]
        [InlineData("x")]
        public async Task EverythingElseIsNotACommandSoTheBotStaysQuiet(string typed)
        {
            var commands = await BuildAsync();

            Assert.False(commands.Search(typed).IsSuccess, typed);
        }
    }
}
