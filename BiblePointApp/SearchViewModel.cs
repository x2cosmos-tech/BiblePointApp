using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Linq;

namespace BiblePointApp.ViewModels
{
    public class SearchViewModel : BaseViewModel
    {
        private readonly DatabaseHelper _dbHelper;

        public ICommand SearchCommand { get; }
        public ICommand ResultTappedCommand { get; }

        public ObservableCollection<BibleVerse> SearchResults { get; } = new ObservableCollection<BibleVerse>();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        private bool _isSearching;
        public bool IsSearching
        {
            get => _isSearching;
            set => SetProperty(ref _isSearching, value);
        }

        public SearchViewModel(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
            SearchCommand = new Command(async () => await ExecuteSearchAsync());

            ResultTappedCommand = new Command<BibleVerse>(async verse =>
            {
                if (verse == null) return;
                await Shell.Current.DisplayAlert("선택한 구절", $"{verse.Book} {verse.Chapter}:{verse.Verse}\n{verse.Content}", "확인");
            });
        }

        private async Task ExecuteSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return;

            IsSearching = true;
            SearchResults.Clear();
            try
            {
                var results = await _dbHelper.SearchBibleAsync(SearchText.Trim());
                foreach (var v in results) SearchResults.Add(v);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex}");
            }
            finally
            {
                IsSearching = false;
            }
        }
    }
}