namespace Cloudy_Canvas.Tests.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Modules;
    using Discord.Commands;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Which commands anyone can use and which are restricted. Splitting or moving command code makes it easy to lose the check that used to
    /// sit at the top of a command, so the commands left open are listed here explicitly: an admin command that loses its gate fails this test.
    /// </summary>
    public class CommandGatingTests
    {
        // Everything anyone in a server may run (subject to the server's ignore rules). Nothing else may be left ungated.
        private static readonly string[] OpenCommands =
        {
            "<blank message>", "<mention>", "about", "featured", "getspoilers", "help", "id", "origin", "pick", "pickrecent", "report", "tags",
        };

        private static readonly Lazy<ServiceProvider> Provider = new(() => TestServices.Build());

        private static async Task<List<(CommandInfo Command, bool Gated)>> AllCommandsAsync()
        {
            var commands = new CommandService();
            var modules = await commands.AddModulesAsync(typeof(Program).Assembly, Provider.Value);
            var result = new List<(CommandInfo, bool)>();
            foreach (var module in commands.Modules)
            {
                foreach (var command in module.Commands)
                {
                    result.Add((command, IsGated(command)));
                }
            }

            return result;
        }

        private static bool IsGated(CommandInfo command)
        {
            var preconditions = command.Preconditions.ToList();
            for (var module = command.Module; module != null; module = module.Parent)
            {
                preconditions.AddRange(module.Preconditions);
            }

            return preconditions.Any(p => p is RequireBotAdminAttribute || p is RequireBroadcastUserAttribute || p is RequireUserPermissionAttribute);
        }

        private static string Name(CommandInfo command) => command.Aliases.First();

        [Fact]
        public async Task OnlyTheListedCommandsAreOpenToEveryone()
        {
            var open = (await AllCommandsAsync()).Where(entry => !entry.Gated).Select(entry => Name(entry.Command)).OrderBy(n => n).ToList();

            Assert.Equal(OpenCommands.OrderBy(n => n), open);
        }

        [Fact]
        public async Task EveryAdminCommandIsGated()
        {
            var all = await AllCommandsAsync();

            foreach (var name in new[] { "setup", "getsettings", "refreshlists", "echo", "setprefix", "listentobots", "safemode", "alias", "watchlist", "log", "broadcast" })
            {
                var command = Assert.Single(all, entry => Name(entry.Command) == name);
                Assert.True(command.Gated, $"'{name}' must be restricted");
            }

            Assert.All(all.Where(entry => Name(entry.Command).StartsWith("admin", StringComparison.OrdinalIgnoreCase)), entry => Assert.True(entry.Gated, Name(entry.Command)));
        }

        [Fact]
        public async Task EveryGatedCommandHasItsHelpHiddenFromNonAdmins()
        {
            // The first word of each restricted command must be an admin help topic, so ";help <command>" doesn't describe it to everyone.
            var gated = (await AllCommandsAsync()).Where(entry => entry.Gated).Select(entry => Name(entry.Command).Split(' ')[0]).Distinct().ToList();

            Assert.True(gated.Count >= 11, $"expected to find the admin commands, found {gated.Count}");
            foreach (var word in gated)
            {
                Assert.True(HelpText.IsAdminTopic(word), $"'{word}' is a restricted command; give it an AdminOnly entry in HelpTopics so its help is hidden from non-admins");
            }
        }

        [Fact]
        public async Task BroadcastIsGatedByItsOwnListNotByTheAdminRole()
        {
            var broadcast = Assert.Single(await AllCommandsAsync(), entry => Name(entry.Command) == "broadcast").Command;

            Assert.Contains(broadcast.Preconditions, p => p is RequireBroadcastUserAttribute);
            Assert.DoesNotContain(broadcast.Preconditions, p => p is RequireBotAdminAttribute);
            Assert.DoesNotContain(broadcast.Module.Preconditions, p => p is RequireBotAdminAttribute);
        }
    }
}
