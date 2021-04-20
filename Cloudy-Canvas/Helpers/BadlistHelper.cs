namespace Cloudy_Canvas.Helpers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Discord.Commands;

    public static class BadlistHelper
    {
        public static async Task<bool> RemoveYellowTerm(string term, SocketCommandContext context)
        {
            var yellowList = await GetYellowList(context);
            var lower = term.ToLower();
            if (!yellowList.Contains(lower))
            {
                return false;
            }

            yellowList.Remove(lower);
            await SaveYellowList(yellowList, context);
            return true;
        }

        public static async Task<List<string>> GetYellowList(SocketCommandContext context)
        {
            var filepath = FileHelper.SetUpFilepath(FilePathType.Server, "YellowList", "txt", context);
            return new List<string>(await File.ReadAllLinesAsync(filepath));
        }

        public static async Task ClearYellowList(SocketCommandContext context)
        {
            var filepath = FileHelper.SetUpFilepath(FilePathType.Server, "YellowList", "txt", context);
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }

            await File.WriteAllTextAsync(filepath, "");
        }

        public static async Task<string> CheckYellowList(string query, SocketCommandContext context)
        {
            var yellowList = await GetYellowList(context);
            var queryList = query.ToLower().Split(", ");
            var matchedTerms = "";
            foreach (var yellow in yellowList)
            {
                foreach (var term in queryList)
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

        public static async Task InitializeYellowList(SocketCommandContext context)
        {
            var filepath = FileHelper.SetUpFilepath(FilePathType.Server, "YellowList", "txt", context);
            if (!File.Exists(filepath))
            {
                await File.WriteAllTextAsync(filepath, "");
            }
        }

        public static async Task<bool> AddYellowTerm(string term, SocketCommandContext context)
        {
            var lower = term.ToLower();
            var yellowList = await GetYellowList(context);
            if (yellowList.Contains(lower))
            {
                return false;
            }

            yellowList.Add(lower);
            await SaveYellowList(yellowList, context);
            return true;
        }

        public static async Task<string> CheckRedList(string query, SocketCommandContext context)
        {
            var filepath = FileHelper.SetUpFilepath(FilePathType.Root, "RedList", "txt", context);
            var rawRedList = await File.ReadAllLinesAsync(filepath);
            var redList = new List<string>();
            foreach (var term in rawRedList)
            {
                if (term == "Redlisted Tags:")
                {
                    continue;
                }

                redList.Add(term.Split(", ", 2)[1]);
            }

            var queryList = query.ToLower().Split(", ");
            var matchedTerms = "";
            foreach (var red in redList)
            {
                foreach (var term in queryList)
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

        private static async Task SaveYellowList(List<string> yellowList, SocketCommandContext context)
        {
            var filepath = FileHelper.SetUpFilepath(FilePathType.Server, "YellowList", "txt", context);
            await File.WriteAllLinesAsync(filepath, yellowList);
        }
    }
}
