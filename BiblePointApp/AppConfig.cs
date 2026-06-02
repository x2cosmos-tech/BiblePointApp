namespace BiblePointApp;

public static class AppConfig
{
    // "KR" 또는 "EN" 저장
    public static string CurrentLanguage
    {
        get => Preferences.Default.Get("AppLanguage", "KR");
        set => Preferences.Default.Set("AppLanguage", value);
    }

    // 언어에 따른 UI 텍스트 반환 예시
    public static string GetUIText(string kr, string en)
    {
        return CurrentLanguage == "KR" ? kr : en;
    }
}