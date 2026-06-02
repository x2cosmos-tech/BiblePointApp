using System;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using BiblePointApp.ViewModels;
using BiblePointApp.Models;

namespace BiblePointApp
{
    public partial class ShopPage : ContentPage
    {
        private readonly ShopViewModel _vm;

        public ShopPage() : this(App.Services.GetRequiredService<ShopViewModel>()) { }

        public ShopPage(ShopViewModel vm)
        {
            InitializeComponent();
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            BindingContext = _vm;
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // 바인딩으로 UI가 갱신됩니다.
        }

        private async void OnBuyItemClicked(object sender, EventArgs e)
        {
            try
            {
                if (!(sender is Button btn)) return;
                var item = btn.CommandParameter as ShopItem;
                if (item == null) return;

                if (BindingContext is ShopViewModel vm && vm.BuyCommand.CanExecute(item))
                {
                    vm.BuyCommand.Execute(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnBuyItemClicked error: {ex}");
                await DisplayAlert("오류", "구매 중 오류가 발생했습니다.", "확인");
            }
        }
    }
}