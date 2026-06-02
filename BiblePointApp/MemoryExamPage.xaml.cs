using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;

namespace BiblePointApp
{
    public partial class MemoryExamPage : ContentPage
    {
        private readonly MemoryExamViewModel _vm;

        public MemoryExamPage(Bookmark bookmark)
        {
            InitializeComponent();

            _vm = new MemoryExamViewModel(bookmark);
            BindingContext = _vm;

            // ViewModel에서 완료 요청시(포인트 지급/팝업/네비) 처리
            _vm.CompletedRequested += OnVmCompletedRequested;
            _vm.MessageRequested += msg => Device.BeginInvokeOnMainThread(() => DisplayAlert("알림", msg, "확인"));

            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
        }

        // ViewModel이 완료를 요청하면 실제 UI/네비 동작을 수행
        private async Task OnVmCompletedRequested()
        {
            // 중복 처리 방지(이벤트가 여러 번 불리는 경우를 대비)
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                string examKey = _vm.ExamKey;
                if (!Preferences.Default.Get(examKey, false))
                {
                    // PointService.AddPoints에 페이지를 넘겨주고, preference를 저장
                    await PointService.AddPoints(_vm.AwardPoints, $"{_vm.Reference} 암기 성공", this);
                    Preferences.Default.Set(examKey, true);
                    // 팝업 표시
                    await this.ShowPopupAsync(new PointAwardedPopup(_vm.AwardPoints));
                }

                // 짧은 지연 후 뒤로 이동
                await Task.Delay(800);
                await Navigation.PopAsync();
            });
        }

        protected override bool OnBackButtonPressed()
        {
            // 기본 백 버튼 동작을 막고 메인으로 이동
            Shell.Current.GoToAsync($"///{nameof(MainPage)}");
            return true;
        }
    }
}