namespace Cloudy_Canvas.Tests.Modules
{
    using System;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// The admin commands are written as one command group ("admin") whose commands have multi-word names ("filter get"), plus low-priority
    /// catch-alls that explain a missing or unknown subcommand. These tests run the real CommandService against a module of that same shape
    /// to pin down how Discord.Net resolves such overlapping commands, because the admin module relies on it.
    /// </summary>
    [Collection("GroupRouting")]
    public class GroupRoutingTests
    {
        [Group("admin")]
        public class ShapeModule : ModuleBase<ICommandContext>
        {
            public static string Called { get; set; }

            // `admin` and `admin <anything we don't recognise>`. Lowest priority of all, so that the catch-all of a sub-group
            // (below) wins over this one for `admin filter bogus`; with equal priorities this one was chosen instead.
            [Command]
            [Priority(-2)]
            public Task AdminDefaultAsync([Remainder] string rest = "")
            {
                Called = $"admin-default[{rest}]";
                return Task.CompletedTask;
            }

            [Command("filter")]
            [Priority(-1)]
            public Task FilterDefaultAsync([Remainder] string rest = "")
            {
                Called = $"filter-default[{rest}]";
                return Task.CompletedTask;
            }

            [Command("filter get")]
            public Task FilterGetAsync([Remainder] string ignored = "")
            {
                Called = "filter-get";
                return Task.CompletedTask;
            }

            [Command("filter set")]
            public Task FilterSetAsync([Remainder] string filter = "")
            {
                Called = $"filter-set[{filter}]";
                return Task.CompletedTask;
            }

            [Command("filterchannel")]
            [Priority(-1)]
            public Task FilterChannelDefaultAsync([Remainder] string rest = "")
            {
                Called = $"filterchannel-default[{rest}]";
                return Task.CompletedTask;
            }

            [Command("filterchannel add")]
            public Task FilterChannelAddAsync(string channel, int filterId = 175)
            {
                Called = $"filterchannel-add[{channel},{filterId}]";
                return Task.CompletedTask;
            }

            [Command("adminrole set")]
            public Task AdminRoleSetAsync([Remainder] string role)
            {
                Called = $"adminrole-set[{role}]";
                return Task.CompletedTask;
            }
        }

        private sealed class StubContext : ICommandContext
        {
            public IDiscordClient Client => null;

            public IGuild Guild => null;

            public IMessageChannel Channel => null;

            public IUser User => null;

            public IUserMessage Message => null;
        }

        private static async Task<(IResult Result, string Called)> RunAsync(string input)
        {
            ShapeModule.Called = null;
            var commands = new CommandService();
            var services = new ServiceCollection().BuildServiceProvider();
            await commands.AddModuleAsync<ShapeModule>(services);
            var result = await commands.ExecuteAsync(new StubContext(), input, services);
            return (result, ShapeModule.Called);
        }

        [Theory]
        [InlineData("admin", "admin-default[]")]
        [InlineData("admin bogus", "admin-default[bogus]")]
        [InlineData("admin bogus and more words", "admin-default[bogus and more words]")]
        [InlineData("admin filter", "filter-default[]")]
        [InlineData("admin filter bogus", "filter-default[bogus]")]
        [InlineData("admin filter get", "filter-get")]
        [InlineData("admin filter set 175", "filter-set[175]")]
        [InlineData("admin filter set", "filter-set[]")]
        [InlineData("admin filterchannel", "filterchannel-default[]")]
        [InlineData("admin filterchannel remove x", "filterchannel-default[remove x]")]
        public async Task TheMostSpecificCommandWinsAndCatchAllsHandleTheRest(string input, string expected)
        {
            var (result, called) = await RunAsync(input);

            Assert.True(result.IsSuccess, result.ErrorReason);
            Assert.Equal(expected, called);
        }

        [Theory]
        [InlineData("admin filter get extra words", "filter-get")]
        [InlineData("admin filterchannel add general", "filterchannel-add[general,175]")]
        [InlineData("admin filterchannel add general 99", "filterchannel-add[general,99]")]
        [InlineData("admin adminrole set Server Moderators", "adminrole-set[Server Moderators]")]
        [InlineData("admin adminrole set <@&123456789012345678>", "adminrole-set[<@&123456789012345678>]")]
        public async Task ArgumentsAreParsedAsDeclared(string input, string expected)
        {
            var (result, called) = await RunAsync(input);

            Assert.True(result.IsSuccess, result.ErrorReason);
            Assert.Equal(expected, called);
        }

        [Fact]
        public async Task ALongNameForTheChannelAndAnIdParsesWhenTheChannelIsOneWord()
        {
            // "add <channel> <filterId>": a channel name is one word, so a trailing number is the filter id.
            var (result, called) = await RunAsync("admin filterchannel add some-channel 56027");

            Assert.True(result.IsSuccess, result.ErrorReason);
            Assert.Equal("filterchannel-add[some-channel,56027]", called);
        }

        [Fact]
        public async Task AMissingRequiredArgumentFallsBackToTheCatchAll()
        {
            // `adminrole set` without a role: the specific command can't parse, the catch-all of the group takes it.
            var (result, called) = await RunAsync("admin adminrole set");

            Assert.True(result.IsSuccess, result.ErrorReason);
            Assert.Equal("admin-default[adminrole set]", called);
        }

        [Fact]
        public async Task CommandNamesAreCaseInsensitive()
        {
            var (result, called) = await RunAsync("ADMIN Filter GET");

            Assert.True(result.IsSuccess, result.ErrorReason);
            Assert.Equal("filter-get", called);
        }

        [Fact]
        public async Task SearchFindsEveryPathTheBotShouldTreatAsACommand()
        {
            var commands = new CommandService();
            await commands.AddModuleAsync<ShapeModule>(new ServiceCollection().BuildServiceProvider());

            foreach (var input in new[] { "admin", "admin filter get", "admin filter set 1", "admin bogus", "admin filterchannel add a" })
            {
                Assert.True(commands.Search(input).IsSuccess, input);
            }

            Assert.False(commands.Search("filter get").IsSuccess); // only reachable through "admin"
            Assert.False(commands.Search("nonsense").IsSuccess);
        }
    }

    [CollectionDefinition("GroupRouting", DisableParallelization = true)]
    public class GroupRoutingCollection
    {
    }
}
