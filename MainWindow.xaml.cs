using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RegexTextEditor.Models;
using RegexTextEditor.Services;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

namespace RegexTextEditor
{
    public sealed partial class MainWindow : Window
    {
        private RegexService RegexService { get; } = new();
        private FileService FileService { get; } = new();
        private FilePickerService FilePickerService { get; } = new();
        private EditorDocumentService DocumentService { get; } = new();
        private WindowAppearanceService AppearanceService { get; } = new();

        private EditorFileState FileState { get; } = new();

        private List<SearchResult> Matches { get; set; } = new();
        private bool IsDirty { get; set; }
        private bool SuppressTextChanged { get; set; }
        private int CurrentMatchIndex { get; set; } = -1;
        private int ReplacedCount { get; set; }
        private bool WordWrapEnabled { get; set; }
        private bool AllowWindowClose { get; set; }
        private bool IsCloseConfirmationInProgress { get; set; }

        private static Windows.UI.Color AllMatchesColor => ColorHelper.FromArgb(90, 70, 110, 160);
        private static Windows.UI.Color CurrentMatchColor => ColorHelper.FromArgb(190, 215, 160, 55);

        public MainWindow()
        {
            InitializeComponent();

            AppearanceService.SetupMica(this);
            AppearanceService.SetupCustomTitleBar(
                this,
                TitleBarDragRegion,
                AppTitleBar,
                LeftInsetColumn,
                RightInsetColumn);

            AppWindow.Closing += AppWindow_Closing;

            LoadDemoText();
            UpdateDocumentTitle();
            UpdateEditorStatistics();
            UpdateCounters();
        }

        private enum UnsavedChangesDecision
        {
            Save,
            Discard,
            Cancel
        }

        private void LoadDemoText()
        {
            const string demoText =
@"User1
User2
Admin1
Admin2
Test1
test@test.com
01.02.2025
02.03.2026";

            SetEditorText(demoText);
            MarkDocumentSaved();
        }

        private string GetEditorText()
        {
            return DocumentService.GetText(EditorBox);
        }

        private void SetEditorText(string text)
        {
            SuppressTextChanged = true;

            try
            {
                DocumentService.SetText(EditorBox, text);
            }
            finally
            {
                SuppressTextChanged = false;
            }

            UpdateEditorStatistics();
        }

        private void UpdateDocumentTitle()
        {
            string dirtyMark = IsDirty ? "*" : string.Empty;
            string titleText = $"{dirtyMark}{FileState.DocumentName}";

            DocumentTitleTextBlock.Text = titleText;
            Title = $"{titleText} - Regex Text Editor";
        }

        private void UpdateEditorStatistics()
        {
            var (lines, chars) = DocumentService.GetStatistics(EditorBox);

            LinesCountTextBlock.Text = $"Строк: {lines}";
            CharsCountTextBlock.Text = $"Символов: {chars}";
        }

        private void UpdateCounters()
        {
            MatchesCountTextBlock.Text = $"Найдено: {Matches.Count}";

            if (Matches.Count == 0 || CurrentMatchIndex < 0)
                CurrentMatchTextBlock.Text = "Текущее: 0/0";
            else
                CurrentMatchTextBlock.Text = $"Текущее: {CurrentMatchIndex + 1}/{Matches.Count}";

            ReplacedCountTextBlock.Text = $"Заменено: {ReplacedCount}";
        }

        private void MarkDocumentDirty()
        {
            if (IsDirty)
                return;

            IsDirty = true;
            UpdateDocumentTitle();
        }

        private void MarkDocumentSaved()
        {
            IsDirty = false;
            UpdateDocumentTitle();
        }

