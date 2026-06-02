using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Plugin.Maui.Audio;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System;
using BiblePointApp.ViewModels;

namespace BiblePointApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
#pragma warning disable CA1416
                .UseMauiCommunityToolkitMediaElement()
#pragma warning restore CA1416
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // 플랫폼 서비스 등록
            builder.Services.AddSingleton(AudioManager.Current);

            // 데이터베이스 헬퍼(싱글톤)
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "biblepoint.db3");
            builder.Services.AddSingleton<DatabaseHelper>(s => new DatabaseHelper(dbPath));

            // ViewModels
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<ChapterListViewModel>();
            builder.Services.AddTransient<VerseWritingViewModel>();
            builder.Services.AddTransient<BookmarkViewModel>();
            builder.Services.AddTransient<MemoryViewModel>();
            builder.Services.AddTransient<SearchViewModel>();
            builder.Services.AddTransient<PrayerViewModel>(s => new PrayerViewModel(s.GetRequiredService<DatabaseHelper>()));
            builder.Services.AddTransient<MyInfoViewModel>();
            builder.Services.AddTransient<ShopViewModel>();

            // Pages (DI에서 Resolve 가능하도록 등록)
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<BibleListPage>();
            builder.Services.AddTransient<ChapterListPage>();
            builder.Services.AddTransient<VerseWritingPage>();
            builder.Services.AddTransient<BookmarkPage>();
            builder.Services.AddTransient<MemoryPage>();
            builder.Services.AddTransient<SearchPage>();
            builder.Services.AddTransient<PrayerPage>();
            builder.Services.AddTransient<MyInfoPage>();
            builder.Services.AddTransient<ShopPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}