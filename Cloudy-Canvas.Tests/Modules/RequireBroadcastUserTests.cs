namespace Cloudy_Canvas.Tests.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Modules;
    using Cloudy_Canvas.Settings;
    using Discord;
    using Discord.Commands;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Xunit;

    public class RequireBroadcastUserTests
    {
        public class BroadcastTestModule : ModuleBase<ICommandContext>
        {
            public static string LastMessage { get; set; }

            public static int Runs { get; set; }

            [Command("broadcast", RunMode = RunMode.Sync)]
            [RequireBroadcastUser]
            public Task BroadcastAsync([Remainder] string message = "")
            {
                Runs++;
                LastMessage = message;
                return Task.CompletedTask;
            }
        }

        /// <summary>Implements any Discord interface by answering "default" to everything except the members given.</summary>
        public class StubProxy : DispatchProxy
        {
            public Dictionary<string, object> Values { get; set; } = new();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (Values.TryGetValue(targetMethod.Name, out var value))
                {
                    return value;
                }

                return targetMethod.ReturnType.IsValueType && targetMethod.ReturnType != typeof(void) ? Activator.CreateInstance(targetMethod.ReturnType) : null;
            }
        }

        private sealed class StubContext : ICommandContext
        {
            public StubContext(ulong userId)
            {
                var proxy = DispatchProxy.Create<IUser, StubProxy>();
                ((StubProxy)(object)proxy).Values["get_Id"] = userId;
                User = proxy;
            }

            public IDiscordClient Client => null;

            public IGuild Guild => null;

            public IMessageChannel Channel => null;

            public IUser User { get; }

            public IUserMessage Message => null;
        }

        private static IServiceProvider Services(params ulong[] broadcastUsers)
        {
            var settings = new DiscordSettings();
            settings.BroadcastUserIds.AddRange(broadcastUsers);
            return new ServiceCollection().AddSingleton<IOptions<DiscordSettings>>(Options.Create(settings)).BuildServiceProvider();
        }

        private static Task<PreconditionResult> Check(ulong userId, IServiceProvider services)
        {
            return new RequireBroadcastUserAttribute().CheckPermissionsAsync(new StubContext(userId), null, services);
        }

        [Fact]
        public async Task AListedUserPasses()
        {
            Assert.True((await Check(42, Services(7, 42))).IsSuccess);
        }

        [Fact]
        public async Task AnyoneElseFailsWithAnEmptyReason()
        {
            var result = await Check(43, Services(7, 42));

            Assert.False(result.IsSuccess);
            Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            Assert.Equal(string.Empty, result.ErrorReason);
        }

        [Fact]
        public async Task NobodyPassesWhenTheListIsEmpty()
        {
            Assert.False((await Check(42, Services())).IsSuccess);
        }

        [Fact]
        public async Task NobodyPassesWhenTheSettingsAreMissing()
        {
            Assert.False((await Check(42, new ServiceCollection().BuildServiceProvider())).IsSuccess);
        }

        private static async Task<IResult> RunAsync(ulong userId, string input, IServiceProvider services)
        {
            BroadcastTestModule.Runs = 0;
            BroadcastTestModule.LastMessage = null;
            var commands = new CommandService();
            await commands.AddModuleAsync<BroadcastTestModule>(services);
            return await commands.ExecuteAsync(new StubContext(userId), input, services);
        }

        [Theory]
        [InlineData("broadcast")]
        [InlineData("broadcast hello")]
        [InlineData("broadcast hello there everyone")]
        [InlineData("broadcast \"quoted\" and `code`")]
        public async Task OtherUsersGetSilenceNoMatterWhatTheyType(string input)
        {
            var result = await RunAsync(43, input, Services(42));

            Assert.False(result.IsSuccess);
            Assert.Equal(CommandError.UnmetPrecondition, result.Error);
            Assert.Null(CommandErrorReplies.For(result)); // the bot says nothing at all
            Assert.Equal(0, BroadcastTestModule.Runs);
        }

        [Fact]
        public async Task AListedUserRunsTheCommandWithTheWholeMessage()
        {
            var result = await RunAsync(42, "broadcast hello there, everyone", Services(42));

            Assert.True(result.IsSuccess);
            Assert.Equal(1, BroadcastTestModule.Runs);
            Assert.Equal("hello there, everyone", BroadcastTestModule.LastMessage);
        }

        [Fact]
        public async Task AListedUserCanCallItWithoutAMessage()
        {
            var result = await RunAsync(42, "broadcast", Services(42));

            Assert.True(result.IsSuccess);
            Assert.Equal(string.Empty, BroadcastTestModule.LastMessage);
        }

        [Fact]
        public void TheRealBroadcastCommandIsGatedByTheSameAttribute()
        {
            var method = typeof(AdminModule).GetMethod(nameof(AdminModule.BroadcastCommandAsync));

            Assert.NotNull(method.GetCustomAttribute<RequireBroadcastUserAttribute>());
            Assert.Null(method.GetCustomAttribute<RequireOwnerAttribute>()); // the old, second gate is gone
        }
    }
}
