using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace BiblePointApp
{
    public partial class MemoryPage : ContentPage
    {
        private bool _isNavigating = false;

        public MemoryPage()
        {
            InitializeComponent();
            try
            {
                if (App.Services?.GetService(typeof(ViewModels.MemoryViewModel)) is ViewModels.MemoryViewModel vm)
                {
                    BindingContext = vm;
                }
            }
            catch { /* ignore */ }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is ViewModels.MemoryViewModel vm)
            {
                vm.LoadCommand.Execute(null);
                return;
            }

            LoadMemoryBook();
        }

        private void LoadMemoryBook()
        {
            try
            {
                string json = Preferences.Default.Get("MyMemoryBook", "[]");
                var memoryList = JsonSerializer.Deserialize<List<Bookmark>>(json) ?? new List<Bookmark>();
                MemoryListView.ItemsSource = memoryList.OrderByDescending(m => m.Date).ToList();
            }
            catch
            {
                MemoryListView.ItemsSource = new List<Bookmark>();
            }
        }

        private async void OnVerseSelected(object sender, SelectionChangedEventArgs e)
        {
            if (_isNavigating) return;

            var selected = e.CurrentSelection.FirstOrDefault() as Bookmark;
            if (selected == null) { if (sender is CollectionView cv) cv.SelectedItem = null; return; }

            try
            {
                _isNavigating = true;

                if (BindingContext is ViewModels.MemoryViewModel vm)
                {
                    vm.OpenCommand.Execute(selected);
                }
                else
                {
                    int totalVerses = BibleService.GetVerseCount(selected.Book, selected.Chapter);
                    if (totalVerses == 0) totalVerses = 25;
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
                _isNavigating = false;
                if (sender is CollectionView cvs) cvs.SelectedItem = null;
            }
        }

        private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is Bookmark item)
            {
                if (await DisplayAlert("삭제", "이 암송 구절을 삭제할까요?", "네", "아니요"))
                {
                    string json = Preferences.Default.Get("MyMemoryBook", "[]");
                    var list = JsonSerializer.Deserialize<List<Bookmark>>(json) ?? new List<Bookmark>();
                    var toRemove = list.FirstOrDefault(m => m.Book == item.Book && m.Chapter == item.Chapter && m.Verse == item.Verse);
                    if (toRemove != null)
                    {
                        list.Remove(toRemove);
                        Preferences.Default.Set("MyMemoryBook", JsonSerializer.Serialize(list));
                        if (BindingContext is ViewModels.MemoryViewModel vm) vm.LoadCommand.Execute(null);
                        else LoadMemoryBook();
                    }
                }
            }
        }

        private async void OnHomeToolbarClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}