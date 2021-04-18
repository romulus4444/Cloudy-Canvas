namespace Cloudy_Canvas.Service
{
    using System.Collections.Generic;
    using System.IO;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;
    using Microsoft.Extensions.Options;

    public class BadlistService
    {
        private readonly DiscordSettings _settings;
        private List<string> YellowList;
        private string Filepath;

        public BadlistService(IOptions<DiscordSettings> settings)
        {
            _settings = settings.Value;
            YellowList = new List<string>();
        }

        public bool AddTerm(string term)
        {
            var lower = term.ToLower();
            if (YellowList.Contains(lower))
            {
                return false;
            }

            YellowList.Add(lower);
            SaveList();
            return true;
        }

        public bool RemoveTerm(string term)
        {
            var lower = term.ToLower();
            if (!YellowList.Contains(lower))
            {
                return false;
            }

            YellowList.Remove(lower);
            SaveList();
            return true;
        }

        public List<string> GetList()
        {
            LoadList();
            return YellowList;
        }

        public void ClearList()
        {
            if (File.Exists(Filepath))
            {
                File.Delete(Filepath);
            }

            File.WriteAllText(Filepath, "");
        }

        public string CheckList(string query)
        {
            LoadList();
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

        public void InitializeList(SocketCommandContext context)
        {
            var path = FileHelper.SetUpFilepath(FilePathType.Server, "YellowList", "txt", context);
            Filepath = path;
            if (File.Exists(Filepath))
            {
                LoadList();
            }
            else
            {
                File.WriteAllText(path, "");
            }
        }

        private void LoadList()
        {
            YellowList = new List<string>(File.ReadAllLines(Filepath));
        }

        private void SaveList()
        {
            File.WriteAllLines(Filepath, YellowList);
        }
    }
}
