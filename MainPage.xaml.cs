using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;
using BiblePointApp.Models;

namespace BiblePointApp
{
    public partial class MainPage : ContentPage
    {
        private BibleVerse? _todayVerse;
        private string _todayVerseCacheKey = "";

        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LoadUserPoints();

            if (!BibleService.IsDataLoaded)
            {
                if (TodayVerseLabel != null)
                {
                    TodayVerseLabel.Text = "오늘의 말씀을 불러오는 중입니다...";
                }
                await BibleService.LoadBibleAsync();
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadTodayVerse();
            });
        }

        private void LoadUserPoints()
        {
            int points = Preferences.Default.Get("UserPoints", 0);
            if (UserPointsLabel != null)
            {
                UserPointsLabel.Text = $"{points:N0} P";
            }
        }

        // 🎯 최적화: 날짜 기반 캐싱 (매일 1회만 계산)
        private void LoadTodayVerse()
        {
            try
            {
                if (BibleService.AllVerses == null || BibleService.AllVerses.Count == 0)
                {
                    if (TodayVerseLabel != null)
                    {
                        TodayVerseLabel.Text = "성경 데이터가 비어있어 불러올 수 없습니다.";
                        TodayVerseLabel.TextColor = Colors.Red;
                    }
                    return;
                }

                DateTime today = DateTime.Today;
                _todayVerseCacheKey = $"TodayVerse_{today:yyyy-MM-dd}";

                // 🎯 캐시 확인 - 이미 오늘 구절을 로드했으면 재사용
                if (Preferences.Default.ContainsKey(_todayVerseCacheKey))
                {
                    string cached = Preferences.Default.Get(_todayVerseCacheKey, "");
                    if (!string.IsNullOrEmpty(cached))
                    {
                        try
                        {
                            _todayVerse = System.Text.Json.JsonSerializer.Deserialize<BibleVerse>(cached);
                            UpdateTodayVerseUI();
                            return;
                        }
                        catch { }
                    }
                }

                // 🎯 새로운 구절 선택 (날짜 기반 시드 = 매일 같은 구절)
                int dateSeed = today.Year * 10000 + today.Month * 100 + today.Day;
                var random = new Random(dateSeed);
                int randomIndex = random.Next(0, BibleService.AllVerses.Count);
                _todayVerse = BibleService.AllVerses[randomIndex];

                // 🎯 캐시 저장 (24시간 유지)
                try
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(_todayVerse);
                    Preferences.Default.Set(_todayVerseCacheKey, json);
                }
                catch { }

                UpdateTodayVerseUI();
            }
            catch (Exception ex)
            {
                if (TodayVerseLabel != null)
                {
                    TodayVerseLabel.Text = $"말씀 로딩 중 오류 발생: {ex.Message}";
                    TodayVerseLabel.TextColor = Colors.Red;
                }
            }
        }

        private void UpdateTodayVerseUI()
        {
            if (_todayVerse == null || TodayVerseLabel == null) return;

            bool isEn = BibleService.CurrentLanguage == "EN";

            string content = isEn ? _todayVerse.Content_EN : _todayVerse.Content;
            string bookName = isEn ? GetEnglishBookName(_todayVerse.Book) : _todayVerse.Book;

            TodayVerseLabel.Text = $"[{bookName} {_todayVerse.Chapter}:{_todayVerse.Verse}]\n\n{content}";
            TodayVerseLabel.TextColor = Color.FromArgb("#333333");
        }

        private void OnTranslationClicked(object sender, EventArgs e)
        {
            BibleService.CurrentLanguage = BibleService.CurrentLanguage == "EN" ? "KR" : "EN";

            if (BindingContext is MainViewModel vm)
            {
                vm.UpdateLanguageProperties();
            }

            UpdateTodayVerseUI();
        }

        private string GetEnglishBookName(string koreanBook)
        {
            return koreanBook switch
            {
                "창세기" => "Genesis",
                "출애굽기" => "Exodus",
                "레위기" => "Leviticus",
                "민수기" => "Numbers",
                "신명기" => "Deuteronomy",
                "여호수아" => "Joshua",
                "사사기" => "Judges",
                "룻기" => "Ruth",
                "사무엘상" => "1 Samuel",
                "사무엘하" => "2 Samuel",
                "열왕기상" => "1 Kings",
                "열왕기하" => "2 Kings",
                "역대상" => "1 Chronicles",
                "역대하" => "2 Chronicles",
                "에스라" => "Ezra",
                "느헤미야" => "Nehemiah",
                "에스더" => "Esther",
                "욥기" => "Job",
                "시편" => "Psalms",
                "잠언" => "Proverbs",
                "전도서" => "Ecclesiastes",
                "아가" => "Song of Solomon",
                "이사야" => "Isaiah",
                "예레미야" => "Jeremiah",
                "예레미야애가" => "Lamentations",
                "에스겔" => "Ezekiel",
                "다니엘" => "Daniel",
                "호세아" => "Hosea",
                "요엘" => "Joel",
                "아모스" => "Amos",
                "오바디야" => "Obadiah",
                "요나" => "Jonah",
                "미가" => "Micah",
                "나훔" => "Nahum",
                "하박국" => "Habakkuk",
                "스바냐" => "Zephaniah",
                "학개" => "Haggai",
                "스가랴" => "Zechariah",
                "말라기" => "Malachi",
                "마태복음" => "Matthew",
                "마가복음" => "Mark",
                "누가복음" => "Luke",
                "요한복음" => "John",
                "사도행전" => "Acts",
                "로마서" => "Romans",
                "고린도전서" => "1 Corinthians",
                "고린도후서" => "2 Corinthians",
                "갈라디아서" => "Galatians",
                "에베소서" => "Ephesians",
                "빌립보서" => "Philippians",
                "골로새서" => "Colossians",
                "데살로니가전서" => "1 Thessalonians",
                "데살로니가후서" => "2 Thessalonians",
                "디모데전서" => "1 Timothy",
                "디모데후서" => "2 Timothy",
                "디도서" => "Titus",
                "빌레몬서" => "Philemon",
                "히브리서" => "Hebrews",
                "야고보서" => "James",
                "베드로전서" => "1 Peter",
                "베드로후서" => "2 Peter",
                "요한일서" => "1 John",
                "요한이서" => "2 John",
                "요한삼서" => "3 John",
                "유다서" => "Jude",
                "요한계시록" => "Revelation",
                _ => koreanBook
            };
        }

        private async void OnVerseItemTapped(object sender, EventArgs e)
        {
            if (_todayVerse == null) return;

            bool isEn = BibleService.CurrentLanguage == "EN";

            string title = isEn ? "Choose an action" : "원하는 작업을 선택하세요";
            string cancel = isEn ? "Cancel" : "취소";

            string actionWrite = isEn ? "✍️ Start New Writing" : "✍️ 새 필사 시작";
            string actionMemory = isEn ? "🧠 Add to Memorization" : "🧠 암기장 추가";
            string actionBookmark = isEn ? "🔖 Add to Bookmarks" : "🔖 책갈피 추가";

            string action = await DisplayActionSheet(title, cancel, null, actionWrite, actionMemory, actionBookmark);

            if (action == actionWrite)
            {
                int maxVerse = BibleService.GetVerseCount(_todayVerse.Book, _todayVerse.Chapter);
                if (maxVerse <= 0) maxVerse = 30;

                string route = $"VerseWritingPage?Book={Uri.EscapeDataString(_todayVerse.Book)}&Chapter={_todayVerse.Chapter}&TotalVerses={maxVerse}&StartVerse={_todayVerse.Verse}";
                await Shell.Current.GoToAsync(route);
            }
            else if (action == actionMemory)
            {
                await SaveVerseToLocalListAsync("MyMemoryBook");
            }
            else if (action == actionBookmark)
            {
                await SaveVerseToLocalListAsync("MyBookmarks");
            }
        }

        private async Task SaveVerseToLocalListAsync(string key)
        {
            if (_todayVerse == null) return;

            try
            {
                string content = BibleService.CurrentLanguage == "EN" ? _todayVerse.Content_EN : _todayVerse.Content;
                var database = App.Services.GetRequiredService<BibleDatabase>();

                bool isMemory = (key == "MyMemoryBook");

                await database.SaveBookmarkAsync(_todayVerse.Book, _todayVerse.Chapter, _todayVerse.Verse, content, isMemory);

                string listName = isMemory ? "🧠 암기장" : "🔖 책갈피";
                await DisplayAlert("저장 완료", $"{_todayVerse.Book} {_todayVerse.Chapter}장 {_todayVerse.Verse}절이 {listName}에 추가되었습니다.", "확인");
            }
            catch (Exception ex)
            {
                await DisplayAlert("오류", $"저장 중 문제가 발생했습니다: {ex.Message}", "확인");
            }
        }

        #region Navigation
        private async void OnStartWritingClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("BibleListPage");

        private async void OnContinueClicked(object sender, EventArgs e)
        {
            try
            {
                string book = Preferences.Default.Get("LastBook", "창세기");
                int chapter = Preferences.Default.Get("LastChapter", 1);
                int verse = Preferences.Default.Get("LastVerse", 1);
                int maxVerse = BibleService.GetVerseCount(book, chapter);
                if (maxVerse <= 0) maxVerse = 31;

                string route = $"VerseWritingPage?Book={Uri.EscapeDataString(book)}&Chapter={chapter}&StartVerse={verse}&TotalVerses={maxVerse}";
                await Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                await DisplayAlert("오류", $"필사 화면 이동 중 문제 발생: {ex.Message}", "확인");
            }
        }

        private async void OnMemoryBookClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("MemoryPage");
        private async void OnBookmarkClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("BookmarkPage");
        private async void OnSearchClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("SearchPage");
        #endregion
    }
}
