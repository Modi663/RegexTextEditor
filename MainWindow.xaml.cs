using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using RegexTextEditor.Models;
using RegexTextEditor.Services;
using RegexTextEditor.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Storage;

namespace RegexTextEditor
{
    public sealed partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; } = new();

        private readonly FileService FileService = new();
        private readonly FilePickerService FilePickerService = new();
        private readonly EditorDocumentService DocumentService = new();
        private readonly WindowAppearanceService AppearanceService = new();
        private readonly DialogService DialogService = new();

        private bool SuppressTextChanged;
        private bool AllowWindowClose;
        private bool IsCloseConfirmationInProgress;
        private Storyboard? _regexPanelStoryboard;
        private bool? _lastRegexPanelTargetVisible;

        private static Windows.UI.Color AllMatchesColor => ColorHelper.FromArgb(90, 70, 110, 160);
        private static Windows.UI.Color CurrentMatchColor => ColorHelper.FromArgb(190, 215, 160, 55);

        public MainWindow()
        {
            ViewModel.ConfigureCommands(
                CreateNewDocumentAsync,
                OpenDocumentCommandAsync,
                SaveToCurrentFileAsync,
                SaveAsFileAsync,
                ExitApplicationAsync,
                ExecuteToggleRegexPanel,
                ExecuteCloseRegexPanel,
                ExecuteFind,
                ExecuteNextMatch,
                ExecutePreviousMatch,
                ExecuteReplaceCurrent,
                ExecuteReplaceAll,
                ExecuteClearDocument,
                ShowAboutDialogAsync,
                ShowShortcutsDialogAsync);

            InitializeComponent();

            RootGrid.DataContext = ViewModel;
            RootGrid.ActualThemeChanged += RootGrid_ActualThemeChanged;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            AppearanceService.SetupMica(this);
            AppearanceService.SetupCustomTitleBar(
                this,
                TitleBarDragRegion,
                AppTitleBar,
                LeftInsetColumn,
                RightInsetColumn);

            WindowMinSizeHook.Attach(this, 900, 580);

            AppWindow.Closing += AppWindow_Closing;

            LoadDemoText();
            ApplyViewModelVisualState();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.WindowTitle) ||
                e.PropertyName == nameof(MainWindowViewModel.IsRegexPanelVisible) ||
                e.PropertyName == nameof(MainWindowViewModel.IsStatusBarVisible) ||
                e.PropertyName == nameof(MainWindowViewModel.WordWrapEnabled) ||
                e.PropertyName == nameof(MainWindowViewModel.ThemeMode) ||
                e.PropertyName == nameof(MainWindowViewModel.HasRegexError))
            {
                ApplyViewModelVisualState();
            }
        }

        private void ApplyViewModelVisualState()
        {
            ApplyTheme();

            Title = ViewModel.WindowTitle;

            ApplyRegexPanelVisualState();

            StatusBarBorder.Visibility = ViewModel.IsStatusBarVisible
                ? Visibility.Visible
                : Visibility.Collapsed;

            EditorBox.TextWrapping = ViewModel.WordWrapEnabled
                ? TextWrapping.Wrap
                : TextWrapping.NoWrap;

            ToggleStatusBarMenuItem.IsChecked = ViewModel.IsStatusBarVisible;
            ToggleWordWrapMenuItem.IsChecked = ViewModel.WordWrapEnabled;

            ApplyRegexErrorVisualState();
        }

        private void ApplyRegexPanelVisualState()
        {
            bool targetVisible = ViewModel.IsRegexPanelVisible;

            if (_lastRegexPanelTargetVisible is null)
            {
                _lastRegexPanelTargetVisible = targetVisible;

                RegexPanel.Visibility = targetVisible
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                RegexPanel.Opacity = targetVisible ? 1 : 0;
                RegexPanelTranslateTransform.X = targetVisible ? 0 : 24;

                return;
            }

            if (_lastRegexPanelTargetVisible == targetVisible)
                return;

            _lastRegexPanelTargetVisible = targetVisible;

            if (targetVisible)
                ShowRegexPanelAnimated();
            else
                HideRegexPanelAnimated();
        }

        private void ShowRegexPanelAnimated()
        {
            _regexPanelStoryboard?.Stop();

            RegexPanel.Visibility = Visibility.Visible;
            RegexPanel.Opacity = 0;
            RegexPanelTranslateTransform.X = 24;

            _regexPanelStoryboard = CreateRegexPanelStoryboard(
                targetOpacity: 1,
                targetX: 0,
                durationMilliseconds: 180);

            _regexPanelStoryboard.Begin();
        }

        private void HideRegexPanelAnimated()
        {
            _regexPanelStoryboard?.Stop();

            if (RegexPanel.Visibility != Visibility.Visible)
            {
                RegexPanel.Opacity = 0;
                RegexPanelTranslateTransform.X = 24;
                RegexPanel.Visibility = Visibility.Collapsed;
                return;
            }

            _regexPanelStoryboard = CreateRegexPanelStoryboard(
                targetOpacity: 0,
                targetX: 24,
                durationMilliseconds: 140);

            _regexPanelStoryboard.Completed += (_, _) =>
            {
                if (!ViewModel.IsRegexPanelVisible)
                    RegexPanel.Visibility = Visibility.Collapsed;
            };

            _regexPanelStoryboard.Begin();
        }

        private Storyboard CreateRegexPanelStoryboard(
            double targetOpacity,
            double targetX,
            int durationMilliseconds)
        {
            Storyboard storyboard = new();

            Duration duration = new(TimeSpan.FromMilliseconds(durationMilliseconds));

            CubicEase easing = new()
            {
                EasingMode = EasingMode.EaseOut
            };

            DoubleAnimation opacityAnimation = new()
            {
                To = targetOpacity,
                Duration = duration,
                EasingFunction = easing
            };

            Storyboard.SetTarget(opacityAnimation, RegexPanel);
            Storyboard.SetTargetProperty(opacityAnimation, "Opacity");

            DoubleAnimation translateAnimation = new()
            {
                To = targetX,
                Duration = duration,
                EasingFunction = easing
            };

            Storyboard.SetTarget(translateAnimation, RegexPanelTranslateTransform);
            Storyboard.SetTargetProperty(translateAnimation, "X");

            storyboard.Children.Add(opacityAnimation);
            storyboard.Children.Add(translateAnimation);

            return storyboard;
        }

        private void ApplyRegexErrorVisualState()
        {
            RegexErrorTextBlock.Visibility = ViewModel.HasRegexError
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (ViewModel.HasRegexError)
            {
                PatternTextBox.BorderBrush = GetBrushResource("AppRegexErrorBorderBrush");
                PatternTextBox.BorderThickness = new Thickness(1.5);
                return;
            }

            PatternTextBox.ClearValue(Control.BorderBrushProperty);
            PatternTextBox.ClearValue(Control.BorderThicknessProperty);
        }

        private Brush GetBrushResource(string resourceKey)
        {
            if (Application.Current.Resources.TryGetValue(resourceKey, out object value) &&
                value is Brush brush)
            {
                return brush;
            }

            return PatternTextBox.BorderBrush;
        }

        private void ApplyTheme()
        {
            ElementTheme requestedTheme = ViewModel.ThemeMode switch
            {
                AppThemeMode.Light => ElementTheme.Light,
                AppThemeMode.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            if (RootGrid.RequestedTheme != requestedTheme)
                RootGrid.RequestedTheme = requestedTheme;

            AppearanceService.ApplyTitleBarButtonTheme(RootGrid.ActualTheme);
        }

        private void RootGrid_ActualThemeChanged(FrameworkElement sender, object args)
        {
            AppearanceService.ApplyTitleBarButtonTheme(sender.ActualTheme);
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
            ViewModel.MarkDocumentSaved();
            ViewModel.SetStatus("Готово");
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

        private void UpdateEditorStatistics()
        {
            EditorStatistics statistics = DocumentService.GetStatistics(EditorBox);
            ViewModel.UpdateStatistics(statistics);
        }

        private async Task<bool> ConfirmCanContinueAsync()
        {
            if (!ViewModel.IsDirty)
                return true;

            UnsavedChangesDecision decision = await DialogService.AskUnsavedChangesAsync(AppTitleBar.XamlRoot);

            if (decision == UnsavedChangesDecision.Cancel)
                return false;

            if (decision == UnsavedChangesDecision.Save)
            {
                await SaveToCurrentFileAsync();
                return !ViewModel.IsDirty;
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

            if (ViewModel.MatchesCount == 0)
                return;

            string editorText = GetEditorText();

            foreach (SearchResult match in ViewModel.Matches)
            {
                var (start, end) = ConvertTextMatchToRichEditRange(editorText, match);

                if (start == end)
                    continue;

                var range = EditorBox.Document.GetRange(start, end);
                range.CharacterFormat.BackgroundColor = AllMatchesColor;
            }

            if (ViewModel.CurrentMatchIndex >= 0 && ViewModel.CurrentMatchIndex < ViewModel.Matches.Count)
            {
                SearchResult current = ViewModel.Matches[ViewModel.CurrentMatchIndex];
                var (start, end) = ConvertTextMatchToRichEditRange(editorText, current);

                if (start == end)
                    return;

                var currentRange = EditorBox.Document.GetRange(start, end);

                currentRange.CharacterFormat.BackgroundColor = CurrentMatchColor;
                EditorBox.Document.Selection.SetRange(start, end);
            }
        }

        private static (int Start, int End) ConvertTextMatchToRichEditRange(
    string text,
    SearchResult match)
        {
            int textStart = Math.Clamp(match.Start, 0, text.Length);
            int textEnd = Math.Clamp(match.Start + match.Length, textStart, text.Length);

            int richEditStart = ConvertTextIndexToRichEditIndex(text, textStart);
            int richEditEnd = ConvertTextIndexToRichEditIndex(text, textEnd);

            return (richEditStart, richEditEnd);
        }

        private static int ConvertTextIndexToRichEditIndex(string text, int textIndex)
        {
            int richEditIndex = textIndex;

            for (int i = 0; i < textIndex; i++)
            {
                if (text[i] == '\r')
                    richEditIndex--;
            }

            return richEditIndex;
        }

        private void ResetRegexState(bool closePanel = true)
        {
            ViewModel.ResetRegexState(clearInputs: true);
            ClearAllHighlights();

            if (closePanel)
                ViewModel.CloseRegexPanel();
        }

        private void ExecuteToggleRegexPanel()
        {
            if (ViewModel.IsRegexPanelVisible)
            {
                ExecuteCloseRegexPanel();
                return;
            }

            ViewModel.OpenRegexPanel();
            PatternTextBox.Focus(FocusState.Programmatic);
        }

        private void ExecuteCloseRegexPanel()
        {
            ViewModel.CloseRegexPanel();
            ClearAllHighlights();
        }

        private void ExecuteFind()
        {
            try
            {
                string text = GetEditorText();

                ViewModel.FindMatches(text);

                HighlightMatches();
                UpdateEditorStatistics();
            }
            catch (Exception ex)
            {
                ViewModel.ClearRegexResults(resetReplacedCount: false);
                ClearAllHighlights();
                ViewModel.SetStatus($"Ошибка regex: {ex.Message}");
            }
        }

        private void ExecuteNextMatch()
        {
            if (!ViewModel.MoveNextMatch())
                return;

            HighlightMatches();
        }

        private void ExecutePreviousMatch()
        {
            if (!ViewModel.MovePreviousMatch())
                return;

            HighlightMatches();
        }

        private void ExecuteReplaceCurrent()
        {
            try
            {
                string text = GetEditorText();

                if (!ViewModel.TryReplaceCurrent(text, out string newText))
                    return;

                SetEditorText(newText);
                ViewModel.MarkDocumentDirty();

                HighlightMatches();
            }
            catch (Exception ex)
            {
                ViewModel.SetStatus($"Ошибка замены: {ex.Message}");
            }
        }

        private void ExecuteReplaceAll()
        {
            try
            {
                string text = GetEditorText();

                bool replaced = ViewModel.TryReplaceAll(text, out string newText);

                SetEditorText(newText);

                if (replaced)
                    ViewModel.MarkDocumentDirty();

                HighlightMatches();
            }
            catch (Exception ex)
            {
                ViewModel.SetStatus($"Ошибка замены: {ex.Message}");
            }
        }

        private void ExecuteClearDocument()
        {
            SetEditorText(string.Empty);
            ResetRegexState(closePanel: false);
            ViewModel.MarkDocumentDirty();
            ViewModel.SetStatus("Документ очищен");
        }

        private async Task OpenFileAsync()
        {
            try
            {
                StorageFile? file = await FilePickerService.PickOpenFileAsync(this);

                if (file is null)
                {
                    ViewModel.SetStatus("Открытие файла отменено");
                    return;
                }

                string text = await FileService.ReadTextAsync(file);

                SetEditorText(text);

                ViewModel.SetFile(file);
                ResetRegexState();
                ViewModel.MarkDocumentSaved();

                ViewModel.SetStatus($"Открыт файл: {file.Name}");
            }
            catch (Exception ex)
            {
                ViewModel.SetStatus($"Ошибка открытия файла: {ex.Message}");
            }
        }

        private async Task SaveToCurrentFileAsync()
        {
            try
            {
                StorageFile? currentFile = ViewModel.CurrentFile;

                if (currentFile is null)
                {
                    await SaveAsFileAsync();
                    return;
                }

                string text = GetEditorText();
                await FileService.WriteTextAsync(currentFile, text);

                ViewModel.SetFile(currentFile);
                ViewModel.MarkDocumentSaved();

                ViewModel.SetStatus($"Файл сохранён: {currentFile.Name}");
            }
            catch (Exception ex)
            {
                ViewModel.SetStatus($"Ошибка сохранения файла: {ex.Message}");
            }
        }

        private async Task SaveAsFileAsync()
        {
            try
            {
                StorageFile? file = await FilePickerService.PickSaveFileAsync(this, ViewModel.DocumentName);

                if (file is null)
                {
                    ViewModel.SetStatus("Сохранение отменено");
                    return;
                }

                string text = GetEditorText();
                await FileService.WriteTextAsync(file, text);

                ViewModel.SetFile(file);
                ViewModel.MarkDocumentSaved();

                ViewModel.SetStatus($"Файл сохранён: {file.Name}");
            }
            catch (Exception ex)
            {
                ViewModel.SetStatus($"Ошибка сохранения файла: {ex.Message}");
            }
        }

        private async Task ShowAboutDialogAsync()
        {
            await DialogService.ShowAboutDialogAsync(AppTitleBar.XamlRoot);
        }

        private async Task ShowShortcutsDialogAsync()
        {
            await DialogService.ShowShortcutsDialogAsync(AppTitleBar.XamlRoot);
        }

        private async void AppWindow_Closing(
            Microsoft.UI.Windowing.AppWindow sender,
            Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
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

        private async Task CreateNewDocumentAsync()
        {
            bool canContinue = await ConfirmCanContinueAsync();

            if (!canContinue)
                return;

            SetEditorText(string.Empty);

            ViewModel.ResetFileState();
            ResetRegexState();
            ViewModel.MarkDocumentSaved();

            ViewModel.SetStatus("Создан новый документ");
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

        private void ToggleStatusBarMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsStatusBarVisible = ToggleStatusBarMenuItem.IsChecked;

            if (ViewModel.IsStatusBarVisible)
                ViewModel.SetStatus("Строка состояния показана");
        }

        private void ToggleWordWrapMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.WordWrapEnabled = ToggleWordWrapMenuItem.IsChecked;

            ViewModel.SetStatus(ViewModel.WordWrapEnabled
                ? "Перенос строк включён"
                : "Перенос строк выключен");
        }

        private void EditorBox_TextChanged(object sender, RoutedEventArgs e)
        {
            UpdateEditorStatistics();

            if (SuppressTextChanged)
                return;

            ViewModel.MarkDocumentDirty();
        }

        private void NewKeyboardAccelerator_Invoked(
            KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            ViewModel.NewCommand.Execute(null);
        }

        private void OpenKeyboardAccelerator_Invoked(
            KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            ViewModel.OpenCommand.Execute(null);
        }

        private void SaveKeyboardAccelerator_Invoked(
            KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            ViewModel.SaveCommand.Execute(null);
        }

        private void SaveAsKeyboardAccelerator_Invoked(
            KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            ViewModel.SaveAsCommand.Execute(null);
        }

        private void FindKeyboardAccelerator_Invoked(
            KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            ViewModel.OpenRegexPanelCommand.Execute(null);
        }

        private void NextKeyboardAccelerator_Invoked(
            KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            ViewModel.NextMatchCommand.Execute(null);
        }

        private void PrevKeyboardAccelerator_Invoked(
            KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            ViewModel.PreviousMatchCommand.Execute(null);
        }

        private void HelpKeyboardAccelerator_Invoked(
            KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            ViewModel.ShowShortcutsCommand.Execute(null);
        }
    }
}