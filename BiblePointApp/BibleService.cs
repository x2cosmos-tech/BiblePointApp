using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BiblePointApp
{
    public static class BibleService
    {
        private static List<BibleVerse> _allVerses = new List<BibleVerse>();
        private static bool _isLoaded;
        public static IReadOnlyList<BibleVerse> AllVerses => _allVerses;
        public static string CurrentLanguage { get; set; } = "KR";

        private static readonly Dictionary<string, int> BookMaxChapters = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { "창세기", 50 }, { "출애굽기", 40 }, { "레위기", 27 }, { "민수기", 36 }, { "신명기", 34 },
            { "여호수아", 24 }, { "사사기", 21}, { "룻기", 4 }, { "사무엘상", 31}, { "사무엘하", 24 },
            { "열왕기상", 22 }, { "열왕기하", 25 }, { "역대상", 29 }, { "역대하", 36 }, { "에스라", 10 },
            { "느헤미야", 13 }, { "에스더", 10 }, { "욥기", 42 }, { "시편", 150 }, { "잠언", 31},
            { "전도서", 12 }, { "아가", 8 }, { "이사야", 66 }, { "예레미야", 52 }, { "예레미야애가", 5 },
            { "에스겔", 48 }, { "다니엘", 12 }, { "호세아", 14 }, { "요엘", 3 }, { "아모스", 9 },
            { "오바댜", 1}, { "요나", 4 }, { "미가", 7 }, { "나훔", 3 }, { "하박국", 3 },
            { "스바냐", 3 }, { "학개", 2 }, { "스가랴", 14 }, { "말라기", 4 },
            { "마태복음", 28 }, { "마가복음", 16 }, { "누가복음", 24 }, { "요한복음", 21}, { "사도행전", 28 },
            { "로마서", 16 }, { "고린도전서", 16 }, { "고린도후서", 13 }, { "갈라디아서", 6 }, { "에베소서", 6 },
            { "빌립보서", 4 }, { "골로새서", 4 }, { "데살로니가전서", 5 }, { "데살로니가후서", 3 },
            { "디모데전서", 6 }, { "디모데후서", 4 }, { "디도서", 3 }, { "빌레몬서", 1}, { "히브리서", 13 },
            { "야고보서", 5 }, { "베드로전서", 5 }, { "베드로후서", 3 }, { "요한1서", 5 }, { "요한2서", 1},
            { "요한3서", 1}, { "유다서", 1}, { "요한계시록", 22 }, { "계시록", 22 }
        };

        public static int GetMaxChapterCount(string bookName)
        {
            var key = bookName?.Trim() ?? "";
            if (key == "요한계시록") key = "계시록";
            return BookMaxChapters.TryGetValue(key, out var v) ? v : 20;
        }

        public static async Task LoadBibleAsync()
        {
            if (_isLoaded) return;
            var kr = await ReadJsonFileAsync("bible.json") ?? new List<BibleVerse>();
            var en = await ReadJsonFileAsync("bible_en.json") ?? new List<BibleVerse>();

            if (kr.Count == 0 && en.Count == 0) { _allVerses = new List<BibleVerse>(); _isLoaded = true; return; }

            // 영어가 있으면 인덱스 매칭으로 병합(더 안정적)
            if (en.Count > 0)
            {
                int n = Math.Min(kr.Count, en.Count);
                for (int i = 0; i < n; i++) kr[i].Content_EN = string.IsNullOrEmpty(kr[i].Content_EN) ? en[i].Content_EN : kr[i].Content_EN;
                if (en.Count > kr.Count) kr.AddRange(en.Skip(kr.Count));
            }

            _allVerses = kr;
            _isLoaded = true;
        }

        private static async Task<List<BibleVerse>?> ReadJsonFileAsync(string fileName)
        {
            try
            {
                using var s = await FileSystem.OpenAppPackageFileAsync(fileName);
                using var r = new StreamReader(s);
                var json = await r.ReadToEndAsync();
                var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (raw == null) return null;

                var list = new List<BibleVerse>(raw.Count);
                bool isEn = fileName.ToLower().Contains("en");
                foreach (var kv in raw)
                {
                    var key = kv.Key.Trim();
                    int lastSpace = key.LastIndexOf(' ');
                    if (lastSpace <= 0) continue;

                    var book = key.Substring(0, lastSpace);
                    var refPart = key.Substring(lastSpace + 1);
                    var parts = refPart.Split(':');
                    if (parts.Length < 2) continue;
                    if (!int.TryParse(parts[0], out var chap) || !int.TryParse(parts[1], out var verse)) continue;

                    var item = new BibleVerse { Book = book, Chapter = chap, Verse = verse };
                    if (isEn) item.Content_EN = kv.Value;
                    else item.Content = kv.Value;
                    list.Add(item);
                }
                return list;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReadJsonFileAsync({fileName}) failed: {ex.Message}");
                return null;
            }
        }

        public static string GetVerseContent(string book, int chapter, int verse)
        {
            var b = (book ?? "").Trim();
            if (b == "요한계시록") b = "계시록";
            var item = _allVerses.FirstOrDefault(v => string.Equals(v.Book?.Trim(), b, StringComparison.Ordinal) && v.Chapter == chapter && v.Verse == verse);
            if (item == null) return string.Empty;
            return CurrentLanguage == "EN" && !string.IsNullOrEmpty(item.Content_EN) ? item.Content_EN : item.Content ?? string.Empty;
        }

        public static int GetVerseCount(string book, int chapter)
        {
            if (_allVerses == null || _allVerses.Count == 0) return 25;
            var b = (book ?? "").Trim();
            if (b == "요한계시록") b = "계시록";
            int c = _allVerses.Count(v => string.Equals(v.Book?.Trim(), b, StringComparison.Ordinal) && v.Chapter == chapter);
            return c > 0 ? c : 25;
        }

        public static BibleVerse? GetVerseDetail(string book, int chapter, int verse)
        {
            if (_allVerses == null) return null;
            var b = (book ?? "").Trim();
            if (b == "요한계시록") b = "계시록";
            return _allVerses.FirstOrDefault(v => string.Equals(v.Book?.Trim(), b, StringComparison.Ordinal) && v.Chapter == chapter && v.Verse == verse);
        }
    }

    public class BibleVerse
    {
        public string Book { get; set; } = "";
        public int Chapter { get; set; }
        public int Verse { get; set; }
        public string Content { get; set; } = "";
        public string Content_EN { get; set; } = "";
        public string FullReference => $"{Book} {Chapter}:{Verse}";
    }
}