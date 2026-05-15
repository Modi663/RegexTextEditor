using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using RegexTextEditor.Models;
using System.Linq;

namespace RegexTextEditor.Services
{
    public class EditorDocumentService
    {
        public string GetText(RichEditBox editor)
        {
            editor.Document.GetText(TextGetOptions.UseCrlf, out string text);

            return NormalizeRichEditBoxText(text);
        }

        public void SetText(RichEditBox editor, string text)
        {
            editor.Document.SetText(TextSetOptions.None, text);
        }

        public EditorStatistics GetStatistics(RichEditBox editor)
        {
            string text = GetText(editor);

            int linesCount = CountLines(text);
            int charactersCount = CountVisibleCharacters(text);

            return new EditorStatistics(linesCount, charactersCount);
        }

        private static string NormalizeRichEditBoxText(string text)
        {
            if (text.EndsWith("\r"))
                text = text[..^1];

            return text;
        }

        private static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 1;

            string normalizedText = NormalizeLineEndings(text);

            return normalizedText.Count(character => character == '\n') + 1;
        }

        private static int CountVisibleCharacters(string text)
        {
            return text.Count(character => character != '\r' && character != '\n');
        }

        private static string NormalizeLineEndings(string text)
        {
            return text
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');
        }
    }
}