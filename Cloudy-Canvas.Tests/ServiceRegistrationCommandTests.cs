namespace Cloudy_Canvas.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Modules;
    using Discord.Commands;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public class ServiceRegistrationCommandTests
    {
        [Fact]
        public async Task CommandsRunAsynchronouslyByDefault()
        {
            // The admin command group relies on this instead of repeating RunMode.Async on every command: a synchronous command would
            // hold up the Discord gateway thread while it talks to Discord and Manebooru.
            using var provider = TestServices.Build();
            var commands = provider.GetRequiredService<CommandService>();

            var module = await commands.AddModuleAsync<AdminSettingsModule>(provider);

            Assert.True(module.Commands.Count > 30);
            Assert.All(module.Commands, command => Assert.Equal(RunMode.Async, command.RunMode));
        }
    }
}
