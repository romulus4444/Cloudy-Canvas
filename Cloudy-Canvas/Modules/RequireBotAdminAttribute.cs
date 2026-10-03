namespace Cloudy_Canvas.Modules
{
    using System;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Discord.Commands;

    /// <summary>
    /// Restricts a command, or every command of a module, to the bot's admins: users with the server's admin role, server administrators,
    /// and (before an admin role has been set) members who can manage the server. Anyone else fails the precondition, which the bot
    /// answers with silence, so the admin commands aren't revealed to people who can't use them. Preconditions run before arguments are
    /// parsed, so even a malformed call from someone else stays silent.
    /// This replaces the check each admin command used to repeat at the top of its body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireBotAdminAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context is not SocketCommandContext socketContext)
            {
                return PreconditionResult.FromError(string.Empty);
            }

            var settings = await FileHelper.LoadServerSettingsAsync(socketContext);
            return await DiscordHelper.IsBotAdminAsync(socketContext, settings)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError(string.Empty);
        }
    }
}
