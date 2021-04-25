namespace Cloudy_Canvas.Helpers
{
    using System.Threading.Tasks;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;

    public static class BadlistHelper
    {
        public static async Task<bool> RemoveYellowTerm(string term, ServerSettings settings, SocketCommandContext context)
        {
            var lower = term.ToLower();
            for (var x = settings.yellowList.list.Count - 1; x >= 0; x--)
            {
                var yellow = settings.yellowList.list[x];
                if (yellow != lower)
                {
                    continue;
                }

                settings.yellowList.list.Remove(yellow);
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
            foreach (var yellow in settings.yellowList.list)
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

        public static async Task<bool> AddYellowTerm(string term, ServerSettings settings, SocketCommandContext context)
        {
            var lower = term.ToLower();
            foreach (var yellow in settings.yellowList.list)
            {
                if (yellow == lower)
                {
                    return false;
                }
            }

            settings.yellowList.list.Add(lower);
            await FileHelper.SaveServerSettingsAsync(settings, context);
            return true;
        }

        public static string CheckRedList(string query, ServerSettings settings)
        {
            var queryList = query.ToLower().Split(", ");
            var parsedList = ParseList(queryList);
            var matchedTerms = "";
            foreach (var red in settings.redList.list)
            {
                foreach (var term in parsedList)
                {
                    if (term != red)
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

        private static string[] ParseList(string[] queryList)
        {
            return queryList;
        }
    }
}
