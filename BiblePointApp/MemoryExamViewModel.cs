using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace BiblePointApp
{
    public class MemoryExamViewModel : INotifyPropertyChanged
    {
        private readonly string _originalAnswer;
        private readonly string _targetNoSpaces;
        private bool _isCompleted;
        private string _inputText = string.Empty;
        private string _displayText = string.Empty;
        private bool _isHintVisible;
        private bool _isExamEntryEnabled = true;

        // 디바운스
        private CancellationTokenSource? _debounceCts;
        private const int DebounceMs = 300;

        public Bookmark Bookmark { get; }

        public MemoryExamViewModel(Bookmark bookmark)
        {
            Bookmark = bookmark ?? throw new ArgumentNullException(nameof(bookmark));
            _originalAnswer = bookmark.Content ?? string.Empty;
            _targetNoSpaces = _originalAnswer.Replace(" ", "");
            Reference = bookmark.FullReference;
            UpdateDisplay(string.Empty);

            ShowHintCommand = new Command(async () => await ShowHintAsync());
            CompleteCommand = new Command(async () => await TryCompleteAsync());
        }

        #region Bindable properties
        public string Reference { get; }

        public string InputText
        {
            get => _inputText;
            set
            {
                if (SetProperty(ref _inputText, value))
                {
                    UpdateDisplay(value);
                    DebounceCheckComplete(value);
                }
            }
        }

        public string DisplayText
        {
            get => _displayText;
            private set => SetProperty(ref _displayText, value);
        }

        public bool IsHintVisible
        {
            get => _isHintVisible;
            private set => SetProperty(ref _isHintVisible, value);
        }

        public bool IsExamEntryEnabled
        {
            get => _isExamEntryEnabled;
            private set => SetProperty(ref _isExamEntryEnabled, value);
        }

        public int AwardPoints => 5;

        // Key를 View가 사용해서 포인트 지급/저장 처리하도록 제공
        public string ExamKey => $"Exam_{Bookmark.Book}_{Bookmark.Chapter}_{Bookmark.Verse}";

        public string OriginalAnswer => _originalAnswer;
        #endregion

        #region Commands
        public ICommand ShowHintCommand { get; }
        public ICommand CompleteCommand { get; }
        #endregion

        #region Events (View가 구독해서 UI/네비/팝업 처리)
        // 뷰는 이 이벤트를 구독하여 포인트 지급/팝업/네비를 처리
        public event Func<Task>? CompletedRequested;

        // 뷰에게 단순 메시지를 보여달라고 요청
        public event Action<string>? MessageRequested;
        #endregion

        #region Core logic
        private void UpdateDisplay(string input)
        {
            if (string.IsNullOrEmpty(_originalAnswer))
            {
                DisplayText = string.Empty;
                return;
            }

            var sb = new StringBuilder();
            string normalized = (input ?? string.Empty).Replace(" ", "");
            int idx = 0;
            foreach (char c in _originalAnswer)
            {
                if (c == ' ')
                {
                    sb.Append("  ");
                }
                else
                {
                    if (idx < normalized.Length)
                    {
                        sb.Append(normalized[idx++]);
                    }
                    else
                    {
                        sb.Append("_");
                    }
                    sb.Append(" ");
                }
            }
            DisplayText = sb.ToString().Trim();
        }

        private void DebounceCheckComplete(string currentInput)
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;
            string normalized = (currentInput ?? string.Empty).Replace(" ", "");

            // Run in threadpool to avoid blocking UI; when complete, raise CompletedRequested (view handles UI)
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(DebounceMs, token);
                }
                catch (TaskCanceledException) { return; }

                if (token.IsCancellationRequested) return;

                if (!_isCompleted && normalized.Equals(_targetNoSpaces, StringComparison.Ordinal))
                {
                    _isCompleted = true;
                    IsExamEntryEnabled = false;
                    // 요청: View에서 포인트 처리 및 네비를 수행
                    if (CompletedRequested != null) await CompletedRequested.Invoke();
                }
            }, token);
        }

        private async Task ShowHintAsync()
        {
            if (IsHintVisible) return;
            IsHintVisible = true;
            await Task.Delay(2000);
            IsHintVisible = false;
        }

        private async Task TryCompleteAsync()
        {
            string normalized = (InputText ?? string.Empty).Replace(" ", "");
            if (!_isCompleted && normalized.Equals(_targetNoSpaces, StringComparison.Ordinal))
            {
                _isCompleted = true;
                IsExamEntryEnabled = false;
                if (CompletedRequested != null) await CompletedRequested.Invoke();
            }
            else
            {
                // View에 메시지를 보여달라 요청
                MessageRequested?.Invoke("아직 구절이 완성되지 않았습니다.");
            }
        }
        #endregion

        #region INotifyPropertyChanged helper
        public event PropertyChangedEventHandler? PropertyChanged;
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
            backingStore = value!;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
        #endregion
    }
}