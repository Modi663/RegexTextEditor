namespace RegexTextEditor.Models
{
    public sealed class EditorStatistics
    {
        public EditorStatistics(int lines, int characters)
        {
            Lines = lines;
            Characters = characters;
        }

        public int Lines { get; }
        public int Characters { get; }
    }
}