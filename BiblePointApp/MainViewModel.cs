using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Windows.Input;
using BiblePointApp.ViewModels;

namespace BiblePointApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public ICommand StartNewCommand { get; }
        public ICommand ResumeCommand { get; }
        public ICommand MemoryCommand { get; }
        public ICommand BookmarkCommand { get; }
        public ICommand PrayerCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ToggleLanguageCommand { get; }
        public ICommand ContinueCommand { get; }

        public string MainTitle => AppConfig.CurrentLanguage == "EN" ? "Bible Verse Transcribing" : "성경 필사 포인트";
        public string StartWritingText => AppConfig.CurrentLanguage == "EN" ? "Start New Writing" : "새 필사 시작하기";
        public string ResumeText => AppConfig.CurrentLanguage == "EN" ? "Resume Last Writing" : "이어 쓰기";
        public string LanguageToggleText => AppConfig.CurrentLanguage == "EN" ? "한국어로 변경" : "Toggle Language";

        public MainViewModel()
        {
            StartNewCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(BibleListPage)));
            ResumeCommand = new Command(async () =>
            {
                string book = Preferences.Default.Get("LastBook", "창세기");
                int chapter = Preferences.Default.Get("LastChapter", 1);
                int verse = Preferences.Default.Get("LastVerse", 1);
                int totalVerses = BibleService.GetVerseCount(book, chapter);
                if (totalVerses <= 0) totalVerses = 31;
                string route = $"{nameof(VerseWritingPage)}?Book={Uri.EscapeDataString(book)}&Chapter={chapter}&TotalVerses={totalVerses}&StartVerse={verse}";
                await Shell.Current.GoToAsync(route);
            });
            MemoryCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(MemoryPage)));
            BookmarkCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(BookmarkPage)));
            PrayerCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(PrayerPage)));
            SearchCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(SearchPage)));
            ToggleLanguageCommand = new Command(() =>
            {
                AppConfig.CurrentLanguage = AppConfig.CurrentLanguage == "KR" ? "EN" : "KR";

                RaisePropertyChanged(nameof(MainTitle));
                RaisePropertyChanged(nameof(StartWritingText));
                RaisePropertyChanged(nameof(ResumeText));
                RaisePropertyChanged(nameof(LanguageToggleText));
            });
            ContinueCommand = new Command(async () =>
            {
                string lastBook = Preferences.Default.Get("LastBook", "창세기");
                int lastChapter = Preferences.Default.Get("LastChapter", 1);
                int lastVerse = Preferences.Default.Get("LastVerse", 1);

                int totalVerses = BibleService.GetVerseCount(lastBook, lastChapter);

                await Shell.Current.GoToAsync($"//VerseWritingPage?Book={lastBook}&Chapter={lastChapter}&StartVerse={lastVerse}&TotalVerses={totalVerses}");
            });
        }
    }
}