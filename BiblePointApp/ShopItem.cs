namespace BiblePointApp.Models
{
    public class ShopItem
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Price { get; set; }
        public string ImageUrl { get; set; } = "";
        public string PriceDisplay => $"{Price:N0} P";
        public Microsoft.Maui.Graphics.Color ButtonColor { get; set; } = Microsoft.Maui.Graphics.Colors.Transparent;
    }
}