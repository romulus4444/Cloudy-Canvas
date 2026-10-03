namespace Cloudy_Canvas.Helpers
{
    public static class QueryHelper
    {
        /// <summary>
        /// Builds the query sent to the booru. In safe mode the user's whole query is wrapped in parentheses before <c>, safe</c> is appended,
        /// so an <c>||</c> in the query cannot let results escape the <c>safe</c> requirement.
        /// </summary>
        public static string ApplySafeMode(string query, bool safeMode)
        {
            if (!safeMode)
            {
                return query;
            }

            // Unbalanced parentheses could close our wrapping group early (e.g. "x) || (y"), so drop them entirely in that case.
            if (!HasBalancedParentheses(query))
            {
                query = query.Replace("(", string.Empty).Replace(")", string.Empty);
            }

            return $"({query}), safe";
        }

        /// <summary>
        /// True if every parenthesis outside of quoted text is matched and no quote is left open.
        /// A backslash escapes the next character.
        /// </summary>
        public static bool HasBalancedParentheses(string query)
        {
            var depth = 0;
            var inQuotes = false;
            for (var i = 0; i < query.Length; i++)
            {
                var c = query[i];
                if (c == '\\')
                {
                    i++;
                    continue;
                }

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (inQuotes)
                {
                    continue;
                }

                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                    if (depth < 0)
                    {
                        return false;
                    }
                }
            }

            return depth == 0 && !inQuotes;
        }
    }
}
