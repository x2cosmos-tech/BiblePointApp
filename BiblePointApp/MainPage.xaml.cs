using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using BiblePointApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BiblePointApp
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _vm;

        public MainPage() : this(App.Services.GetRequiredService<MainViewModel>()) { }

        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = _vm;
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
            UpdateLanguageUI();
        }

        private void OnToggleLanguageClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                vm.ToggleLanguageCommand.Execute(null);
                UpdateLanguageUI();
                return;
            }

            AppConfig.CurrentLanguage = AppConfig.CurrentLanguage == "KR" ? "EN" : "KR";
            UpdateLanguageUI();
        }

        private async void OnSearchButtonClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                if (vm.SearchCommand.CanExecute(null))
                {
                    vm.SearchCommand.Execute(null);
                    return;
                }
            }
            await Shell.Current.GoToAsync(nameof(SearchPage));
        }

        private async void OnStartNewWritingClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm && vm.StartNewCommand.CanExecute(null))
            {
                vm.StartNewCommand.Execute(null);
                return;
            }
            await Shell.Current.GoToAsync(nameof(BibleListPage));
        }

        private async void OnResumeWritingClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm && vm.ResumeCommand.CanExecute(null))
            {
                vm.ResumeCommand.Execute(null);
                return;
            }

            string book = Preferences.Default.Get("LastBook", "창세기");
            int chapter = Preferences.Default.Get("LastChapter", 1);
            int verse = Preferences.Default.Get("LastVerse", 1);
            int totalVerses = BibleService.GetVerseCount(book, chapter);
            if (totalVerses <= 0) totalVerses = 31;
            string route = $"{nameof(VerseWritingPage)}?Book={Uri.EscapeDataString(book)}&Chapter={chapter}&TotalVerses={totalVerses}&StartVerse={verse}";
            await Shell.Current.GoToAsync(route);
        }

        private async void OnMemoryBookClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm && vm.MemoryCommand.CanExecute(null))
            {
                vm.MemoryCommand.Execute(null);
                return;
            }
            await Shell.Current.GoToAsync(nameof(MemoryPage));
        }

        private async void OnBookmarkClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm && vm.BookmarkCommand.CanExecute(null))
            {
                vm.BookmarkCommand.Execute(null);
                return;
            }
            await Shell.Current.GoToAsync(nameof(BookmarkPage));
        }

        private async void OnPrayerNotesClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm && vm.PrayerCommand.CanExecute(null))
            {
                vm.PrayerCommand.Execute(null);
                return;
            }
            await Shell.Current.GoToAsync(nameof(PrayerPage));
        }

        private void UpdateLanguageUI()
        {
            bool isEnglish = AppConfig.CurrentLanguage == "EN";

            try
            {
                if (this.FindByName<Label>("MainTitleLabel") is Label mainTitle)
                    mainTitle.Text = isEnglish ? "Bible Verse Transcribing" : "성경 필사 포인트";

                if (this.FindByName<Button>("StartWritingButton") is Button startBtn)
                    startBtn.Text = isEnglish ? "Start New Writing" : "새 필사 시작하기";

                if (this.FindByName<Button>("ResumeButton") is Button resumeBtn)
                    resumeBtn.Text = isEnglish ? "Resume Last Writing" : "이어 쓰기";

                if (this.FindByName<Button>("MemoryBookButton") is Button memBtn)
                    memBtn.Text = isEnglish ? "My Memory Book" : "나의 암기장";

                if (this.FindByName<Button>("BookmarkButton") is Button bookBtn)
                    bookBtn.Text = isEnglish ? "Bookmarks" : "책갈피";

                if (this.FindByName<Button>("PrayerNotesButton") is Button prayerBtn)
                    prayerBtn.Text = isEnglish ? "My Prayer Notes" : "나의 기도 노트";

                if (this.FindByName<Button>("LanguageToggleButton") is Button langBtn)
                    langBtn.Text = isEnglish ? "한국어로 변경" : "Toggle Language";
            }
            catch
            {
                // 안전하게 무시
            }
        }
    }
}