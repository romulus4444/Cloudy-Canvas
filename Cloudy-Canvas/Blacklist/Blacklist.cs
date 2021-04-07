namespace Cloudy_Canvas.Blacklist
{
    using System.Collections.Generic;
    using System.IO;

    public class Blacklist
    {
        private List<string> BlackList;

        public Blacklist()
        {
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
            LoadList();
        }

        private void LoadList()
        {
            BlackList = new List<string>(File.ReadAllLines(@"blacklist.txt"));
        }

        private void SaveList()
        {
            File.WriteAllLines("Blacklist.txt", BlackList);
        }
    }
}
