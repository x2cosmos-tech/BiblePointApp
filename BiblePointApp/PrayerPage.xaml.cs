using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using BiblePointApp.Models;
using System;
using System.Threading.Tasks;

namespace BiblePointApp
{
    public partial class PrayerPage : ContentPage
    {
        private readonly ViewModels.PrayerViewModel _vm;

        public PrayerPage() : this(App.Services.GetRequiredService<ViewModels.PrayerViewModel>()) { }

        public PrayerPage(ViewModels.PrayerViewModel vm)
        {
            InitializeComponent();
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            BindingContext = _vm;
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_vm != null) await _vm.LoadPrayers();
        }

        private void OnAddClicked(object sender, EventArgs e)
        {
            var entry = this.FindByName<Entry>("PrayerEntry");
            var text = entry?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text)) return;

            if (BindingContext is ViewModels.PrayerViewModel vm)
            {
                vm.NewPrayerTitle = text;
                if (vm.AddPrayerCommand.CanExecute(null))
                {
                    vm.AddPrayerCommand.Execute(null);
                }
            }

            if (entry != null) entry.Text = string.Empty;
        }

        private void OnCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox cb && cb.BindingContext is Models.PrayerItem item)
            {
                if (BindingContext is ViewModels.PrayerViewModel vm && vm.CompletePrayerCommand.CanExecute(item))
                {
                    vm.CompletePrayerCommand.Execute(item);
                }
            }
        }

        private void OnDeleteClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is Models.PrayerItem item)
            {
                if (BindingContext is ViewModels.PrayerViewModel vm && vm.DeletePrayerCommand.CanExecute(item))
                {
                    vm.DeletePrayerCommand.Execute(item);
                }
            }
        }
    }
}