        private async Task<UnsavedChangesDecision> AskUnsavedChangesAsync()
        {
            if (!IsDirty)
                return UnsavedChangesDecision.Discard;

            ContentDialog dialog = new()
            {
                Title = "Есть несохранённые изменения",
                Content = "Сохранить изменения перед продолжением?",
                PrimaryButtonText = "Сохранить",
                SecondaryButtonText = "Не сохранять",
                CloseButtonText = "Отмена",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = AppTitleBar.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();

            return result switch
            {
                ContentDialogResult.Primary => UnsavedChangesDecision.Save,
                ContentDialogResult.Secondary => UnsavedChangesDecision.Discard,
                _ => UnsavedChangesDecision.Cancel
            };
        }

        private async Task<bool> ConfirmCanContinueAsync()
        {
            UnsavedChangesDecision decision = await AskUnsavedChangesAsync();

            if (decision == UnsavedChangesDecision.Cancel)
                return false;

            if (decision == UnsavedChangesDecision.Save)
            {
                await SaveToCurrentFileAsync();
                return !IsDirty;
            }

            return true;
        }

        private void ClearAllHighlights()
        {
            var fullRange = EditorBox.Document.GetRange(0, int.MaxValue);
            fullRange.CharacterFormat.BackgroundColor = Colors.Transparent;
        }

        private void HighlightMatches()
        {
            ClearAllHighlights();

            if (Matches.Count == 0)
                return;

            foreach (SearchResult match in Matches)
            {
                var range = EditorBox.Document.GetRange(match.Start, match.Start + match.Length);
                range.CharacterFormat.BackgroundColor = AllMatchesColor;
            }

            if (CurrentMatchIndex >= 0 && CurrentMatchIndex < Matches.Count)
            {
                SearchResult current = Matches[CurrentMatchIndex];
                var currentRange = EditorBox.Document.GetRange(current.Start, current.Start + current.Length);

                currentRange.CharacterFormat.BackgroundColor = CurrentMatchColor;
                EditorBox.Document.Selection.SetRange(current.Start, current.Start + current.Length);
            }
        }

        private void ResetRegexState(bool closePanel = true)
        {
            Matches.Clear();
            CurrentMatchIndex = -1;
            ReplacedCount = 0;

            PatternTextBox.Text = string.Empty;
            ReplacementTextBox.Text = string.Empty;

            ClearAllHighlights();

            if (closePanel)
                RegexPanel.Visibility = Visibility.Collapsed;

            UpdateCounters();
        }

        private void OpenRegexPanel()
        {
            RegexPanel.Visibility = Visibility.Visible;
            StatusTextBlock.Text = "Панель regex открыта";
        }

        private void CloseRegexPanel()
        {
            Matches.Clear();
            CurrentMatchIndex = -1;
            ReplacedCount = 0;

            ClearAllHighlights();
            UpdateCounters();
            RegexPanel.Visibility = Visibility.Collapsed;
            StatusTextBlock.Text = "Панель regex закрыта";
        }

        private async Task OpenFileAsync()
        {
            try
            {
                StorageFile? file = await FilePickerService.PickOpenFileAsync(this);

                if (file is null)
                {
                    StatusTextBlock.Text = "Открытие файла отменено";
                    return;
                }

                string text = await FileService.ReadTextAsync(file);

                SetEditorText(text);

                FileState.File = file;
                FileState.FilePath = file.Path;
                FileState.DocumentName = file.Name;

                UpdateDocumentTitle();
                ResetRegexState();
                MarkDocumentSaved();

                StatusTextBlock.Text = $"Открыт файл: {file.Name}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка открытия файла: {ex.Message}";
            }
        }

        private async Task SaveToCurrentFileAsync()
        {
            try
            {
                if (FileState.File is null)
                {
                    await SaveAsFileAsync();
                    return;
                }

                string text = GetEditorText();
                await FileService.WriteTextAsync(FileState.File, text);

                FileState.FilePath = FileState.File.Path;
                FileState.DocumentName = FileState.File.Name;

                UpdateDocumentTitle();
                MarkDocumentSaved();

                StatusTextBlock.Text = $"Файл сохранён: {FileState.File.Name}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка сохранения файла: {ex.Message}";
            }
        }

        private async Task SaveAsFileAsync()
        {
            try
            {
                StorageFile? file = await FilePickerService.PickSaveFileAsync(this, FileState.DocumentName);

                if (file is null)
                {
                    StatusTextBlock.Text = "Сохранение отменено";
                    return;
                }

                string text = GetEditorText();
                await FileService.WriteTextAsync(file, text);

                FileState.File = file;
                FileState.FilePath = file.Path;
                FileState.DocumentName = file.Name;

                UpdateDocumentTitle();
                MarkDocumentSaved();

                StatusTextBlock.Text = $"Файл сохранён: {file.Name}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка сохранения файла: {ex.Message}";
            }
        }

        private async Task ShowAboutDialogAsync()
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
                XamlRoot = AppTitleBar.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private async Task ShowShortcutsDialogAsync()
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
                XamlRoot = AppTitleBar.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private async void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            if (AllowWindowClose || IsCloseConfirmationInProgress)
                return;

            args.Cancel = true;
            IsCloseConfirmationInProgress = true;

            try
            {
                bool canContinue = await ConfirmCanContinueAsync();

                if (!canContinue)
                    return;

                AllowWindowClose = true;
                Close();
            }
            finally
            {
                IsCloseConfirmationInProgress = false;
            }
        }

