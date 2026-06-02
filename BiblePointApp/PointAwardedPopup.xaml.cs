using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using System;

namespace BiblePointApp
{
    public partial class PointAwardedPopup : Popup
    {
        public PointAwardedPopup(int awardedPoints)
        {
            InitializeComponent();

            if (PointValueLabel != null)
            {
                PointValueLabel.Text = $"+{awardedPoints}";
            }
        }

        private void OnCloseButtonClicked(object sender, EventArgs e)
        {
            try
            {
                Close();
            }
            catch { }
        }
    }
}