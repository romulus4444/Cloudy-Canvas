namespace Cloudy_Canvas.Blacklist
{
    using System.Collections.Generic;
    using System.IO;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;
    using Microsoft.Extensions.Options;

    public class BlacklistService
    {
        private readonly DiscordSettings _settings;
        private List<string> BlackList;
        private string Filepath;

        public BlacklistService(IOptions<DiscordSettings> settings)
        {
            _settings = settings.Value;
            BlackList = new List<string>();
        }

        public bool AddTerm(string term)
        {
            var lower = term.ToLower();
            if (BlackList.Contains(lower))
            {
                return false;
            }

            BlackList.Add(lower);
            SaveList();
            return true;
        }

        public bool RemoveTerm(string term)
        {
            var lower = term.ToLower();
            if (!BlackList.Contains(lower))
            {
                return false;
            }

            BlackList.Remove(lower);
            SaveList();
            return true;
        }

        public List<string> GetList()
        {
            LoadList();
            return BlackList;
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
            foreach (var term in BlackList)
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
            var path = FileHelper.SetUpFilepath(FilePathType.Server, "Blacklist", "txt", context);
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
            BlackList = new List<string>(File.ReadAllLines(Filepath));
        }

        private void SaveList()
        {
            File.WriteAllLines(Filepath, BlackList);
        }
    }
}
