namespace RegexTextEditor.Models
{
    public class SearchResult
    {
        public int Start { get; set; }
        public int Length { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}