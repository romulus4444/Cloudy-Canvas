namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;

    public static class BadlistHelper
    {
        public static async Task<bool> RemoveYellowTerm(string term, ServerSettings settings, SocketCommandContext context)
        {
            var lower = term.ToLower();
            for (var x = settings.yellowList.Count - 1; x >= 0; x--)
            {
                var yellow = settings.yellowList[x];
                if (yellow != lower)
                {
                    continue;
                }

                settings.yellowList.Remove(yellow);
                await FileHelper.SaveServerSettingsAsync(settings, context);
                return true;
            }

            return false;
        }

        public static string CheckYellowList(string query, ServerSettings settings)
        {
            var queryList = query.ToLower().Split(", ");
            var parsedList = ParseList(queryList);
            var matchedTerms = "";
            foreach (var yellow in settings.yellowList)
            {
                foreach (var term in parsedList)
                {
                    if (term != yellow)
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

        public static async Task<Tuple<List<string>, List<string>>> AddYellowTerm(string term, ServerSettings settings, SocketCommandContext context)
        {
            var termList = term.ToLower().Split(", ");
            var failList = new List<string>();
            var addList = new List<string>();
            foreach (var singleTerm in termList)
            {
                var failed = false;
                foreach (var yellow in settings.yellowList)
                {
                    if (yellow != singleTerm)
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

                settings.yellowList.Add(singleTerm);
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
