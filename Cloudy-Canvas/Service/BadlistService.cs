namespace Cloudy_Canvas.Service
{
    using System.Collections.Generic;
    using System.IO;
    using Cloudy_Canvas.Helpers;
    using Discord.Commands;

    public class BadlistService
    {
        private List<string> YellowList;
        private List<string> RedList;
        private string YellowFilepath;
        private string RedFilepath;

        public BadlistService()
        {
            YellowList = new List<string>();
            RedList = new List<string>();
        }

        public bool AddYellowTerm(string term)
        {
            var lower = term.ToLower();
            if (YellowList.Contains(lower))
            {
                return false;
            }

            YellowList.Add(lower);
            SaveYellowList();
            return true;
        }

        public bool RemoveYellowTerm(string term)
        {
            var lower = term.ToLower();
            if (!YellowList.Contains(lower))
            {
                return false;
            }

            YellowList.Remove(lower);
            SaveYellowList();
            return true;
        }

        public List<string> GetYellowList()
        {
            LoadYellowList();
            return YellowList;
        }

        public List<string> GetRedList(SocketCommandContext context)
        {
            LoadRedList(context);
            return YellowList;
        }

        public void ClearYellowList()
        {
            if (File.Exists(YellowFilepath))
            {
                File.Delete(YellowFilepath);
            }

            File.WriteAllText(YellowFilepath, "");
        }

        public string CheckYellowList(string query)
        {
            LoadYellowList();
            var lower = query.ToLower();
            var matchedTerms = "";
            foreach (var term in YellowList)
            {
                if (!lower.Contains(term))
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

            return matchedTerms;
        }

        public string CheckRedList(string query, SocketCommandContext context)
        {
            LoadRedList(context);
            var lower = query.ToLower();
            var matchedTerms = "";
            foreach (var term in RedList)
            {
                var split = "";
                if (term == "Redlisted Tags:")
                {
                    continue;
                }

                split = term.Split(", ", 2)[1];
                if (!lower.Contains(split))
                {
                    continue;
                }

                if (matchedTerms == "")
                {
                    matchedTerms += split;
                }
                else
                {
                    matchedTerms += $", {split}";
                }
            }

            return matchedTerms;
        }

        public void InitializeYellowList(SocketCommandContext context)
        {
            var path = FileHelper.SetUpFilepath(FilePathType.Server, "YellowList", "txt", context);
            YellowFilepath = path;
            if (File.Exists(YellowFilepath))
            {
                LoadYellowList();
            }
            else
            {
                File.WriteAllText(path, "");
            }
        }

        private void LoadYellowList()
        {
            YellowList = new List<string>(File.ReadAllLines(YellowFilepath));
        }

        private void LoadRedList(SocketCommandContext context)
        {
            RedFilepath = FileHelper.SetUpFilepath(FilePathType.Root, "RedList", "txt", context);
            RedList = new List<string>(File.ReadAllLines(RedFilepath));
        }

        private void SaveYellowList()
        {
            File.WriteAllLines(YellowFilepath, YellowList);
        }
    }
}
