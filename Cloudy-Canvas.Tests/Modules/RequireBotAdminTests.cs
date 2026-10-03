namespace Cloudy_Canvas.Tests.Modules
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Modules;
    using Discord;
    using Discord.Commands;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public class RequireBotAdminTests
    {
        public class GatedModule : ModuleBase<ICommandContext>
        {
            public static int Runs { get; set; }

            [Command("secret")]
            [RequireBotAdmin]
            public Task SecretAsync([Remainder] string rest = "")
            {
                Runs++;
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

        [Fact]
        public async Task AContextThatIsNotAServerCommandContextIsRefusedQuietly()
        {
            var result = await new RequireBotAdminAttribute().CheckPermissionsAsync(new StubContext(), null, new ServiceCollection().BuildServiceProvider());

            Assert.False(result.IsSuccess);
            Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            Assert.Equal(string.Empty, result.ErrorReason);
        }

        [Theory]
        [InlineData("secret")]
        [InlineData("secret with some arguments")]
        public async Task ARefusedCallRunsNothingAndTheBotStaysSilent(string input)
        {
            GatedModule.Runs = 0;
            var commands = new CommandService();
            var services = new ServiceCollection().BuildServiceProvider();
            await commands.AddModuleAsync<GatedModule>(services);

            var result = await commands.ExecuteAsync(new StubContext(), input, services);

            Assert.False(result.IsSuccess);
            Assert.Null(CommandErrorReplies.For(result));
            Assert.Equal(0, GatedModule.Runs);
        }

        [Fact]
        public void TheAttributeCanBePutOnAWholeModuleOrOnOneCommand()
        {
            var usage = typeof(RequireBotAdminAttribute).GetCustomAttribute<AttributeUsageAttribute>();

            Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Class));
            Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Method));
        }
    }
}
