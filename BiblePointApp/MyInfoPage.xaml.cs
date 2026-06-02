using Microsoft.Extensions.DependencyInjection;
using BiblePointApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;

namespace BiblePointApp
{
    public partial class MyInfoPage : ContentPage
    {
        public MyInfoPage() : this(App.Services.GetRequiredService<MyInfoViewModel>()) { }

        public MyInfoPage(MyInfoViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is MyInfoViewModel vm) vm.Refresh();
        }

        private async void OnEditNameClicked(object sender, EventArgs e)
        {
            if (BindingContext is MyInfoViewModel vm)
            {
                await vm.EditNameAsync();
            }
            else
            {
                string result = await DisplayPromptAsync("이름 변경", "새로운 이름을 입력해주세요.", "저장", "취소", "이름");
                if (!string.IsNullOrWhiteSpace(result))
                {
                    Preferences.Default.Set("UserName", result);
                    if (BindingContext is MyInfoViewModel vm2) vm2.Refresh();
                }
            }
        }

        protected override bool OnBackButtonPressed()
        {
            Shell.Current.GoToAsync($"///{nameof(MainPage)}");
            return true;
        }
    }
}