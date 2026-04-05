using Windows.Storage;

namespace RegexTextEditor.Models
{
    public class EditorFileState
    {
        public string DocumentName { get; set; } = "Без имени";
        public string? FilePath { get; set; }
        public StorageFile? File { get; set; }

        public void Reset()
        {
            DocumentName = "Без имени";
            FilePath = null;
            File = null;
        }
    }
}