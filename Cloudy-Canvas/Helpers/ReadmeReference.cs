namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Writes the README's command reference from <see cref="HelpTopics"/>, so the README and ";help" are one text. The README keeps its
    /// hand-written parts; only what is between the "BEGIN GENERATED" and "END GENERATED" lines is replaced. A test fails when the
    /// README is out of date, and regenerates it when run with UPDATE_SNAPSHOTS=1.
    /// </summary>
    public static class ReadmeReference
    {
        /// <summary>The README documents the default prefix.</summary>
        private const string Prefix = ";";

        private static readonly Regex Region = new(
            @"(?<begin><!-- BEGIN GENERATED: (?<name>\w+)\.[^\n]*-->\n\n)(?<body>.*?)(?<end><!-- END GENERATED: \k<name> -->)",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        private static readonly Regex Usage = new(@"^`(?<usage>[^`]+)`(?: (?<description>.*))?$", RegexOptions.CultureInvariant);

        // An argument like <query> in running text is shown as code, the way the README always has.
        private static readonly Regex Argument = new(@"<[A-Za-z][^<>`]*>", RegexOptions.CultureInvariant);

        /// <summary>The README with each generated region rewritten from the help topics.</summary>
        public static string Update(string readme)
        {
            return Region.Replace(readme, match => match.Groups["begin"].Value + Render(SectionOf(match.Groups["name"].Value)) + match.Groups["end"].Value);
        }

        /// <summary>The reference for one part of the README: each topic as paragraphs, followed by a horizontal rule.</summary>
        public static string Render(HelpSection section)
        {
            var text = new StringBuilder();
            foreach (var topic in HelpTopics.All.Where(topic => topic.Section == section && topic.Text != null))
            {
                if (topic.Name == "admin")
                {
                    // The overview of ";admin" is for the bot; the README spells every setting out.
                    foreach (var setting in HelpTopics.AdminSettings)
                    {
                        AppendTopic(text, HelpText.AdminSettingLines(setting), setting.ReadmeNotes);
                    }
                }
                else
                {
                    AppendTopic(text, topic.Text, topic.ReadmeNotes);
                }
            }

            return text.ToString();
        }

        private static HelpSection SectionOf(string name)
        {
            return Enum.TryParse<HelpSection>(name, true, out var section) && section != HelpSection.None
                ? section
                : throw new InvalidOperationException($"The README has a generated region '{name}', which is not a section of the help.");
        }

        private static void AppendTopic(StringBuilder text, IReadOnlyList<string> lines, IReadOnlyList<string> notes)
        {
            var paragraphs = Paragraphs(lines);
            if (notes != null)
            {
                // Notes follow the introduction of the topic; a topic that starts straight with a command has no introduction, so they go last.
                var firstCommand = paragraphs.FindIndex(paragraph => paragraph.StartsWith('`'));
                paragraphs.InsertRange(firstCommand > 0 ? firstCommand : paragraphs.Count, notes.Select(Format));
            }

            text.Append(string.Join("\n\n", paragraphs)).Append("\n\n---\n\n");
        }

        /// <summary>
        /// The help's lines as markdown paragraphs. Headings and the "only admins may use this" notices are dropped (the README says that
        /// once, above the admin commands), a command and its description share a paragraph, and a summary in italics becomes plain text.
        /// </summary>
        private static List<string> Paragraphs(IReadOnlyList<string> lines)
        {
            var paragraphs = new List<string>();
            string pendingCommand = null;
            foreach (var line in lines.Select(line => line.Replace("{p}", Prefix, StringComparison.Ordinal)))
            {
                if (line.StartsWith("**__", StringComparison.Ordinal) || line.StartsWith("__", StringComparison.Ordinal) || line.StartsWith("*Only users with the specified admin role", StringComparison.Ordinal))
                {
                    continue;
                }

                var usage = Usage.Match(line);
                if (pendingCommand != null)
                {
                    // The command was on a line of its own; what follows is a note on it (in italics) or its description.
                    if (IsItalic(line))
                    {
                        pendingCommand += " " + line;
                    }
                    else
                    {
                        paragraphs.Add(pendingCommand + " " + Format(line));
                        pendingCommand = null;
                    }

                    continue;
                }

                if (usage.Success && !usage.Groups["description"].Success)
                {
                    pendingCommand = line;
                }
                else if (usage.Success)
                {
                    paragraphs.Add($"`{usage.Groups["usage"].Value}` {Format(usage.Groups["description"].Value)}");
                }
                else
                {
                    paragraphs.Add(Format(IsItalic(line) ? line[1..^1] : line));
                }
            }

            if (pendingCommand != null)
            {
                paragraphs.Add(pendingCommand);
            }

            return paragraphs;
        }

        private static bool IsItalic(string line)
        {
            return line.Length > 2 && line[0] == '*' && line[^1] == '*' && line[1] != '*';
        }

        /// <summary>Shows each &lt;argument&gt; in running text as code; text already in backticks is left alone.</summary>
        private static string Format(string text)
        {
            var parts = text.Split('`');
            for (var i = 0; i < parts.Length; i += 2)
            {
                parts[i] = Argument.Replace(parts[i], match => $"`{match.Value}`");
            }

            return string.Join('`', parts);
        }
    }
}
