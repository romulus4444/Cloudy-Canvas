namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.Globalization;
    using System.Text;

    /// <summary>Where a logged command came from.</summary>
    /// <param name="IsPrivate">True for a direct message.</param>
    public sealed record LogSource(bool IsPrivate, string UserName, ulong UserId, string GuildName, ulong GuildId, string ChannelName, ulong ChannelId);

    /// <summary>
    /// How a command is written to the log. Anything a user typed is made safe first: a message can contain line breaks, and an
    /// unescaped one would let a user write extra lines into the log that look like entries made by someone else.
    /// </summary>
    public static class LogLine
    {
        private const char LineSeparator = (char)0x2028;
        private const char ParagraphSeparator = (char)0x2029;

        /// <summary>
        /// One log line (or, for a file, one entry or the header at the top of a file).
        /// </summary>
        /// <param name="source">Where the command came from.</param>
        /// <param name="message">What happened, including whatever the user typed.</param>
        /// <param name="utcNow">The time shown at the start of an entry.</param>
        /// <param name="fileEntry">True for an entry in a channel's log file, which leaves out what the file's header already says.</param>
        /// <param name="header">True for the header line a log file starts with: only where the log is from, no time or message.</param>
        public static string Format(LogSource source, string message, DateTime utcNow, bool fileEntry = false, bool header = false)
        {
            var line = new StringBuilder();
            if (!header)
            {
                line.Append(CultureInfo.InvariantCulture, $"[{utcNow:s}] ");
            }

            if (source.IsPrivate)
            {
                if (!fileEntry)
                {
                    line.Append(CultureInfo.InvariantCulture, $"DM with @{Escape(source.UserName)} ({source.UserId})");
                }

                if (!fileEntry && !header)
                {
                    line.Append(", ");
                }

                if (!header)
                {
                    line.Append(Escape(message));
                }
            }
            else
            {
                if (!fileEntry)
                {
                    line.Append(CultureInfo.InvariantCulture, $"server: {Escape(source.GuildName)} ({source.GuildId}) #{Escape(source.ChannelName)} ({source.ChannelId})");
                }

                if (!fileEntry && !header)
                {
                    line.Append(' ');
                }

                if (!header)
                {
                    line.Append(CultureInfo.InvariantCulture, $"@{Escape(source.UserName)} ({source.UserId}), ");
                    line.Append(Escape(message));
                }
            }

            if (fileEntry || header)
            {
                line.Append(Environment.NewLine);
            }

            return line.ToString();
        }

        /// <summary>
        /// The text with line breaks and other control characters written out (as \n, \r, \t or \uXXXX) so it stays on one line.
        /// Ordinary text, including everything printable, comes back unchanged.
        /// </summary>
        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text ?? string.Empty;
            }

            StringBuilder escaped = null;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                var replacement = c switch
                {
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    _ when char.IsControl(c) || c is LineSeparator or ParagraphSeparator => string.Create(CultureInfo.InvariantCulture, $"\\u{(int)c:x4}"),
                    _ => null,
                };

                if (replacement == null)
                {
                    escaped?.Append(c);
                    continue;
                }

                escaped ??= new StringBuilder(text.Length + 8).Append(text, 0, i);
                escaped.Append(replacement);
            }

            return escaped?.ToString() ?? text;
        }
    }
}
