using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BiblePointApp
{
    [QueryProperty(nameof(BookName), "BookName")]
    public partial class ChapterListPage : ContentPage
    {
        public string BookName { get; set; } = "";

        private readonly ViewModels.ChapterListViewModel _vm;

        public ChapterListPage() : this(App.Services.GetRequiredService<ViewModels.ChapterListViewModel>()) { }

        public ChapterListPage(ViewModels.ChapterListViewModel vm)
        {
            InitializeComponent();
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            BindingContext = _vm;
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // QueryProperty로 들어온 BookName을 ViewModel에 전달
            try
            {
                if (!string.IsNullOrEmpty(BookName))
                {
                    _vm.InitializeFromRouteParam(BookName);
                }

                RenderChapters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChapterListPage.OnAppearing error: {ex}");
            }
        }

        private void RenderChapters()
        {
            ChapterButtonsLayout.Children.Clear();
            int totalChapters = BibleService.GetMaxChapterCount(_vm.BookName);

            for (int i = 1; i <= totalChapters; i++)
            {
                int chapterNum = i;
                var btn = new Button
                {
                    Text = chapterNum.ToString(),
                    WidthRequest = 55,
                    HeightRequest = 55,
                    Margin = new Thickness(5),
                    CornerRadius = 27,
                    BackgroundColor = Color.FromArgb("#512BD4"),
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold
                };

                btn.Clicked += async (s, e) =>
                {
                    try
                    {
                        int realTotalVerses = BibleService.GetVerseCount(_vm.BookName, chapterNum);
                        if (realTotalVerses == 0) realTotalVerses = 25;
                        string encodedBook = Uri.EscapeDataString(_vm.BookName);
                        string route = $"{nameof(VerseWritingPage)}?Book={encodedBook}&Chapter={chapterNum}&TotalVerses={realTotalVerses}&StartVerse=1";
                        await Shell.Current.GoToAsync(route);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Chapter button click error: {ex}");
                    }
                };

                ChapterButtonsLayout.Children.Add(btn);
            }

            if (BookTitleLabel != null)
            {
                BookTitleLabel.Text = $"{_vm.BookName} 장 선택";
            }
        }
    }
}