using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;

namespace BiblePointApp
{
    public partial class BibleListPage : ContentPage
    {
        // 샘플 한글 성경 목록(필요 시 외부 데이터로 바꿔도 됩니다)
        private readonly List<string> _oldTestament = new List<string>
        {
            "창세기","출애굽기","레위기","민수기","신명기","여호수아","사사기","룻기","사무엘상","사무엘하",
            "열왕기상","열왕기하","역대상","역대하","에스라","느헤미야","에스더","욥기","시편","잠언"
        };

        private readonly List<string> _newTestament = new List<string>
        {
            "마태복음","마가복음","누가복음","요한복음","사도행전","로마서","고린도전서","고린도후서","갈라디아서","에베소서"
        };

        public BibleListPage()
        {
            InitializeComponent();
            BuildBookButtons();
        }

        private void BuildBookButtons()
        {
            try
            {
                OldTestamentLayout.Children.Clear();
                NewTestamentLayout.Children.Clear();

                foreach (var b in _oldTestament)
                {
                    var btn = CreateBookButton(b);
                    OldTestamentLayout.Children.Add(btn);
                }

                foreach (var b in _newTestament)
                {
                    var btn = CreateBookButton(b);
                    NewTestamentLayout.Children.Add(btn);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BuildBookButtons error: {ex}");
            }
        }

        private Button CreateBookButton(string bookName)
        {
            var btn = new Button
            {
                Text = bookName,
                WidthRequest = 100,
                HeightRequest = 50,
                Margin = new Thickness(6),
                CornerRadius = 8,
                BackgroundColor = Color.FromArgb("#512BD4"),
                TextColor = Colors.White
            };

            btn.Clicked += async (s, e) =>
            {
                try
                {
                    int totalChapters = BibleService.GetMaxChapterCount(bookName);
                    string encodedBook = Uri.EscapeDataString(bookName);
                    string route = $"{nameof(ChapterListPage)}?BookName={encodedBook}";
                    await Shell.Current.GoToAsync(route);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("오류", "페이지를 열 수 없습니다.", "확인");
                    System.Diagnostics.Debug.WriteLine($"Book button click error: {ex}");
                }
            };

            return btn;
        }
    }
}