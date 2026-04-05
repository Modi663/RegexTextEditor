using RegexTextEditor.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RegexTextEditor.Services
{
    public class RegexService
    {
        public List<SearchResult> FindMatches(string text, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return new List<SearchResult>();

            MatchCollection matches = Regex.Matches(text, pattern);

            List<SearchResult> results = new();

            foreach (Match match in matches)
            {
                results.Add(new SearchResult
                {
                    Start = match.Index,
                    Length = match.Length,
                    Value = match.Value
                });
            }

            return results;
        }

        public string ReplaceAll(string text, string pattern, string replacement)
        {
            if (string.IsNullOrEmpty(pattern))
                return text;

            return Regex.Replace(text, pattern, replacement);
        }

        public string ReplaceOnlyCurrent(string text, string pattern, string replacement, int targetIndex)
        {
            MatchCollection matches = Regex.Matches(text, pattern);

            if (targetIndex < 0 || targetIndex >= matches.Count)
                return text;

            Match match = matches[targetIndex];

            string before = text[..match.Index];
            string after = text[(match.Index + match.Length)..];
            string replacedCurrent = match.Result(replacement);

            return before + replacedCurrent + after;
        }
    }
}