        private void RegexPanelButton_Click(object sender, RoutedEventArgs e)
        {
            OpenRegexPanel();
        }

        private void CloseRegexPanelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRegexPanel();
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = GetEditorText();
                string pattern = PatternTextBox.Text;

                Matches = RegexService.FindMatches(text, pattern);
                CurrentMatchIndex = Matches.Count > 0 ? 0 : -1;
                ReplacedCount = 0;

                UpdateCounters();
                HighlightMatches();
                UpdateEditorStatistics();

                StatusTextBlock.Text = Matches.Count > 0
                    ? "Поиск выполнен"
                    : "Совпадения не найдены";
            }
            catch (Exception ex)
            {
                Matches.Clear();
                CurrentMatchIndex = -1;
                UpdateCounters();
                ClearAllHighlights();
                StatusTextBlock.Text = $"Ошибка regex: {ex.Message}";
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (Matches.Count == 0)
                return;

            CurrentMatchIndex++;

            if (CurrentMatchIndex >= Matches.Count)
                CurrentMatchIndex = 0;

            HighlightMatches();
            UpdateCounters();
            StatusTextBlock.Text = "Переход к следующему совпадению";
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (Matches.Count == 0)
                return;

            CurrentMatchIndex--;

            if (CurrentMatchIndex < 0)
                CurrentMatchIndex = Matches.Count - 1;

            HighlightMatches();
            UpdateCounters();
            StatusTextBlock.Text = "Переход к предыдущему совпадению";
        }

        private void ReplaceCurrentButton_Click(object sender, RoutedEventArgs e)
        {
            if (Matches.Count == 0 || CurrentMatchIndex < 0)
                return;

            try
            {
                string text = GetEditorText();
                string pattern = PatternTextBox.Text;
                string replacement = ReplacementTextBox.Text;

                string newText = RegexService.ReplaceOnlyCurrent(text, pattern, replacement, CurrentMatchIndex);
                SetEditorText(newText);

                ReplacedCount++;
                MarkDocumentDirty();

                Matches = RegexService.FindMatches(newText, pattern);

                if (Matches.Count == 0)
                {
                    CurrentMatchIndex = -1;
                }
                else if (CurrentMatchIndex >= Matches.Count)
                {
                    CurrentMatchIndex = 0;
                }

                UpdateCounters();
                HighlightMatches();
                StatusTextBlock.Text = "Текущее совпадение заменено";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка замены: {ex.Message}";
            }
        }

        private void ReplaceAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = GetEditorText();
                string pattern = PatternTextBox.Text;
                string replacement = ReplacementTextBox.Text;

                MatchCollection matchesBeforeReplace = Regex.Matches(text, pattern);
                ReplacedCount = matchesBeforeReplace.Count;

