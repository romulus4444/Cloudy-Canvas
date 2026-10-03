namespace Cloudy_Canvas.Modules
{
    using System;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Restricts a command to the Discord users listed in <see cref="DiscordSettings.BroadcastUserIds"/>. Everyone else fails the
    /// precondition, which the bot answers with silence (as for an unknown command), so the command's existence isn't revealed.
    /// Preconditions run before arguments are parsed, so even a malformed call from someone else stays silent.
    /// </summary>
    public class RequireBroadcastUserAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var allowed = services.GetService<IOptions<DiscordSettings>>()?.Value?.BroadcastUserIds;
            var result = allowed != null && allowed.Contains(context.User.Id)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError(string.Empty);
            return Task.FromResult(result);
        }
    }
}
