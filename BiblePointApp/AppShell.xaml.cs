using Microsoft.Maui.Controls;
using System;
using Microsoft.Maui.Storage;

namespace BiblePointApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // 라우트 등록 (기존 코드 유지)
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(BibleListPage), typeof(BibleListPage));
        Routing.RegisterRoute(nameof(ChapterListPage), typeof(ChapterListPage));
        Routing.RegisterRoute(nameof(VerseWritingPage), typeof(VerseWritingPage));
        Routing.RegisterRoute(nameof(MemoryPage), typeof(MemoryPage));
        Routing.RegisterRoute(nameof(PrayerPage), typeof(PrayerPage));
        Routing.RegisterRoute(nameof(SearchPage), typeof(SearchPage));
        Routing.RegisterRoute(nameof(MyInfoPage), typeof(MyInfoPage));
        Routing.RegisterRoute(nameof(ShopPage), typeof(ShopPage));
        Routing.RegisterRoute(nameof(BookmarkPage), typeof(BookmarkPage));
    }

    // 삼선 메뉴를 닫고 안전하게 이동하는 메서드
    private async void NavigateTo(string route)
    {
        FlyoutIsPresented = false;
        await Shell.Current.GoToAsync(route);
    }

    // 1번 이슈 해결: 홈 버튼 클릭 시 메인페이지 루트로 강제 이동
    private void OnHomeClicked(object sender, EventArgs e)
        => NavigateTo($"//{nameof(MainPage)}");

    // 2번 이슈 해결: 이어하기 로직
    private async void OnWritingPageClicked(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;

        string book = Preferences.Default.Get("LastBook", "창세기");
        int chapter = Preferences.Default.Get("LastChapter", 1);
        int verse = Preferences.Default.Get("LastVerse", 1);

        int maxVerse = BibleService.GetVerseCount(book, chapter);

        string route = $"//{nameof(VerseWritingPage)}?Book={Uri.EscapeDataString(book)}&Chapter={chapter}&TotalVerses={maxVerse}&StartVerse={verse}";
        await Shell.Current.GoToAsync(route);
    }

    // 나머지 페이지 이동
    private void OnPrayerPageClicked(object sender, EventArgs e) => NavigateTo($"//{nameof(PrayerPage)}");
    private void OnMemoryPageClicked(object sender, EventArgs e) => NavigateTo($"//{nameof(MemoryPage)}");
    private void OnBookmarkPageClicked(object sender, EventArgs e) => NavigateTo($"//{nameof(BookmarkPage)}");
    private void OnSearchPageClicked(object sender, EventArgs e) => NavigateTo($"//{nameof(SearchPage)}");
}