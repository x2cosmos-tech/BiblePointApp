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

        public ICommand AddPrayerCommand { get; }
        public ICommand CompletePrayerCommand { get; }
        public ICommand DeletePrayerCommand { get; }

        public PrayerViewModel(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));

            AddPrayerCommand = new Command(async () =>
            {
                if (string.IsNullOrWhiteSpace(NewPrayerTitle)) return;

                var newItem = new PrayerItem
                {
                    Title = NewPrayerTitle.Trim(),
                    StartDate = DateTime.Now,
                    IsCompleted = false
                };

                await _dbHelper.SavePrayerAsync(newItem);
                NewPrayerTitle = string.Empty;

                await LoadPrayers();
            });

            CompletePrayerCommand = new Command<PrayerItem>(async item =>
            {
                if (item == null) return;

                item.IsCompleted = !item.IsCompleted;
                item.AnswerDate = item.IsCompleted ? DateTime.Now : null;

                await _dbHelper.SavePrayerAsync(item);

                var idx = PrayerList.IndexOf(item);
                if (idx >= 0)
                {
                    PrayerList[idx] = item;
                }
            });

            DeletePrayerCommand = new Command<PrayerItem>(async item =>
            {
                if (item == null) return;
                await _dbHelper.DeletePrayerAsync(item);
                PrayerList.Remove(item);
            });
        }

        public async Task LoadPrayers()
        {
            var prayers = await _dbHelper.GetPrayersAsync();
            PrayerList.Clear();
            foreach (var p in prayers) PrayerList.Add(p);
        }
    }
}