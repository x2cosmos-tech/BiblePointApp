using System;
using BiblePointApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace BiblePointApp
{
    public partial class SearchPage : ContentPage
    {
        public SearchPage() : this(App.Services.GetRequiredService<SearchViewModel>()) { }

        public SearchPage(SearchViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
        }

        private void OnSearchResultTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is BibleVerse verse && BindingContext is SearchViewModel vm)
            {
                vm.ResultTappedCommand.Execute(verse);
            }
        }
    }
}