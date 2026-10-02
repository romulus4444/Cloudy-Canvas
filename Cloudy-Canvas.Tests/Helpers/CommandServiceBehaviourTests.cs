namespace Cloudy_Canvas.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Discord;
    using Discord.Commands;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Pins down how Discord.Net's CommandService reports failures for RunMode.Async commands, which the bot's
    /// CommandExecuted handler relies on, using a test module and a stub context (no Discord connection needed).
    /// </summary>
    public class CommandServiceBehaviourTests
    {
        public class TestModule : ModuleBase<ICommandContext>
        {
            [Command("ok", RunMode = RunMode.Async)]
            public Task OkAsync() => Task.CompletedTask;

            [Command("boom", RunMode = RunMode.Async)]
            public async Task BoomAsync()
            {
                await Task.Yield();
                throw new InvalidOperationException("kaboom");
            }

            [Command("number", RunMode = RunMode.Async)]
            public Task NumberAsync(int value) => Task.CompletedTask;
        }

        private sealed class StubContext : ICommandContext
        {
            public IDiscordClient Client => null;

            public IGuild Guild => null;

            public IMessageChannel Channel => null;

            public IUser User => null;

            public IUserMessage Message => null;
        }

        private sealed class Outcome
        {
            public TaskCompletionSource<IResult> Executed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public List<LogMessage> Logs { get; } = new();
        }

        private static async Task<(IResult Returned, Outcome Outcome)> RunAsync(string input)
        {
            var commands = new CommandService();
            var outcome = new Outcome();
            commands.CommandExecuted += (_, _, result) =>
            {
                outcome.Executed.TrySetResult(result);
                return Task.CompletedTask;
            };
            commands.Log += message =>
            {
                lock (outcome.Logs)
                {
                    outcome.Logs.Add(message);
                }

                return Task.CompletedTask;
            };

            var services = new ServiceCollection().BuildServiceProvider();
            await commands.AddModuleAsync<TestModule>(services);

            var returned = await commands.ExecuteAsync(new StubContext(), input, services);
            return (returned, outcome);
        }

        private static async Task<IResult> ExecutedAsync(Outcome outcome)
        {
            var finished = await Task.WhenAny(outcome.Executed.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.Same(outcome.Executed.Task, finished); // CommandExecuted must fire
            return await outcome.Executed.Task;
        }

        [Fact]
        public async Task ASuccessfulCommandStaysSilent()
        {
            var (_, outcome) = await RunAsync("ok");

            var result = await ExecutedAsync(outcome);

            Assert.True(result.IsSuccess);
            Assert.Null(CommandErrorReplies.For(result));
        }

        [Fact]
        public async Task AnExceptionInAnAsyncCommandIsReportedThroughCommandExecutedAndLogged()
        {
            var (returned, outcome) = await RunAsync("boom");

            var result = await ExecutedAsync(outcome);

            Assert.True(returned.IsSuccess); // ExecuteAsync itself already returned: the command runs on its own task
            Assert.False(result.IsSuccess);
            Assert.Equal(CommandError.Exception, result.Error);
            Assert.Equal("Something went wrong on my end. Please try again in a moment.", CommandErrorReplies.For(result));
            lock (outcome.Logs)
            {
                Assert.Contains(outcome.Logs, m => m.Severity == LogSeverity.Error && m.Exception != null);
            }
        }

        [Fact]
        public async Task ABadArgumentGetsAHelpfulReply()
        {
            var (returned, outcome) = await RunAsync("number notanumber");

            Assert.False(returned.IsSuccess);
            Assert.Equal(CommandError.ParseFailed, returned.Error);
            Assert.Equal("I couldn't understand that. Use the help command to see how it works.", CommandErrorReplies.For(returned));

            // CommandExecuted is raised for these failures too, so the bot replies from that one place only
            // (and must not also reply from the value returned by ExecuteAsync).
            var viaEvent = await ExecutedAsync(outcome);
            Assert.Equal(CommandError.ParseFailed, viaEvent.Error);
        }

        [Fact]
        public async Task AMissingArgumentGetsAHelpfulReply()
        {
            var (returned, outcome) = await RunAsync("number");

            Assert.False(returned.IsSuccess);
            Assert.Equal(CommandError.BadArgCount, returned.Error);
            Assert.NotNull(CommandErrorReplies.For(returned));
            Assert.Equal(CommandError.BadArgCount, (await ExecutedAsync(outcome)).Error);
        }

        [Fact]
        public async Task AnUnknownCommandStaysSilent()
        {
            var (returned, _) = await RunAsync("nosuchcommand");

            Assert.Equal(CommandError.UnknownCommand, returned.Error);
            Assert.Null(CommandErrorReplies.For(returned));
        }

        [Theory]
        [InlineData(CommandError.UnmetPrecondition)]
        [InlineData(CommandError.UnknownCommand)]
        [InlineData(CommandError.Unsuccessful)]
        public void OtherFailuresStaySilent(CommandError error)
        {
            Assert.Null(CommandErrorReplies.For(ExecuteResult.FromError(error, "x")));
        }

        [Fact]
        public void NullAndSuccessGiveNoReply()
        {
            Assert.Null(CommandErrorReplies.For(null));
            Assert.Null(CommandErrorReplies.For(ExecuteResult.FromSuccess()));
        }
    }
}
