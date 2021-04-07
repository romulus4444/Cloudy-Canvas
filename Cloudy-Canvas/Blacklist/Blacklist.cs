namespace Cloudy_Canvas.Blacklist
{
    using System.Collections.Generic;

    public class Blacklist
    {
        private List<string> BlackList;

        public Blacklist()
        {
            BlackList = new List<string>();
            InitializeList();
        }

        private void InitializeList()
        {
            BlackList.Add("aryanne");
        }

        public bool AddTerm(string term)
        {
            var lower = term.ToLower();
            if (BlackList.Contains(lower))
            {
                return false;
            }
            BlackList.Add(lower);
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
            return true;

        }

        public List<string> GetList()
        {
            return BlackList;
        }
        public string CheckList(string query)
        {
            var lower = query.ToLower();
            var matchedTerms = "";
            foreach (var term in BlackList)
            {
                if (!lower.Contains(term))
                {
                    return matchedTerms;
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
    }
}
