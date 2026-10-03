namespace Cloudy_Canvas.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Cloudy_Canvas.Settings;

    /// <summary>What ";watchlist" does: the reply, the line to log, whether the settings changed and whether the log line goes to the file log.</summary>
    public sealed record WatchListResult(string Message, string LogText, bool Changed = false, bool LogToFile = false);

    /// <summary>
    /// The ";watchlist" command as plain logic over the server's settings, so it can be tested without Discord. The module loads the
    /// settings, calls <see cref="Run"/>, saves them if <see cref="WatchListResult.Changed"/>, then replies and logs.
    /// </summary>
    public static class WatchListOperations
    {
        public static WatchListResult Run(string command, string term, ServerSettings settings)
        {
            return command switch
            {
                "" => new WatchListResult("You must specify a subcommand.", "watchlist: <FAIL>"),
                "add" => Add(term, settings),
                "remove" => Remove(term, settings),
                "get" => Get(settings),
                "clear" => Clear(settings),
                _ => new WatchListResult("Invalid subcommand", $"watchlist: {command} <FAIL>"),
            };
        }

        private static WatchListResult Add(string typed, ServerSettings settings)
        {
            // Terms are stored in lower case; "a, b" adds two terms.
            var terms = (typed ?? string.Empty).ToLowerInvariant().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (terms.Length == 0)
            {
                return new WatchListResult("You must specify a term to add.", "watchlist: add <FAIL>");
            }

            var added = new List<string>();
            var alreadyThere = new List<string>();
            foreach (var term in terms)
            {
                // Checked against the list as it grows, so "a, a" adds the term once and reports the repeat.
                if (settings.WatchList.Contains(term, StringComparer.OrdinalIgnoreCase))
                {
                    alreadyThere.Add(term);
                }
                else
                {
                    settings.WatchList.Add(term);
                    added.Add(term);
                }
            }

            if (alreadyThere.Count == 0)
            {
                var addedText = Quoted(added);
                return new WatchListResult($"Added {addedText} to the watchlist.", $"watchlist: add {addedText} <SUCCESS>", Changed: true, LogToFile: true);
            }

            if (added.Count == 0)
            {
                return new WatchListResult("All terms entered are already on the watchlist.", $"watchlist: add <FAIL> {typed}");
            }

            var failedText = Quoted(alreadyThere);
            var someAddedText = Quoted(added);
            return new WatchListResult(
                $"Added {someAddedText} to the watchlist, and the watchlist already contained {failedText}.",
                $"watchlist: add {someAddedText} <FAIL> {failedText}",
                Changed: true);
        }

        private static WatchListResult Remove(string typed, ServerSettings settings)
        {
            var term = (typed ?? string.Empty).Trim();
            if (term.Length == 0)
            {
                return new WatchListResult("You must specify a term to remove.", "watchlist: remove <FAIL>");
            }

            if (settings.WatchList.RemoveAll(watch => string.Equals(watch, term, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                return new WatchListResult($"Removed `{term}` from the watchlist.", $"watchlist: remove {term} <SUCCESS>", Changed: true, LogToFile: true);
            }

            return new WatchListResult($"`{term}` was not on the watchlist.", $"watchlist: remove {term} <FAIL>");
        }

        private static WatchListResult Get(ServerSettings settings)
        {
            var terms = settings.WatchList.Count == 0
                ? "The watchlist is currently empty."
                : string.Join(", ", settings.WatchList.Select(item => $"`{item}`"));
            return new WatchListResult($"__Watchlist Terms:__{Environment.NewLine}{terms}", "watchlist: get");
        }

        private static WatchListResult Clear(ServerSettings settings)
        {
            settings.WatchList.Clear();
            return new WatchListResult("Watchlist cleared", "watchlist: clear", Changed: true, LogToFile: true);
        }

        /// <summary>`a`, `a` and `b`, or `a`, `b`, and `c`: each term in backticks, joined the way a sentence would.</summary>
        public static string Quoted(IReadOnlyList<string> terms)
        {
            var text = new StringBuilder();
            for (var i = 0; i < terms.Count; i++)
            {
                text.Append('`').Append(terms[i]).Append('`');
                if (i < terms.Count - 2)
                {
                    text.Append(", ");
                }
                else if (i == terms.Count - 2)
                {
                    text.Append(terms.Count == 2 ? " and " : ", and ");
                }
            }

            return text.ToString();
        }
    }
}
