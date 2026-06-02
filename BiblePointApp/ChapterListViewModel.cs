using System;
using System.Collections.Generic;
using System.Linq;

namespace BiblePointApp.ViewModels
{
    public class ChapterListViewModel : BaseViewModel
    {
        private IReadOnlyList<int> _chapters = Array.Empty<int>();
        public IReadOnlyList<int> Chapters
        {
            get => _chapters;
            private set => SetProperty(ref _chapters, value);
        }

        private string _bookName = "창세기";
        public string BookName
        {
            get => _bookName;
            set => SetProperty(ref _bookName, value);
        }

        public ChapterListViewModel()
        {
            // 기본값은 InitializeFromRouteParam에서 세팅
        }

        public void InitializeFromRouteParam(string bookNameParam)
        {
            var inputName = string.IsNullOrWhiteSpace(bookNameParam) ? "창세기" : Uri.UnescapeDataString(bookNameParam).Trim();
            BookName = inputName;

            int totalChapters = BibleService.GetMaxChapterCount(BookName);

            Chapters = Enumerable.Range(1, totalChapters).ToList();
        }
    }
}