using Microsoft.Maui.Storage;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BiblePointApp.ViewModels
{
    public class BookmarkViewModel : BaseViewModel
    {
        public ObservableCollection<object> DisplayBookmarks { get; } = new ObservableCollection<object>();

        public ICommand LoadCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand DeleteCommand { get; }

        public BookmarkViewModel()
        {
            // LoadCommand는 비동기 호출을 래핑
            LoadCommand = new Command(async () => await LoadBookmarksAsync());

            OpenCommand = new Command<object>(async obj =>
            {
                var prop = obj?.GetType().GetProperty("OriginalItem");
                if (prop?.GetValue(obj) is Bookmark b)
                {
                    int totalVerses = BibleService.GetVerseCount(b.Book, b.Chapter);
                    if (totalVerses <= 0) totalVerses = 31;
                    string encodedBook = Uri.EscapeDataString(b.Book);
                    string route = $"{nameof(VerseWritingPage)}?Book={encodedBook}&Chapter={b.Chapter}&TotalVerses={totalVerses}&StartVerse={b.Verse}";
                    await Shell.Current.GoToAsync(route);
                }
            });

            DeleteCommand = new Command<object>(async obj =>
            {
                var prop = obj?.GetType().GetProperty("OriginalItem");
                if (prop?.GetValue(obj) is Bookmark b)
                {
                    if (await Application.Current.MainPage!.DisplayAlert("삭제", "이 책갈피를 삭제할까요?", "네", "아니요"))
                    {
                        string json = Preferences.Default.Get("MyBookmarks", "[]");
                        var list = JsonSerializer.Deserialize<System.Collections.Generic.List<Bookmark>>(json) ?? new System.Collections.Generic.List<Bookmark>();
                        var toRemove = list.FirstOrDefault(m => m.Book == b.Book && m.Chapter == b.Chapter && m.Verse == b.Verse);
                        if (toRemove != null)
                        {
                            list.Remove(toRemove);
                            Preferences.Default.Set("MyBookmarks", JsonSerializer.Serialize(list));
                            await LoadBookmarksAsync();
                        }
                    }
                }
            });
        }

        public void LoadBookmarks()
        {
            try
            {
                string json = Preferences.Default.Get("MyBookmarks", "[]");
                var rawList = JsonSerializer.Deserialize<System.Collections.Generic.List<Bookmark>>(json) ?? new System.Collections.Generic.List<Bookmark>();
                DisplayBookmarks.Clear();
                foreach (var item in rawList.OrderByDescending(x => x.Date))
                {
                    DisplayBookmarks.Add(new { OriginalItem = item, DisplayReference = $"{item.Book} {item.Chapter}장 {item.Verse}절", Content = item.Content, Date = item.Date });
                }
            }
            catch
            {
                DisplayBookmarks.Clear();
            }
        }

        public Task LoadBookmarksAsync()
        {
            return Task.Run(() => LoadBookmarks());
        }
    }
}