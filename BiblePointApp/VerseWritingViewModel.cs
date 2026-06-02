using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BiblePointApp.ViewModels
{
    public class VerseWritingViewModel : BaseViewModel
    {
        public event Func<Task>? CompletedRequested;

        public ICommand SaveBookmarkCommand { get; }
        public ICommand SaveMemoryCommand { get; }
        public ICommand ToggleLanguageCommand { get; }
        public ICommand NextVerseCommand { get; }
        public ICommand PreviousVerseCommand { get; }
        public ICommand PrepareCommand { get; }

        public ObservableCollection<VerseItem> VerseItems { get; } = new ObservableCollection<VerseItem>();

        public string Book { get; set; } = string.Empty;
        public int Chapter { get; set; }
        public int TotalVerses { get; set; }
        public int StartVerse { get; set; }

        public string VerseReference => $"{Book} {Chapter}장 {CurrentVerseNumber}절";

        private string? _targetContent;
        public string? TargetContent
        {
            get => _targetContent;
            private set => SetProperty(ref _targetContent, value);
        }

        private int _currentVerseNumber;
        public int CurrentVerseNumber
        {
            get => _currentVerseNumber;
            set
            {
                if (SetProperty(ref _currentVerseNumber, value))
                {
                    OnPropertyChanged(nameof(VerseReference));
                    UpdateSelectedVerseFlag();
                }
            }
        }

        private string _inputText = string.Empty;
        public string InputText
        {
            get => _inputText;
            set { if (SetProperty(ref _inputText, value)) DebounceCheck(value); }
        }

        private VerseItem? _selectedVerse;
        public VerseItem? SelectedVerse
        {
            get => _selectedVerse;
            set
            {
                if (SetProperty(ref _selectedVerse, value) && value != null)
                {
                    CurrentVerseNumber = value.VerseNumber;
                    LoadVerseDetail();
                }
            }
        }

        private CancellationTokenSource? _cts;
        private const int DebounceMs = 250;

        private string _languageButtonText = "KR";
        public string LanguageButtonText
        {
            get => _languageButtonText;
            set => SetProperty(ref _languageButtonText, value);
        }

        public VerseWritingViewModel()
        {
            SaveBookmarkCommand = new Command(() => SaveToLocalList("MyBookmarks"));
            SaveMemoryCommand = new Command(() => SaveToLocalList("MyMemoryBook"));

            ToggleLanguageCommand = new Command(() =>
            {
                BibleService.CurrentLanguage = BibleService.CurrentLanguage == "KR" ? "EN" : "KR";
                LanguageButtonText = BibleService.CurrentLanguage == "EN" ? "EN" : "KR";
                LoadVerseDetail();
                BuildVerseList();
            });

            NextVerseCommand = new Command(async () => await MoveToNextVerse());
            PreviousVerseCommand = new Command(async () => await MoveToPreviousVerse());
            PrepareCommand = new Command(async () => await InitializeAsync());
        }

        public async Task InitializeAsync()
        {
            await BibleService.LoadBibleAsync();
            if (string.IsNullOrEmpty(Book)) Book = Preferences.Default.Get("LastBook", "창세기");
            if (Chapter == 0) Chapter = Preferences.Default.Get("LastChapter", 1);
            CurrentVerseNumber = StartVerse != 0 ? StartVerse : Preferences.Default.Get("LastVerse", 1);
            TotalVerses = TotalVerses != 0 ? TotalVerses : BibleService.GetVerseCount(Book, Chapter);

            LanguageButtonText = BibleService.CurrentLanguage == "EN" ? "EN" : "KR";

            OnPropertyChanged(nameof(VerseReference));
            LoadVerseDetail();
            BuildVerseList();
        }

        public void LoadVerseDetail()
        {
            TotalVerses = BibleService.GetVerseCount(Book, Chapter);
            var db = BibleService.GetVerseDetail(Book, Chapter, CurrentVerseNumber);
            TargetContent = db == null ? string.Empty : (BibleService.CurrentLanguage == "EN" ? db.Content_EN : db.Content) ?? string.Empty;

            var sel = VerseItems.FirstOrDefault(v => v.VerseNumber == CurrentVerseNumber);
            if (sel != null) SelectedVerse = sel;
            UpdateSelectedVerseFlag();
        }

        private void BuildVerseList()
        {
            VerseItems.Clear();
            for (int i = 1; i <= TotalVerses; i++)
            {
                var db = BibleService.GetVerseDetail(Book, Chapter, i);
                var text = db == null ? $"[{i}절 데이터 없음]" : (BibleService.CurrentLanguage == "EN" ? db.Content_EN : db.Content) ?? "";
                VerseItems.Add(new VerseItem { VerseNumber = i, Text = text, IsCurrent = i == CurrentVerseNumber });
            }
        }

        private void UpdateSelectedVerseFlag()
        {
            foreach (var item in VerseItems) item.IsCurrent = item.VerseNumber == CurrentVerseNumber;
        }

        private void DebounceCheck(string text)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var t = _cts.Token;
            string typedNoSpaces = (text ?? "").Replace(" ", "");

            _ = Task.Run(async () =>
            {
                try { await Task.Delay(DebounceMs, t); } catch { return; }
                if (t.IsCancellationRequested) return;

                string targetNoSpaces = (TargetContent ?? "").Replace(" ", "");
                if (!string.IsNullOrEmpty(targetNoSpaces) && typedNoSpaces == targetNoSpaces)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () => await OnCompleteFromViewAsync());
                }
            }, t);
        }

        public async Task OnCompleteFromViewAsync()
        {
            string key = $"Completed_{Book}_{Chapter}_{CurrentVerseNumber}";
            if (!Preferences.Default.Get(key, false))
            {
                Preferences.Default.Set(key, true);
                if (CompletedRequested != null) await CompletedRequested.Invoke();
                await Task.Delay(600);
            }
            await MoveToNextVerse();
        }

        private async Task MoveToNextVerse()
        {
            int total = BibleService.GetVerseCount(Book, Chapter);
            if (CurrentVerseNumber < total)
            {
                CurrentVerseNumber++;
            }
            else if (Chapter < BibleService.GetMaxChapterCount(Book))
            {
                Chapter++;
                CurrentVerseNumber = 1;
                BuildVerseList();
                OnPropertyChanged(nameof(VerseReference));
            }
            Preferences.Default.Set("LastVerse", CurrentVerseNumber);
            LoadVerseDetail();
        }

        private async Task MoveToPreviousVerse()
        {
            if (CurrentVerseNumber > 1)
            {
                CurrentVerseNumber--;
            }
            else if (Chapter > 1)
            {
                Chapter--;
                TotalVerses = BibleService.GetVerseCount(Book, Chapter);
                CurrentVerseNumber = TotalVerses;
                BuildVerseList();
                OnPropertyChanged(nameof(VerseReference));
            }
            Preferences.Default.Set("LastVerse", CurrentVerseNumber);
            LoadVerseDetail();
        }

        private void SaveToLocalList(string key)
        {
            if (string.IsNullOrEmpty(TargetContent)) return;

            var list = JsonSerializer.Deserialize<List<Bookmark>>(Preferences.Default.Get(key, "[]")) ?? new List<Bookmark>();

            list.RemoveAll(x => x.Book == Book && x.Chapter == Chapter && x.Verse == CurrentVerseNumber);

            list.Insert(0, new Bookmark
            {
                Book = Book,
                Chapter = Chapter,
                Verse = CurrentVerseNumber,
                Content = TargetContent!,
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

            Preferences.Default.Set(key, JsonSerializer.Serialize(list));
        }
    }

    public class VerseItem : BaseViewModel
    {
        private bool _isCurrent;
        public int VerseNumber { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCurrent
        {
            get => _isCurrent;
            set => SetProperty(ref _isCurrent, value);
        }
    }
}