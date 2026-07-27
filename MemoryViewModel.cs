using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using BiblePointApp.Services;
using BiblePointApp.Models;

namespace BiblePointApp.ViewModels
{
    public class MemoryViewModel : BaseViewModel
    {
        // 🎯 BookmarkEntity 사용 (성능 최적화)
        public ObservableCollection<BookmarkEntity> Memories { get; } = new ObservableCollection<BookmarkEntity>();

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoadCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand DeleteCommand { get; }

        public MemoryViewModel()
        {
            LoadCommand = new Command(async () => await LoadAsync());

            // 🎯 BookmarkEntity 매개변수로 수정
            OpenCommand = new Command<BookmarkEntity>(async b =>
            {
                if (b == null) return;
                int totalVerses = BibleService.GetVerseCount(b.Book, b.Chapter);
                if (totalVerses == 0) totalVerses = 25;
                string encodedBook = Uri.EscapeDataString(b.Book);
                string route = $"{nameof(VerseWritingPage)}?Book={encodedBook}&Chapter={b.Chapter}&TotalVerses={totalVerses}&StartVerse={b.Verse}";
                await Shell.Current.GoToAsync(route);
            });

            // 🎯 BookmarkEntity 매개변수로 수정 + 비동기 삭제
            DeleteCommand = new Command<BookmarkEntity>(async b =>
            {
                if (b == null) return;
                if (!await Application.Current.MainPage.DisplayAlert("삭제", "이 암송 구절을 삭제할까요?", "네", "아니요")) return;

                try
                {
                    var database = App.Services.GetRequiredService<BibleDatabase>();
                    await database.DeleteBookmarkAsync(b.Book, b.Chapter, b.Verse, true);
                    
                    await LoadAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"암기장 삭제 오류: {ex.Message}");
                }
            });
        }

        // 🎯 최적화: 비동기 로드 + 백그라운드 처리
        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var memoryList = await Task.Run(async () =>
                {
                    var database = App.Services.GetRequiredService<BibleDatabase>();
                    return await database.GetBookmarksAsync(isMemorizedOnly: true);
                });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Memories.Clear();
                    // 최신 저장된 순서대로 정렬
                    foreach (var m in memoryList.OrderByDescending(x => x.Date))
                    {
                        Memories.Add(m);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"암기장 로드 오류: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(() => Memories.Clear());
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
