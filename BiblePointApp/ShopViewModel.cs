using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using BiblePointApp.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace BiblePointApp.ViewModels
{
    public class ShopViewModel : BaseViewModel
    {
        public ObservableCollection<ShopItem> Items { get; } = new ObservableCollection<ShopItem>();

        private int _currentPoints;
        public int CurrentPoints
        {
            get => _currentPoints;
            private set
            {
                if (SetProperty(ref _currentPoints, value))
                {
                    OnPropertyChanged(nameof(CurrentPointsDisplay));
                }
            }
        }

        public string CurrentPointsDisplay => $"{CurrentPoints:N0} P";

        public ICommand BuyCommand { get; }

        public ShopViewModel()
        {
            CurrentPoints = PointService.GetCurrentPoints();
            LoadItems();

            BuyCommand = new Command<ShopItem>(async item =>
            {
                if (item == null) return;

                int current = PointService.GetCurrentPoints();
                if (current < item.Price)
                {
                    await Application.Current.MainPage.DisplayAlert("포인트 부족", $"필요 포인트: {item.Price - current}P", "확인");
                    return;
                }

                bool confirm = await Application.Current.MainPage.DisplayAlert("구매 확인", $"{item.Name}을(를) 구매하시겠습니까?", "구매", "취소");
                if (!confirm) return;

                await PointService.AddPoints(-item.Price, $"{item.Name} 구매", (ContentPage)Application.Current.MainPage);
                CurrentPoints = PointService.GetCurrentPoints();
                LoadItems();
                await Application.Current.MainPage.DisplayAlert("완료", "구매가 완료되었습니다.", "확인");
            });
        }

        private void LoadItems()
        {
            Items.Clear();
            Items.Add(new ShopItem { Name = "아이템 A", Price = 1000, ImageUrl = "dotnet_bot.png", Description = "설명 A", ButtonColor = Color.FromArgb("#512BD4") });
            Items.Add(new ShopItem { Name = "아이템 B", Price = 3000, ImageUrl = "rank_level9.png", Description = "설명 B", ButtonColor = Color.FromArgb("#28A745") });
            Items.Add(new ShopItem { Name = "아이템 C", Price = 5000, ImageUrl = "app_bg.jpg", Description = "설명 C", ButtonColor = Color.FromArgb("#FF8C00") });
        }
    }
}