namespace Cloudy_Canvas.Helpers
{
    using Discord.Commands;

    public static class CommandErrorReplies
    {
        /// <summary>
        /// The message to send to the user when a command failed, or null to stay silent.
        /// Unknown commands and unmet preconditions (admin-only commands used by non-admins) stay silent, as they always have,
        /// so the bot doesn't advertise or answer commands the user can't use.
        /// </summary>
        public static string For(IResult result)
        {
            if (result == null || result.IsSuccess)
            {
                return null;
            }

            return result.Error switch
            {
                CommandError.Exception => "Something went wrong on my end. Please try again in a moment.",
                CommandError.BadArgCount or CommandError.ParseFailed or CommandError.ObjectNotFound or CommandError.MultipleMatches
                    => "I couldn't understand that. Use the help command to see how it works.",
                _ => null,
            };
        }
    }
}
