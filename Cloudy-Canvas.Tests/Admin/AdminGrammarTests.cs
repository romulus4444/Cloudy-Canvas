namespace Cloudy_Canvas.Tests.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Admin;
    using Cloudy_Canvas.Modules;
    using Discord.Commands;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public class AdminGrammarTests
    {
        [Theory]
        [InlineData("", "You need to specify an admin command.", "admin: <FAIL>")]
        [InlineData("   ", "You need to specify an admin command.", "admin: <FAIL>")]
        [InlineData(null, "You need to specify an admin command.", "admin: <FAIL>")]
        [InlineData("bogus", "Invalid command `bogus`", "admin: bogus <FAIL>")]
        [InlineData("bogus and more words", "Invalid command `bogus`", "admin: bogus <FAIL>")]
        [InlineData("filter", "You must specify a subcommand.", "admin: filter <FAIL>")]
        [InlineData("watchrole", "You must specify a subcommand.", "admin: watchrole <FAIL>")]
        [InlineData("filter bogus", "Invalid command bogus", "admin: filter bogus <FAIL>")]
        [InlineData("adminchannel clear", "Invalid command clear", "admin: adminchannel clear <FAIL>")] // the admin channel can't be cleared
        [InlineData("ignorerole set x", "Invalid command set", "admin: ignorerole set <FAIL>")] // lists have add/remove, not set
        [InlineData("watchchannel add x", "Invalid command add", "admin: watchchannel add <FAIL>")]
        public void WhatNoSpecificCommandTookIsExplained(string rest, string message, string log)
        {
            var reply = AdminGrammar.Explain(rest);

            Assert.Equal(message, reply.Message);
            Assert.Equal(log, reply.LogText);
        }

        [Fact]
        public void AKnownActionThatFailedToParseIsNotCalledInvalid()
        {
            // For example "filterchannel add general abc": the action exists, its arguments don't make sense.
            var reply = AdminGrammar.Explain("filterchannel add general abc");

            Assert.StartsWith("I couldn't understand those arguments for `filterchannel add`", reply.Message);
            Assert.Equal("admin: filterchannel add <FAIL>", reply.LogText);
        }

        [Fact]
        public void SettingsAndActionsAreMatchedWithoutRegardToCase()
        {
            Assert.Equal("You must specify a subcommand.", AdminGrammar.Explain("FILTER").Message);
            Assert.StartsWith("I couldn't understand those arguments", AdminGrammar.Explain("Filter SET").Message);
        }

        [Fact]
        public void TheTableCoversTheTwelveSettings()
        {
            Assert.Equal(
                new[]
                {
                    "adminchannel", "adminrole", "allowuser", "filter", "filterchannel", "ignorechannel", "ignorerole", "logchannel",
                    "reportchannel", "reportrole", "watchchannel", "watchrole",
                },
                AdminGrammar.Actions.Keys.OrderBy(k => k));
        }

        [Fact]
        public void EverySettingCanBeRead()
        {
            Assert.All(AdminGrammar.Actions, pair => Assert.Contains("get", pair.Value));
        }
    }

    /// <summary>Compares the grammar table with the commands the real admin module declares, in both directions.</summary>
    public class AdminModuleGrammarTests
    {
        // Discord.Net checks a module's constructor dependencies when the module is added, so use the bot's real container.
        private static readonly Lazy<ServiceProvider> Provider = new(() => TestServices.Build());

        private static async Task<(CommandService Commands, ModuleInfo Module)> BuildAsync()
        {
            var commands = new CommandService();
            var module = await commands.AddModuleAsync<AdminSettingsModule>(Provider.Value);
            return (commands, module);
        }

        private static IEnumerable<string> PathsOf(ModuleInfo module)
        {
            return module.Commands
                .SelectMany(command => command.Aliases)
                .Where(alias => alias.StartsWith("admin ", StringComparison.OrdinalIgnoreCase))
                .Select(alias => alias.Substring("admin ".Length).ToLowerInvariant())
                .Distinct();
        }

        [Fact]
        public async Task EverySettingAndActionInTheGrammarHasACommand()
        {
            var (_, module) = await BuildAsync();
            var declared = PathsOf(module).ToHashSet();

            foreach (var (setting, actions) in AdminGrammar.Actions)
            {
                foreach (var action in actions)
                {
                    Assert.True(declared.Contains($"{setting} {action}"), $"'admin {setting} {action}' is in the grammar but the module has no such command");
                }
            }
        }

        [Fact]
        public async Task EveryCommandOfTheModuleIsInTheGrammar()
        {
            var (_, module) = await BuildAsync();

            foreach (var path in PathsOf(module))
            {
                var words = path.Split(' ');
                Assert.True(words.Length == 2, $"'admin {path}' should be '<setting> <action>'");
                Assert.True(AdminGrammar.Actions.TryGetValue(words[0], out var actions), $"'admin {path}': '{words[0]}' is not a setting in the grammar");
                Assert.Contains(words[1], actions);
            }
        }

        [Fact]
        public async Task EveryPathIsFoundAndRoutedToItsOwnCommandNotTheCatchAll()
        {
            var (commands, _) = await BuildAsync();

            foreach (var (setting, actions) in AdminGrammar.Actions)
            {
                foreach (var action in actions)
                {
                    var path = $"admin {setting} {action}";
                    var search = commands.Search($"{path} something");

                    Assert.True(search.IsSuccess, path);
                    Assert.Contains(search.Commands, match => match.Command.Aliases.Contains(path, StringComparer.OrdinalIgnoreCase));
                }
            }
        }

        [Fact]
        public async Task OnlyTheCatchAllHasALowerPriorityAndItHasNoName()
        {
            var (_, module) = await BuildAsync();

            var catchAll = Assert.Single(module.Commands, command => command.Priority < 0);
            Assert.Equal("admin", catchAll.Aliases.Single(), ignoreCase: true);
            Assert.All(module.Commands.Where(command => command != catchAll), command => Assert.Equal(0, command.Priority));
        }

        [Fact]
        public async Task TheWholeGroupIsRestrictedToBotAdmins()
        {
            var (_, module) = await BuildAsync();

            Assert.Contains(module.Preconditions, precondition => precondition is RequireBotAdminAttribute);
        }

        [Fact]
        public async Task TheCommandsTakeAFreeTextArgumentSoRoleNamesWithSpacesWork()
        {
            var (_, module) = await BuildAsync();

            foreach (var command in module.Commands.Where(c => c.Aliases.Any(a => a.EndsWith(" set") || a.EndsWith(" add") || a.EndsWith(" remove"))))
            {
                if (command.Aliases.Any(a => a.StartsWith("admin filterchannel add", StringComparison.OrdinalIgnoreCase)))
                {
                    continue; // channel and filter id: a channel name is one word
                }

                Assert.True(command.Parameters.Single().IsRemainder, string.Join(", ", command.Aliases));
            }
        }
    }
}