                string newText = RegexService.ReplaceAll(text, pattern, replacement);
                SetEditorText(newText);

                if (ReplacedCount > 0)
                    MarkDocumentDirty();

                Matches = RegexService.FindMatches(newText, pattern);
                CurrentMatchIndex = Matches.Count > 0 ? 0 : -1;

                UpdateCounters();
                HighlightMatches();
                StatusTextBlock.Text = "Замена всех совпадений выполнена";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка замены: {ex.Message}";
            }
        }

        private async Task CreateNewDocumentAsync()
        {
            bool canContinue = await ConfirmCanContinueAsync();
            if (!canContinue)
                return;

            SetEditorText(string.Empty);

            FileState.Reset();
            UpdateDocumentTitle();

            ResetRegexState();
            MarkDocumentSaved();

            StatusTextBlock.Text = "Создан новый документ";
        }

        private async Task OpenDocumentCommandAsync()
        {
            bool canContinue = await ConfirmCanContinueAsync();
            if (!canContinue)
                return;

            await OpenFileAsync();
        }

        private async Task ExitApplicationAsync()
        {
            bool canContinue = await ConfirmCanContinueAsync();
            if (!canContinue)
                return;

            AllowWindowClose = true;
            Close();
        }

        private async void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            await CreateNewDocumentAsync();
        }

        private async void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            await OpenDocumentCommandAsync();
        }

        private async void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            await SaveToCurrentFileAsync();
        }

        private async void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            await SaveAsFileAsync();
        }

        private async void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            await ExitApplicationAsync();
        }

        private void MenuFind_Click(object sender, RoutedEventArgs e)
        {
            OpenRegexPanel();
        }

        private void MenuNextMatch_Click(object sender, RoutedEventArgs e)
        {
            NextButton_Click(sender, e);
        }

        private void MenuPrevMatch_Click(object sender, RoutedEventArgs e)
        {
            PrevButton_Click(sender, e);
        }

        private void MenuReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            ReplaceAllButton_Click(sender, e);
        }

        private void MenuClearDocument_Click(object sender, RoutedEventArgs e)
        {
            SetEditorText(string.Empty);
            ResetRegexState(closePanel: false);
            MarkDocumentDirty();
            StatusTextBlock.Text = "Документ очищен";
        }

        private async void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            await ShowAboutDialogAsync();
        }

        private async void MenuShortcuts_Click(object sender, RoutedEventArgs e)
        {
            await ShowShortcutsDialogAsync();
        }

        private void ToggleStatusBarMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ToggleStatusBarMenuItem.IsChecked)
            {
                StatusBarBorder.Visibility = Visibility.Visible;
                StatusTextBlock.Text = "Строка состояния показана";
            }
            else
            {
                StatusBarBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void ToggleWordWrapMenuItem_Click(object sender, RoutedEventArgs e)
        {
            WordWrapEnabled = ToggleWordWrapMenuItem.IsChecked;

            EditorBox.TextWrapping = WordWrapEnabled
                ? TextWrapping.Wrap
                : TextWrapping.NoWrap;

            StatusTextBlock.Text = WordWrapEnabled
                ? "Перенос строк включён"
                : "Перенос строк выключен";
        }

        private void EditorBox_TextChanged(object sender, RoutedEventArgs e)
        {
            UpdateEditorStatistics();

            if (SuppressTextChanged)
                return;

            MarkDocumentDirty();
        }

        private async void NewKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            await CreateNewDocumentAsync();
        }

        private async void OpenKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            await OpenDocumentCommandAsync();
        }

        private async void SaveKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            await SaveToCurrentFileAsync();
        }

        private async void SaveAsKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            await SaveAsFileAsync();
        }

        private void FindKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            OpenRegexPanel();
        }

        private void NextKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            NextButton_Click(sender, new RoutedEventArgs());
        }

        private void PrevKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            PrevButton_Click(sender, new RoutedEventArgs());
        }

        private async void HelpKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            await ShowShortcutsDialogAsync();
        }
    }
}