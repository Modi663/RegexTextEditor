using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace RegexTextEditor.Services
{
    public enum UnsavedChangesDecision
    {
        Save,
        Discard,
        Cancel
    }

    public sealed class DialogService
    {
        public async Task<UnsavedChangesDecision> AskUnsavedChangesAsync(XamlRoot xamlRoot)
        {
            ContentDialog dialog = new()
            {
                Title = "Есть несохранённые изменения",
                Content = "Сохранить изменения перед продолжением?",
                PrimaryButtonText = "Сохранить",
                SecondaryButtonText = "Не сохранять",
                CloseButtonText = "Отмена",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();

            return result switch
            {
                ContentDialogResult.Primary => UnsavedChangesDecision.Save,
                ContentDialogResult.Secondary => UnsavedChangesDecision.Discard,
                _ => UnsavedChangesDecision.Cancel
            };
        }

        public async Task ShowAboutDialogAsync(XamlRoot xamlRoot)
        {
            ContentDialog dialog = new()
            {
                Title = "О программе",
                Content =
@"Regex Text Editor
Версия: Beta 0.1

Текстовый редактор с поддержкой поиска и замены по регулярным выражениям.
Проект на WinUI 3 / C#.",
                CloseButtonText = "Закрыть",
                XamlRoot = xamlRoot
            };

            await dialog.ShowAsync();
        }

        public async Task ShowShortcutsDialogAsync(XamlRoot xamlRoot)
        {
            ContentDialog dialog = new()
            {
                Title = "Горячие клавиши",
                Content =
@"Ctrl+N  — новый документ
Ctrl+O  — открыть
Ctrl+S  — сохранить
Ctrl+Shift+S — сохранить как
Ctrl+F  — открыть regex-панель
Ctrl+H  — открыть regex-панель
F3      — следующее совпадение
Shift+F3 — предыдущее совпадение
F1      — справка",
                CloseButtonText = "Закрыть",
                XamlRoot = xamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}