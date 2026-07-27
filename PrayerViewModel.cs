using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using BiblePointApp.Models;
using Microsoft.Maui.Controls;
using System.Linq;

namespace BiblePointApp.ViewModels
{
    public class PrayerViewModel : BaseViewModel
    {
        private readonly DatabaseHelper _dbHelper;

        public ObservableCollection<PrayerItem> PrayerList { get; } = new ObservableCollection<PrayerItem>();

        private string _newPrayerTitle = string.Empty;
        public string NewPrayerTitle
        {
            get => _newPrayerTitle;
            set => SetProperty(ref _newPrayerTitle, value);
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand AddPrayerCommand { get; }
        public ICommand CompletePrayerCommand { get; }
        public ICommand DeletePrayerCommand { get; }

        public PrayerViewModel(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));

            AddPrayerCommand = new Command(async () => await AddPrayerAsync());
            CompletePrayerCommand = new Command<PrayerItem>(async item => await CompletePrayerAsync(item));
            DeletePrayerCommand = new Command<PrayerItem>(async item => await DeletePrayerAsync(item));
        }

        // 🎯 최적화: 비동기 기도 추가
        private async Task AddPrayerAsync()
        {
            if (string.IsNullOrWhiteSpace(NewPrayerTitle)) return;

            IsLoading = true;
            try
            {
                var newItem = new PrayerItem
                {
                    Title = NewPrayerTitle.Trim(),
                    StartDate = DateTime.Now,
                    IsCompleted = false
                };

                await _dbHelper.SavePrayerAsync(newItem);
                NewPrayerTitle = string.Empty;

                await LoadPrayers();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"기도 추가 오류: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // 🎯 최적화: 비동기 기도 완료 처리
        private async Task CompletePrayerAsync(PrayerItem item)
        {
            if (item == null) return;

            try
            {
                item.IsCompleted = !item.IsCompleted;
                item.AnswerDate = item.IsCompleted ? DateTime.Now : null;

                await _dbHelper.SavePrayerAsync(item);

                var idx = PrayerList.IndexOf(item);
                if (idx >= 0)
                {
                    PrayerList[idx] = item;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"기도 완료 처리 오류: {ex.Message}");
            }
        }

        // 🎯 최적화: 비동기 기도 삭제
        private async Task DeletePrayerAsync(PrayerItem item)
        {
            if (item == null) return;

            try
            {
                await _dbHelper.DeletePrayerAsync(item);
                PrayerList.Remove(item);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"기도 삭제 오류: {ex.Message}");
            }
        }

        // 🎯 최적화: 백그라운드에서 기도 목록 로드
        public async Task LoadPrayers()
        {
            IsLoading = true;
            try
            {
                var prayers = await Task.Run(async () => await _dbHelper.GetPrayersAsync());
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PrayerList.Clear();
                    foreach (var p in prayers.OrderByDescending(x => x.StartDate))
                    {
                        PrayerList.Add(p);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"기도 목록 로드 오류: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
