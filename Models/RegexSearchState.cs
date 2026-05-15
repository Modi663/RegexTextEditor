using System.Collections.Generic;
using System.Linq;

namespace RegexTextEditor.Models
{
    public sealed class RegexSearchState
    {
        public List<SearchResult> Matches { get; } = new();

        public int CurrentMatchIndex { get; set; } = -1;

        public int ReplacedCount { get; set; }

        public SearchResult? CurrentMatch
        {
            get
            {
                if (CurrentMatchIndex < 0 || CurrentMatchIndex >= Matches.Count)
                    return null;

                return Matches[CurrentMatchIndex];
            }
        }

        public void SetMatches(IEnumerable<SearchResult> matches)
        {
            Matches.Clear();
            Matches.AddRange(matches);

            if (Matches.Count == 0)
            {
                CurrentMatchIndex = -1;
                return;
            }

            if (CurrentMatchIndex < 0 || CurrentMatchIndex >= Matches.Count)
                CurrentMatchIndex = 0;
        }

        public void ClearMatches()
        {
            Matches.Clear();
            CurrentMatchIndex = -1;
        }

        public bool MoveNext()
        {
            if (Matches.Count == 0)
                return false;

            CurrentMatchIndex++;

            if (CurrentMatchIndex >= Matches.Count)
                CurrentMatchIndex = 0;

            return true;
        }

        public bool MovePrevious()
        {
            if (Matches.Count == 0)
                return false;

            CurrentMatchIndex--;

            if (CurrentMatchIndex < 0)
                CurrentMatchIndex = Matches.Count - 1;

            return true;
        }

        public void Reset()
        {
            Matches.Clear();
            CurrentMatchIndex = -1;
            ReplacedCount = 0;
        }
    }
}