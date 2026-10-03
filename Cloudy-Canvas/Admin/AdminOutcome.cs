namespace Cloudy_Canvas.Admin
{
    /// <summary>The result of an admin settings operation: what to tell the admin, and whether settings changed and need saving.</summary>
    /// <param name="Message">The reply.</param>
    /// <param name="Success">False when the request couldn't be carried out (an unknown channel or role, a missing argument).</param>
    /// <param name="Changed">True when the settings were modified.</param>
    public sealed record AdminOutcome(string Message, bool Success = true, bool Changed = false)
    {
        public static AdminOutcome Failure(string message) => new(message, false);

        public static AdminOutcome Info(string message) => new(message);

        public static AdminOutcome Updated(string message) => new(message, true, true);
    }
}
