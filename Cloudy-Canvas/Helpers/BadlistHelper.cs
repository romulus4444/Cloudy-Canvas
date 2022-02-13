namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;

    public static class BadlistHelper
    {
        public static async Task<bool> RemoveWatchTerm(string term, ServerSettings settings, SocketCommandContext context)
        {
            var lower = term.ToLower();
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

        public static string CheckWatchList(string query, ServerSettings settings)
        {
            var queryList = query.ToLower().Split(", ");
            var parsedList = ParseList(queryList);
            var matchedTerms = "";
            foreach (var watch in settings.WatchList)
            {
                foreach (var term in parsedList)
                {
                    if (term != watch)
                    {
                        continue;
                    }

                    if (matchedTerms == "")
                    {
                        matchedTerms += term;
                    }
                    else
                    {
                        matchedTerms += $", {term}";
                    }
                }
            }

            return matchedTerms;
        }

        public static async Task<Tuple<List<string>, List<string>>> AddWatchTerm(string term, ServerSettings settings, SocketCommandContext context)
        {
            var termList = term.ToLower().Split(", ");
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
            var combined = new Tuple<List<string>, List<string>> (addList, failList);
            return combined;
        }

        private static string[] ParseList(string[] queryList)
        {
            var parsedList = new List<string>();
            foreach (var query in queryList)
            {
                var parsedString = "";
                foreach (var character in query)
                {
                    if (character == '"')
                    {
                        continue;
                    }

                    parsedString += character;
                }
                parsedList.Add(parsedString.Trim());
            }
            
            return parsedList.ToArray();
        }
    }
}
