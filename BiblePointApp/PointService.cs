using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui;
using Microsoft.Maui.Storage;
using System;
using CommunityToolkit.Maui.Views; // <-- 추가

namespace BiblePointApp
{
    public static class PointService
    {
        private static readonly string[] RankNames = {
            "Iron 3", "Iron 2", "Iron 1",
            "Bronze 3", "Bronze 2", "Bronze 1",
            "Silver 3", "Silver 2", "Silver 1",
            "Gold 3", "Gold 2", "Gold 1",
            "Platinum 3", "Platinum 2", "Platinum 1",
            "Diamond 3", "Diamond 2", "Diamond 1"
        };

        private static readonly int[] Thresholds = {
            300,300,300,500,500,500,800,800,800,1200,1200,1200,2000,2000,2000,4000,4000,4000
        };

        private const string KeyPoints = "UserPoints";

        public static int GetCurrentPoints() => Preferences.Default.Get(KeyPoints, 0);

        public static async Task AddPoints(int amount, string reason, ContentPage? page)
        {
            try
            {
                int current = GetCurrentPoints();
                int updated = current + amount;
                if (updated < 0) updated = 0;
                Preferences.Default.Set(KeyPoints, updated);

                if (amount > 0 && page != null)
                {
                    try
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            try
                            {
                                await page.ShowPopupAsync(new PointAwardedPopup(amount));
                            }
                            catch { }
                        });
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PointService.AddPoints error: {ex}");
            }
        }

        public static int GetCurrentLevelIndex()
        {
            int points = GetCurrentPoints();
            int level = 0;
            int i = 0;
            while (i < Thresholds.Length)
            {
                if (points >= Thresholds[i])
                {
                    points -= Thresholds[i];
                    level = i + 1;
                    i++;
                }
                else break;
            }
            if (level < 0) level = 0;
            if (level >= RankNames.Length) level = RankNames.Length - 1;
            return level;
        }

        public static string GetRankName() => RankNames[Math.Clamp(GetCurrentLevelIndex(), 0, RankNames.Length - 1)];

        public static string GetRankImage()
        {
            int idx = Math.Clamp(GetCurrentLevelIndex(), 0, RankNames.Length - 1);
            return $"rank_level{idx}.png";
        }

        public static string GetNextLevelProgress()
        {
            int points = GetCurrentPoints();
            int idx = GetCurrentLevelIndex();
            if (idx >= Thresholds.Length) return "최고 등급 달성!";
            int spent = 0;
            for (int i = 0; i < idx; i++) spent += Thresholds[i];
            int remaining = Thresholds[Math.Min(idx, Thresholds.Length - 1)] - (points - spent);
            if (remaining <= 0) return "다음 등급 준비 완료!";
            return $"다음 등급까지 {remaining}P 남음";
        }
    }
}