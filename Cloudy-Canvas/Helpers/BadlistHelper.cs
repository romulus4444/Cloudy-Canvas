namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;

    public static class BadlistHelper
    {
        public static async Task<bool> RemoveWatchTerm(string term, ServerSettings settings, SocketCommandContext context)
        {
            var lower = term.ToLowerInvariant();
            for (var x = settings.WatchList.Count - 1; x >= 0; x--)
            {
                var watch = settings.WatchList[x];
                if (watch != lower)
                {
                    continue;
                }

                settings.WatchList.Remove(watch);
                await FileHelper.SaveServerSettingsAsync(settings, context);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the (comma separated) search terms in <paramref name="query"/> that hit the watchlist, or an empty string if none do.
        /// Terms are pulled out of the query however they are written: separated by commas (with or without a space), grouped in parentheses,
        /// combined with <c>||</c>/<c>&amp;&amp;</c>/<c>AND</c>/<c>OR</c>, negated, quoted or wrapped in wildcards. <c>id:123</c> matches a watched "123".
        /// </summary>
        public static string CheckWatchList(string query, ServerSettings settings)
        {
            var terms = ExtractTerms(query);
            var matched = new List<string>();
            foreach (var watch in settings.WatchList)
            {
                var normalizedWatch = NormalizeTerm(watch);
                if (normalizedWatch.Length == 0)
                {
                    continue;
                }

                foreach (var term in terms)
                {
                    if (term == normalizedWatch && !matched.Contains(term))
                    {
                        matched.Add(term);
                    }
                }
            }

            return string.Join(", ", matched);
        }

        public static async Task<Tuple<List<string>, List<string>>> AddWatchTerm(string term, ServerSettings settings, SocketCommandContext context)
        {
            var termList = term.ToLowerInvariant().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var failList = new List<string>();
            var addList = new List<string>();
            foreach (var singleTerm in termList)
            {
                var failed = false;
                foreach (var watch in settings.WatchList)
                {
                    if (watch != singleTerm)
                    {
                        continue;
                    }

                    failList.Add(singleTerm);
                    failed = true;
                }

                if (failed)
                {
                    continue;
                }

                settings.WatchList.Add(singleTerm);
                addList.Add(singleTerm);
            }

            await FileHelper.SaveServerSettingsAsync(settings, context);
            var combined = new Tuple<List<string>, List<string>>(addList, failList);
            return combined;
        }

        private static readonly Regex OperatorRegex = new(@"\|\||&&|\s+(?:and|or)\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

        private static List<string> ExtractTerms(string query)
        {
            var terms = new List<string>();
            var lowered = (query ?? string.Empty).ToLowerInvariant().Replace("\"", string.Empty);
            foreach (var commaPart in lowered.Split(','))
            {
                // Check each comma separated part whole (tags can contain parentheses), then again with groups and operators taken apart.
                var pieces = new List<string> { commaPart };
                pieces.AddRange(commaPart.Split('(', ')'));
                foreach (var piece in pieces)
                {
                    AddTerm(terms, piece);
                    foreach (var operand in OperatorRegex.Split(piece))
                    {
                        AddTerm(terms, operand);
                    }
                }
            }

            return terms;
        }

        private static void AddTerm(List<string> terms, string raw)
        {
            var term = NormalizeTerm(raw);
            if (term.Length == 0)
            {
                return;
            }

            if (!terms.Contains(term))
            {
                terms.Add(term);
            }

            if (term.StartsWith("id:", StringComparison.Ordinal))
            {
                var id = term[3..].Trim();
                if (id.Length > 0 && !terms.Contains(id))
                {
                    terms.Add(id);
                }
            }
        }

        private static string NormalizeTerm(string raw)
        {
            var term = WhitespaceRegex.Replace((raw ?? string.Empty).Trim().ToLowerInvariant(), " ");
            while (term.StartsWith('-') || term.StartsWith('!'))
            {
                term = term[1..].TrimStart();
            }

            if (term.StartsWith("not ", StringComparison.Ordinal))
            {
                term = term[4..].TrimStart();
            }

            return term.Trim('*', ' ');
        }
    }
}
