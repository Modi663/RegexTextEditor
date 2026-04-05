using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;

namespace RegexTextEditor.Services
{
    public class EditorDocumentService
    {
        public string GetText(RichEditBox editor)
        {
            editor.Document.GetText(TextGetOptions.None, out string text);

            if (text.EndsWith("\r"))
                text = text[..^1];

            return text;
        }

        public void SetText(RichEditBox editor, string text)
        {
            editor.Document.SetText(TextSetOptions.None, text);
        }

        public (int Lines, int Chars) GetStatistics(RichEditBox editor)
        {
            string text = GetText(editor);

            int charsCount = text.Length;
            int linesCount = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;

            return (linesCount, charsCount);
        }
    }
}