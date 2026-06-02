using System;
using System.Linq;
using System.Text.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiblePointApp
{
    public partial class BookmarkPage : ContentPage
    {
        public BookmarkPage()
        {
            InitializeComponent();

            // DI에서 ViewModel을 제공하면 이를 바인딩하고, 아니라면 로컬 로직(Preferences)로 동작하도록 fallback 처리
            try
            {
                var vm = App.Services?.GetService(typeof(ViewModels.BookmarkViewModel)) as ViewModels.BookmarkViewModel;
                if (vm != null) BindingContext = vm;
            }
            catch { /* ignore */ }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is ViewModels.BookmarkViewModel vm)
            {
                if (vm.LoadCommand.CanExecute(null)) vm.LoadCommand.Execute(null);
                return;
            }

            LoadBookmarks();
        }

        private void LoadBookmarks()
        {
            try
            {
                string json = Preferences.Default.Get("MyBookmarks", "[]");
                var rawList = JsonSerializer.Deserialize<List<Bookmark>>(json) ?? new List<Bookmark>();
                var displayList = rawList.OrderByDescending(x => x.Date)
                                         .Select(item => new
                                         {
                                             OriginalItem = item,
                                             DisplayReference = $"{item.Book} {item.Chapter}장 {item.Verse}절",
                                             Content = item.Content,
                                             Date = item.Date
                                         }).ToList();
                BookmarkListView.ItemsSource = displayList;
            }
            catch
            {
                BookmarkListView.ItemsSource = null;
            }
        }

        private async void OnBookmarkSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedObj = e.CurrentSelection.FirstOrDefault();
            if (selectedObj == null) return;

            try
            {
                if (BindingContext is ViewModels.BookmarkViewModel vm)
                {
                    vm.OpenCommand.Execute(selectedObj);
                    return;
                }

                var prop = selectedObj.GetType().GetProperty("OriginalItem");
                if (prop?.GetValue(selectedObj) is Bookmark selected)
                {
                    int totalVerses = BibleService.GetVerseCount(selected.Book, selected.Chapter);
                    if (totalVerses <= 0) totalVerses = 31;
                    string encodedBook = Uri.EscapeDataString(selected.Book);
                    string route = $"{nameof(VerseWritingPage)}?Book={encodedBook}&Chapter={selected.Chapter}&TotalVerses={totalVerses}&StartVerse={selected.Verse}";
                    await Shell.Current.GoToAsync(route);
                }
            }
            catch
            {
                await DisplayAlert("알림", "페이지를 열 수 없습니다.", "확인");
            }
            finally
            {
                if (sender is CollectionView cv) cv.SelectedItem = null;
            }
        }

        private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe)
            {
                var bindingContext = swipe.BindingContext;
                var prop = bindingContext?.GetType().GetProperty("OriginalItem");
                if (prop?.GetValue(bindingContext) is Bookmark item)
                {
                    if (await DisplayAlert("삭제", "이 책갈피를 삭제할까요?", "네", "아니요"))
                    {
                        string json = Preferences.Default.Get("MyBookmarks", "[]");
                        var list = JsonSerializer.Deserialize<List<Bookmark>>(json) ?? new List<Bookmark>();
                        var toRemove = list.FirstOrDefault(m => m.Book == item.Book && m.Chapter == item.Chapter && m.Verse == item.Verse);
                        if (toRemove != null)
                        {
                            list.Remove(toRemove);
                            Preferences.Default.Set("MyBookmarks", JsonSerializer.Serialize(list));
                            if (BindingContext is ViewModels.BookmarkViewModel vm && vm.LoadCommand.CanExecute(null)) await Task.Run(() => vm.LoadCommand.Execute(null));
                            else LoadBookmarks();
                        }
                    }
                }
            }
        }

        private async void OnHomeToolbarClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"///{nameof(MainPage)}");
        }
    }
}