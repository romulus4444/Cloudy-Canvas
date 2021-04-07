namespace Cloudy_Canvas.Blacklist
{
    using System.Collections.Generic;
    using System.IO;
    using Cloudy_Canvas.Settings;
    using Microsoft.Extensions.Options;

    public class Blacklist
    {
        private readonly DiscordSettings _settings;
        private List<string> BlackList;

        public Blacklist(IOptions<DiscordSettings> settings)
        {
            _settings = settings.Value;
            BlackList = new List<string>();
            InitializeList();
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
            if (File.Exists(_settings.blacklistpath))
            {
                File.Delete(_settings.blacklistpath);
            }

            File.WriteAllText(_settings.blacklistpath, "");
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

        private void InitializeList()
        {
            if (File.Exists(_settings.blacklistpath))
            {
                LoadList();
            }
            else
            {
                File.WriteAllText(_settings.blacklistpath, "");
            }
        }

        private void LoadList()
        {
            BlackList = new List<string>(File.ReadAllLines(_settings.blacklistpath));
        }

        private void SaveList()
        {
            File.WriteAllLines(_settings.blacklistpath, BlackList);
        }
    }
}
