using Microsoft.Maui.Storage;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace BiblePointApp.ViewModels
{
    public class MemoryViewModel : BaseViewModel
    {
        public ObservableCollection<Bookmark> Memories { get; } = new ObservableCollection<Bookmark>();

        public ICommand LoadCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand DeleteCommand { get; }

        public MemoryViewModel()
        {
            LoadCommand = new Command(Load);
            OpenCommand = new Command<Bookmark>(async b =>
            {
                if (b == null) return;
                int totalVerses = BibleService.GetVerseCount(b.Book, b.Chapter);
                if (totalVerses == 0) totalVerses = 25;
                string encodedBook = Uri.EscapeDataString(b.Book);
                string route = $"{nameof(VerseWritingPage)}?Book={encodedBook}&Chapter={b.Chapter}&TotalVerses={totalVerses}&StartVerse={b.Verse}";
                await Shell.Current.GoToAsync(route);
            });

            DeleteCommand = new Command<Bookmark>(async b =>
            {
                if (b == null) return;
                if (!await Application.Current.MainPage.DisplayAlert("삭제", "이 암송 구절을 삭제할까요?", "네", "아니요")) return;

                string json = Preferences.Default.Get("MyMemoryBook", "[]");
                var list = JsonSerializer.Deserialize<System.Collections.Generic.List<Bookmark>>(json) ?? new System.Collections.Generic.List<Bookmark>();
                var toRemove = list.FirstOrDefault(m => m.Book == b.Book && m.Chapter == b.Chapter && m.Verse == b.Verse);
                if (toRemove != null)
                {
                    list.Remove(toRemove);
                    Preferences.Default.Set("MyMemoryBook", JsonSerializer.Serialize(list));
                    Load();
                }
            });
        }

        public void Load()
        {
            try
            {
                string json = Preferences.Default.Get("MyMemoryBook", "[]");
                var memoryList = JsonSerializer.Deserialize<System.Collections.Generic.List<Bookmark>>(json) ?? new System.Collections.Generic.List<Bookmark>();
                Memories.Clear();
                foreach (var m in memoryList.OrderByDescending(x => x.Date)) Memories.Add(m);
            }
            catch
            {
                Memories.Clear();
            }
        }
    }
}