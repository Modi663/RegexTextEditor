using RegexTextEditor.Infrastructure;
using RegexTextEditor.Models;
using RegexTextEditor.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace RegexTextEditor.ViewModels
{
    public sealed class MainWindowViewModel : ObservableObject
    {
        private readonly RegexService _regexService = new();
        private readonly RegexSearchState _regexState = new();

        private string _statusText = "Готово";
        private bool _isDirty;
        private int _linesCount = 1;
        private int _charactersCount;
        private bool _wordWrapEnabled;
        private bool _isStatusBarVisible = true;
        private bool _isRegexPanelVisible;
        private string _pattern = string.Empty;
        private string _replacement = string.Empty;
        private AppThemeMode _themeMode = AppThemeMode.System;

        private bool _hasRegexError;
        private string _regexErrorText = string.Empty;
        public EditorFileState FileState { get; } = new();

        public AsyncRelayCommand NewCommand { get; private set; } = new(() => Task.CompletedTask);
        public AsyncRelayCommand OpenCommand { get; private set; } = new(() => Task.CompletedTask);
        public AsyncRelayCommand SaveCommand { get; private set; } = new(() => Task.CompletedTask);
        public AsyncRelayCommand SaveAsCommand { get; private set; } = new(() => Task.CompletedTask);
        public AsyncRelayCommand ExitCommand { get; private set; } = new(() => Task.CompletedTask);
        public AsyncRelayCommand ShowAboutCommand { get; private set; } = new(() => Task.CompletedTask);
        public AsyncRelayCommand ShowShortcutsCommand { get; private set; } = new(() => Task.CompletedTask);

        public RelayCommand OpenRegexPanelCommand { get; private set; } = new(() => { });
        public RelayCommand CloseRegexPanelCommand { get; private set; } = new(() => { });
        public RelayCommand FindCommand { get; private set; } = new(() => { });
        public RelayCommand NextMatchCommand { get; private set; } = new(() => { });
        public RelayCommand PreviousMatchCommand { get; private set; } = new(() => { });
        public RelayCommand ReplaceCurrentCommand { get; private set; } = new(() => { });
        public RelayCommand ReplaceAllCommand { get; private set; } = new(() => { });
        public RelayCommand ClearDocumentCommand { get; private set; } = new(() => { });

        public RelayCommand UseSystemThemeCommand { get; private set; } = new(() => { });
        public RelayCommand UseLightThemeCommand { get; private set; } = new(() => { });
        public RelayCommand UseDarkThemeCommand { get; private set; } = new(() => { });

        public string DocumentName => FileState.DocumentName;

        public string DocumentTitle
        {
            get
            {
                string dirtyMark = IsDirty ? "*" : string.Empty;
                return $"{dirtyMark}{DocumentName}";
            }
        }

        public string WindowTitle => $"{DocumentTitle} - Regex Text Editor";

        public StorageFile? CurrentFile => FileState.File;

        public IReadOnlyList<SearchResult> Matches => _regexState.Matches;

        public int CurrentMatchIndex => _regexState.CurrentMatchIndex;

        public int MatchesCount => _regexState.Matches.Count;

        public int ReplacedCount => _regexState.ReplacedCount;

        public string MatchesCountText => $"Найдено: {MatchesCount}";

        public string CurrentMatchText
        {
            get
            {
                if (MatchesCount == 0 || CurrentMatchIndex < 0)
                    return "Текущее: 0/0";

                return $"Текущее: {CurrentMatchIndex + 1}/{MatchesCount}";
            }
        }

        public string ReplacedCountText => $"Заменено: {ReplacedCount}";

        public string LinesCountText => $"Строк: {LinesCount}";

        public string CharactersCountText => $"Символов: {CharactersCount}";

        public bool IsSystemThemeSelected => ThemeMode == AppThemeMode.System;
        public bool IsLightThemeSelected => ThemeMode == AppThemeMode.Light;
        public bool IsDarkThemeSelected => ThemeMode == AppThemeMode.Dark;

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                if (!SetProperty(ref _isDirty, value))
                    return;

                OnPropertyChanged(nameof(DocumentTitle));
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public int LinesCount
        {
            get => _linesCount;
            private set
            {
                if (SetProperty(ref _linesCount, value))
                    OnPropertyChanged(nameof(LinesCountText));
            }
        }

        public int CharactersCount
        {
            get => _charactersCount;
            private set
            {
                if (SetProperty(ref _charactersCount, value))
                    OnPropertyChanged(nameof(CharactersCountText));
            }
        }

        public bool WordWrapEnabled
        {
            get => _wordWrapEnabled;
            set => SetProperty(ref _wordWrapEnabled, value);
        }

        public bool IsStatusBarVisible
        {
            get => _isStatusBarVisible;
            set => SetProperty(ref _isStatusBarVisible, value);
        }

        public bool IsRegexPanelVisible
        {
            get => _isRegexPanelVisible;
            private set => SetProperty(ref _isRegexPanelVisible, value);
        }

        public string Pattern
        {
            get => _pattern;
            set
            {
                if (!SetProperty(ref _pattern, value))
                    return;

                ClearRegexError();
            }
        }

        public string Replacement
        {
            get => _replacement;
            set => SetProperty(ref _replacement, value);
        }

        public bool HasRegexError
        {
            get => _hasRegexError;
            private set => SetProperty(ref _hasRegexError, value);
        }

        public string RegexErrorText
        {
            get => _regexErrorText;
            private set => SetProperty(ref _regexErrorText, value);
        }

        public AppThemeMode ThemeMode
        {
            get => _themeMode;
            private set
            {
                if (!SetProperty(ref _themeMode, value))
                    return;

                OnPropertyChanged(nameof(IsSystemThemeSelected));
                OnPropertyChanged(nameof(IsLightThemeSelected));
                OnPropertyChanged(nameof(IsDarkThemeSelected));

                StatusText = value switch
                {
                    AppThemeMode.System => "Тема: системная",
                    AppThemeMode.Light => "Тема: светлая",
                    AppThemeMode.Dark => "Тема: тёмная",
                    _ => "Тема изменена"
                };
            }
        }

        public void ConfigureCommands(
            Func<Task> createNewDocumentAsync,
            Func<Task> openDocumentAsync,
            Func<Task> saveAsync,
            Func<Task> saveAsAsync,
            Func<Task> exitAsync,
            Action openRegexPanel,
            Action closeRegexPanel,
            Action find,
            Action nextMatch,
            Action previousMatch,
            Action replaceCurrent,
            Action replaceAll,
            Action clearDocument,
            Func<Task> showAboutAsync,
            Func<Task> showShortcutsAsync)
        {
            NewCommand = new AsyncRelayCommand(createNewDocumentAsync);
            OpenCommand = new AsyncRelayCommand(openDocumentAsync);
            SaveCommand = new AsyncRelayCommand(saveAsync);
            SaveAsCommand = new AsyncRelayCommand(saveAsAsync);
            ExitCommand = new AsyncRelayCommand(exitAsync);
            ShowAboutCommand = new AsyncRelayCommand(showAboutAsync);
            ShowShortcutsCommand = new AsyncRelayCommand(showShortcutsAsync);

            OpenRegexPanelCommand = new RelayCommand(openRegexPanel);
            CloseRegexPanelCommand = new RelayCommand(closeRegexPanel);
            FindCommand = new RelayCommand(find);
            NextMatchCommand = new RelayCommand(nextMatch);
            PreviousMatchCommand = new RelayCommand(previousMatch);
            ReplaceCurrentCommand = new RelayCommand(replaceCurrent);
            ReplaceAllCommand = new RelayCommand(replaceAll);
            ClearDocumentCommand = new RelayCommand(clearDocument);

            UseSystemThemeCommand = new RelayCommand(() => SetThemeMode(AppThemeMode.System));
            UseLightThemeCommand = new RelayCommand(() => SetThemeMode(AppThemeMode.Light));
            UseDarkThemeCommand = new RelayCommand(() => SetThemeMode(AppThemeMode.Dark));

            NotifyCommandsChanged();
        }

        public void UpdateStatistics(EditorStatistics statistics)
        {
            LinesCount = statistics.Lines;
            CharactersCount = statistics.Characters;
        }

        public void SetStatus(string status)
        {
            StatusText = status;
        }

        public void MarkDocumentDirty()
        {
            if (IsDirty)
                return;

            IsDirty = true;
        }

        public void MarkDocumentSaved()
        {
            IsDirty = false;
        }

        public void SetFile(StorageFile file)
        {
            FileState.File = file;
            FileState.FilePath = file.Path;
            FileState.DocumentName = file.Name;

            NotifyDocumentNameChanged();
        }

        public void ResetFileState()
        {
            FileState.Reset();
            NotifyDocumentNameChanged();
        }

        public void OpenRegexPanel()
        {
            IsRegexPanelVisible = true;
            StatusText = "Панель regex открыта";
        }

        public void CloseRegexPanel()
        {
            ClearRegexResults(resetReplacedCount: true);
            IsRegexPanelVisible = false;
            StatusText = "Панель regex закрыта";
        }

        public void ResetRegexState(bool clearInputs)
        {
            _regexState.Reset();

            if (clearInputs)
            {
                Pattern = string.Empty;
                Replacement = string.Empty;
            }

            NotifyRegexStateChanged();
        }

        public void ClearRegexResults(bool resetReplacedCount)
        {
            _regexState.ClearMatches();

            if (resetReplacedCount)
                _regexState.ReplacedCount = 0;

            NotifyRegexStateChanged();
        }

        public void FindMatches(string text)
        {
            if (!TryPrepareRegexOperation())
                return;

            _regexState.ReplacedCount = 0;
            _regexState.SetMatches(_regexService.FindMatches(text, Pattern));

            NotifyRegexStateChanged();

            StatusText = MatchesCount > 0
                ? "Поиск выполнен"
                : "Совпадения не найдены";
        }

        public bool MoveNextMatch()
        {
            bool moved = _regexState.MoveNext();

            if (!moved)
                return false;

            NotifyRegexStateChanged();
            StatusText = "Переход к следующему совпадению";
            return true;
        }

        public bool MovePreviousMatch()
        {
            bool moved = _regexState.MovePrevious();

            if (!moved)
                return false;

            NotifyRegexStateChanged();
            StatusText = "Переход к предыдущему совпадению";
            return true;
        }

        public bool TryReplaceCurrent(string text, out string newText)
        {
            newText = text;

            if (!TryPrepareRegexOperation())
                return false;

            if (MatchesCount == 0 || CurrentMatchIndex < 0)
                return false;

            newText = _regexService.ReplaceOnlyCurrent(text, Pattern, Replacement, CurrentMatchIndex);

            _regexState.ReplacedCount++;
            _regexState.SetMatches(_regexService.FindMatches(newText, Pattern));

            NotifyRegexStateChanged();

            StatusText = "Текущее совпадение заменено";
            return true;
        }

        public bool TryReplaceAll(string text, out string newText)
        {
            newText = text;

            if (!TryPrepareRegexOperation())
                return false;

            List<SearchResult> matchesBeforeReplace = _regexService.FindMatches(text, Pattern);

            _regexState.ReplacedCount = matchesBeforeReplace.Count;

            newText = _regexService.ReplaceAll(text, Pattern, Replacement);

            _regexState.SetMatches(_regexService.FindMatches(newText, Pattern));

            NotifyRegexStateChanged();

            StatusText = "Замена всех совпадений выполнена";
            return _regexState.ReplacedCount > 0;
        }

        private bool TryPrepareRegexOperation()
        {
            if (string.IsNullOrEmpty(Pattern))
            {
                ClearRegexError();
                ClearRegexResults(resetReplacedCount: true);
                StatusText = "Введите регулярное выражение";
                return false;
            }

            if (_regexService.TryValidatePattern(Pattern, out string errorMessage))
            {
                ClearRegexError();
                return true;
            }

            SetRegexError(errorMessage);
            ClearRegexResults(resetReplacedCount: true);
            StatusText = "Ошибка в регулярном выражении";

            return false;
        }

        private void SetRegexError(string errorMessage)
        {
            RegexErrorText = $"Ошибка regex: {errorMessage}";
            HasRegexError = true;
        }

        private void ClearRegexError()
        {
            if (!HasRegexError && string.IsNullOrEmpty(RegexErrorText))
                return;

            HasRegexError = false;
            RegexErrorText = string.Empty;
        }

        private void SetThemeMode(AppThemeMode themeMode)
        {
            ThemeMode = themeMode;
        }

        private void NotifyDocumentNameChanged()
        {
            OnPropertyChanged(nameof(DocumentName));
            OnPropertyChanged(nameof(DocumentTitle));
            OnPropertyChanged(nameof(WindowTitle));
        }

        private void NotifyRegexStateChanged()
        {
            OnPropertyChanged(nameof(Matches));
            OnPropertyChanged(nameof(CurrentMatchIndex));
            OnPropertyChanged(nameof(MatchesCount));
            OnPropertyChanged(nameof(ReplacedCount));
            OnPropertyChanged(nameof(MatchesCountText));
            OnPropertyChanged(nameof(CurrentMatchText));
            OnPropertyChanged(nameof(ReplacedCountText));
        }

        private void NotifyCommandsChanged()
        {
            OnPropertyChanged(nameof(NewCommand));
            OnPropertyChanged(nameof(OpenCommand));
            OnPropertyChanged(nameof(SaveCommand));
            OnPropertyChanged(nameof(SaveAsCommand));
            OnPropertyChanged(nameof(ExitCommand));
            OnPropertyChanged(nameof(ShowAboutCommand));
            OnPropertyChanged(nameof(ShowShortcutsCommand));

            OnPropertyChanged(nameof(OpenRegexPanelCommand));
            OnPropertyChanged(nameof(CloseRegexPanelCommand));
            OnPropertyChanged(nameof(FindCommand));
            OnPropertyChanged(nameof(NextMatchCommand));
            OnPropertyChanged(nameof(PreviousMatchCommand));
            OnPropertyChanged(nameof(ReplaceCurrentCommand));
            OnPropertyChanged(nameof(ReplaceAllCommand));
            OnPropertyChanged(nameof(ClearDocumentCommand));

            OnPropertyChanged(nameof(UseSystemThemeCommand));
            OnPropertyChanged(nameof(UseLightThemeCommand));
            OnPropertyChanged(nameof(UseDarkThemeCommand));
        }
    }
}