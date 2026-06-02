using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;
using Microsoft.Maui.Storage;
using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiblePointApp
{
    [QueryProperty(nameof(Book), "Book")]
    [QueryProperty(nameof(Chapter), "Chapter")]
    [QueryProperty(nameof(TotalVerses), "TotalVerses")]
    [QueryProperty(nameof(StartVerse), "StartVerse")]
    public partial class VerseWritingPage : ContentPage
    {
        private string _selectedBook = string.Empty;
        private int _selectedChapter;
        private int _currentVerseNumber;
        private int _totalVerses;

        private string? _targetContent;
        private readonly IAudioManager? _audioManager;
        private IAudioPlayer? _bgmPlayer;
        private bool _isBgmPlaying = true;

        public VerseWritingPage()
        {
            InitializeComponent();
            _audioManager = AudioManager.Current;

            // 기본값 restore
            _selectedBook = Preferences.Default.Get("LastBook", "창세기");
            _selectedChapter = Preferences.Default.Get("LastChapter", 1);
            _currentVerseNumber = Preferences.Default.Get("LastVerse", 1);
            _totalVerses = BibleService.GetVerseCount(_selectedBook, _selectedChapter);
        }

        // QueryProperty 매핑용 public 프로퍼티들
        public string Book
        {
            get => _selectedBook;
            set { if (!string.IsNullOrEmpty(value)) _selectedBook = Uri.UnescapeDataString(value); }
        }

        public int Chapter
        {
            get => _selectedChapter;
            set { if (value > 0) _selectedChapter = value; }
        }

        public int TotalVerses
        {
            get => _totalVerses;
            set { if (value > 0) _totalVerses = value; }
        }

        public int StartVerse
        {
            get => _currentVerseNumber;
            set { if (value > 0) _currentVerseNumber = value; }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                await BibleService.LoadBibleAsync();

                if (!string.IsNullOrEmpty(_selectedBook))
                    BookChapterLabel.Text = $"{_selectedBook} {_selectedChapter}장";

                _totalVerses = BibleService.GetVerseCount(_selectedBook, _selectedChapter);
                if (_totalVerses <= 0) _totalVerses = 25;

                BuildEntireVerseListUI();
                LoadVerseDetail();
                await PrepareBgm();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VerseWritingPage.OnAppearing error: {ex}");
            }
        }

        private void CreateCharLabels(string content)
        {
            if (GreyVerseLayout == null) return;
            GreyVerseLayout.Children.Clear();
            if (string.IsNullOrEmpty(content)) return;

            foreach (char c in content)
            {
                GreyVerseLayout.Children.Add(new Label
                {
                    Text = c.ToString(),
                    FontSize = 24,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#A0A0A0"),
                    Margin = new Thickness(c == ' ' ? 5 : 1, 0),
                    VerticalOptions = LayoutOptions.Center
                });
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_targetContent) || GreyVerseLayout == null) return;
                string typedNoSpaces = (e.NewTextValue ?? "").Replace(" ", "");
                string targetNoSpaces = _targetContent.Replace(" ", "");

                var charLabels = GreyVerseLayout.Children.OfType<Label>().ToList();
                int typedCharIdx = 0;
                for (int i = 0; i < charLabels.Count; i++)
                {
                    if (charLabels[i].Text == " ") continue;

                    if (typedCharIdx < typedNoSpaces.Length)
                    {
                        if (typedCharIdx < targetNoSpaces.Length)
                            charLabels[i].TextColor = (typedNoSpaces[typedCharIdx] == targetNoSpaces[typedCharIdx]) ? Colors.Black : Colors.Red;
                        else
                            charLabels[i].TextColor = Colors.Red;
                        typedCharIdx++;
                    }
                    else
                    {
                        charLabels[i].TextColor = Color.FromArgb("#A0A0A0");
                    }
                }

                if (!string.IsNullOrEmpty(targetNoSpaces) && typedNoSpaces == targetNoSpaces)
                {
                    CompleteVerse();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnTextChanged error: {ex}");
            }
        }

        private async void CompleteVerse()
        {
            if (WritingEntry == null) return;

            WritingEntry.IsEnabled = false;
            try
            {
                string completionKey = $"Completed_{_selectedBook}_{_selectedChapter}_{_currentVerseNumber}";
                if (!Preferences.Default.Get(completionKey, false))
                {
                    Preferences.Default.Set(completionKey, true);
                    try { await this.ShowPopupAsync(new PointAwardedPopup(1)); } catch { /* ignore popup errors */ }
                    await Task.Delay(800);
                }
                await MoveToNextVerse();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CompleteVerse error: {ex}");
            }
            finally
            {
                WritingEntry.IsEnabled = true;
            }
        }

        private async Task MoveToNextVerse()
        {
            if (_currentVerseNumber < _totalVerses)
            {
                _currentVerseNumber++;
                LoadVerseDetail();
                RefreshListHighlights();
                SaveCurrentPosition();
            }
            else
            {
                int maxChap = BibleService.GetMaxChapterCount(_selectedBook);
                if (_selectedChapter < maxChap)
                {
                    bool answer = await DisplayAlert("장 완료", $"{_selectedChapter}장을 모두 필사했습니다!\n다음 장으로 이동할까요?", "이어서 하기", "그만하기");
                    if (answer)
                    {
                        int nextChapter = _selectedChapter + 1;
                        int nextTotal = BibleService.GetVerseCount(_selectedBook, nextChapter);
                        await Shell.Current.GoToAsync($"{nameof(VerseWritingPage)}?Book={Uri.EscapeDataString(_selectedBook)}&Chapter={nextChapter}&TotalVerses={nextTotal}&StartVerse=1");
                    }
                }
                else
                {
                    await DisplayAlert("완료", "모든 필사를 완수하셨습니다!", "확인");
                }
            }
        }

        private void LoadVerseDetail()
        {
            var dbVerse = BibleService.GetVerseDetail(_selectedBook, _selectedChapter, _currentVerseNumber);
            _targetContent = (dbVerse != null) ? ((BibleService.CurrentLanguage == "EN") ? dbVerse.Content_EN : dbVerse.Content) : string.Empty;
            if (string.IsNullOrWhiteSpace(_targetContent)) _targetContent = "[본문을 불러오지 못했습니다]";

            if (InstructionLabel != null) InstructionLabel.Text = "아래 구절을 따라 적으세요";
            if (CurrentVerseNumberLabel != null) CurrentVerseNumberLabel.Text = $"{_currentVerseNumber}절";
            CreateCharLabels(_targetContent);
        }

        private void BuildEntireVerseListUI()
        {
            if (VerseListLayout == null) return;
            VerseListLayout.Children.Clear();

            for (int i = 1; i <= _totalVerses; i++)
            {
                int verseNum = i;
                var dbVerse = BibleService.GetVerseDetail(_selectedBook, _selectedChapter, verseNum);
                string textInList = (dbVerse != null) ? ((BibleService.CurrentLanguage == "EN") ? dbVerse.Content_EN : dbVerse.Content) : $"[{verseNum}절 데이터 없음]";

                var itemBorder = new Border
                {
                    AutomationId = $"VerseBorder_{verseNum}",
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 3),
                    StrokeThickness = 1,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    Stroke = _currentVerseNumber == verseNum ? Color.FromArgb("#512BD4") : Color.FromArgb("#E0E0E0"),
                    BackgroundColor = _currentVerseNumber == verseNum ? Color.FromArgb("#F3E5F5") : Colors.White,
                    HorizontalOptions = LayoutOptions.Fill
                };

                var itemGrid = new Grid
                {
                    RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Auto } },
                    ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star } },
                    RowSpacing = 4,
                    HorizontalOptions = LayoutOptions.Fill
                };

                var numLabel = new Label { Text = $"{verseNum}절", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#512BD4") };
                var contentLabel = new Label { Text = textInList, FontSize = 15, TextColor = Colors.Black, LineBreakMode = LineBreakMode.WordWrap, HorizontalOptions = LayoutOptions.Fill };

                itemGrid.Add(numLabel, 0, 0);
                itemGrid.Add(contentLabel, 0, 1);
                itemBorder.Content = itemGrid;

                var tap = new TapGestureRecognizer();
                tap.Tapped += async (s, e) =>
                {
                    _currentVerseNumber = verseNum;
                    if (WritingEntry != null) WritingEntry.Text = string.Empty;
                    LoadVerseDetail();
                    RefreshListHighlights();
                    if (MainScrollView != null) await MainScrollView.ScrollToAsync(0, 0, true);
                    WritingEntry?.Focus();
                };
                itemBorder.GestureRecognizers.Add(tap);
                VerseListLayout.Children.Add(itemBorder);
            }
        }

        private void RefreshListHighlights()
        {
            if (VerseListLayout == null) return;
            foreach (var child in VerseListLayout.Children)
            {
                if (child is Border border)
                {
                    bool isCurrent = border.AutomationId == $"VerseBorder_{_currentVerseNumber}";
                    border.Stroke = isCurrent ? Color.FromArgb("#512BD4") : Color.FromArgb("#E0E0E0");
                    border.BackgroundColor = isCurrent ? Color.FromArgb("#F3E5F5") : Colors.White;
                }
            }
        }

        private void SaveCurrentPosition() => Preferences.Default.Set("LastVerse", _currentVerseNumber);

        private async Task PrepareBgm()
        {
            try
            {
                if (_bgmPlayer == null && _audioManager != null)
                {
                    var file = await FileSystem.OpenAppPackageFileAsync("bgm_quiet.mp3");
                    _bgmPlayer = _audioManager.CreatePlayer(file);
                    _bgmPlayer.Loop = true;
                    _bgmPlayer.Volume = 0.5;
                }
                if (_isBgmPlaying && _bgmPlayer != null && !_bgmPlayer.IsPlaying) _bgmPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PrepareBgm error: {ex}");
            }
        }

        private void OnSaveBookmarkClicked(object sender, EventArgs e) => SaveToLocalList("MyBookmarks");
        private void OnSaveToMemoryBookClicked(object sender, EventArgs e) => SaveToLocalList("MyMemoryBook");

        private void SaveToLocalList(string key)
        {
            if (string.IsNullOrEmpty(_targetContent)) return;

            try
            {
                var list = JsonSerializer.Deserialize<List<Bookmark>>(Preferences.Default.Get(key, "[]")) ?? new List<Bookmark>();
                list.RemoveAll(x => x.Book == _selectedBook && x.Chapter == _selectedChapter && x.Verse == _currentVerseNumber);
                list.Insert(0, new Bookmark { Book = _selectedBook, Chapter = _selectedChapter, Verse = _currentVerseNumber, Content = _targetContent, Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
                Preferences.Default.Set(key, JsonSerializer.Serialize(list));
                DisplayAlert("알림", "저장되었습니다.", "확인");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveToLocalList error: {ex}");
            }
        }

        private void OnBgmToggleClicked(object sender, EventArgs e)
        {
            if (_bgmPlayer == null) return;
            if (_isBgmPlaying) _bgmPlayer.Pause(); else _bgmPlayer.Play();
            _isBgmPlaying = !_isBgmPlaying;
        }

        private async void OnHomeToolbarClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//MainPage");

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _bgmPlayer?.Pause();
        }
    }
}