using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using BiblePointApp.Models;
using BiblePointApp.Services;

namespace BiblePointApp.ViewModels
{
    public class SearchViewModel : INotifyPropertyChanged
    {
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<BibleVerse> _searchResults = new ObservableCollection<BibleVerse>();
        public ObservableCollection<BibleVerse> SearchResults
        {
            get => _searchResults;
            set
            {
                _searchResults = value;
                OnPropertyChanged();
            }
        }

        private bool _isSearching = false;
        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                _isSearching = value;
                OnPropertyChanged();
            }
        }

        public ICommand SearchCommand { get; }

        public SearchViewModel()
        {
            SearchCommand = new Command(async () => await ExecuteSearchAsync());
        }

        // 🎯 최적화: 비동기 검색 + 스레드 풀
        private async Task ExecuteSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return;

            IsSearching = true;
            SearchResults.Clear();

            try
            {
                // 🎯 백그라운드 스레드에서 검색 수행
                var results = await Task.Run(() =>
                {
                    return BibleService.SearchVerse(SearchText.Trim(), BibleService.CurrentLanguage == "EN", 200);
                });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var item in results)
                    {
                        SearchResults.Add(item);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"검색 오류: {ex.Message}");
            }
            finally
            {
                IsSearching = false;
            }
        }

        // 🎯 언어 변경 시 바인딩 갱신
        public void RefreshSearchResults()
        {
            if (SearchResults == null || SearchResults.Count == 0) return;

            var currentItems = SearchResults.ToList();
            SearchResults.Clear();
            foreach (var item in currentItems)
            {
                SearchResults.Add(item);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
