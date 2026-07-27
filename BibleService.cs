using BiblePointApp.Models;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BiblePointApp.Services
{
    public static class BibleService
    {
        private static List<BibleVerse> _allVerses = new List<BibleVerse>();
        public static IReadOnlyList<BibleVerse> AllVerses => _allVerses;
        public static string CurrentLanguage { get; set; } = "KR";
        public static bool IsDataLoaded { get; private set; } = false;
        public static string ErrorMessage { get; private set; } = string.Empty;

        // 🎯 검색 인덱스 캐싱 (성능 향상)
        private static Dictionary<string, List<BibleVerse>> _searchCache = new Dictionary<string, List<BibleVerse>>();
        private const int MaxCacheSize = 50;

        private static readonly Dictionary<string, int> BookMaxChapters = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { "창세기", 50 }, { "출애굽기", 40 }, { "레위기", 27 }, { "민수기", 36 }, { "신명기", 34 },
            { "여호수아", 24 }, { "사사기", 21 }, { "룻기", 4 }, { "사무엘상", 31 }, { "사무엘하", 24 },
            { "열왕기상", 22 }, { "열왕기하", 25 }, { "역대상", 29 }, { "역대하", 36 }, { "에스라", 10 },
            { "느헤미야", 13 }, { "에스더", 10 }, { "욥기", 42 }, { "시편", 150 }, { "잠언", 31 },
            { "전도서", 12 }, { "아가", 8 }, { "이사야", 66 }, { "예레미야", 52 }, { "예레미야애가", 5 },
            { "에스겔", 48 }, { "다니엘", 12 }, { "호세아", 14 }, { "요엘", 3 }, { "아모스", 9 },
            { "오바디야", 1 }, { "요나", 4 }, { "미가", 7 }, { "나훔", 3 }, { "하박국", 3 },
            { "스바냐", 3 }, { "학개", 2 }, { "스가랴", 14 }, { "말라기", 4 },
            { "마태복음", 28 }, { "마가복음", 16 }, { "누가복음", 24 }, { "요한복음", 21 }, { "사도행전", 28 },
            { "로마서", 16 }, { "고린도전서", 16 }, { "고린도후서", 13 }, { "갈라디아서", 6 }, { "에베소서", 6 },
            { "빌립보서", 4 }, { "골로새서", 4 }, { "데살로니가전서", 5 }, { "데살로니가후서", 3 }, { "디모데전서", 6 },
            { "디모데후서", 4 }, { "디도서", 3 }, { "빌레몬서", 1 }, { "히브리서", 13 }, { "야고보서", 5 },
            { "베드로전서", 5 }, { "베드로후서", 3 }, { "요한1서", 5 }, { "요한2서", 1 }, { "요한3서", 1 },
            { "유다서", 1 }, { "요한계시록", 22 }
        };

        public static BibleVerse? GetVerseDetail(string book, int chapter, int verse)
            => _allVerses.FirstOrDefault(v => v.Book == book && v.Chapter == chapter && v.Verse == verse);

        public static int GetMaxChapterCount(string bookName)
        {
            var key = bookName?.Trim() ?? "";
            return BookMaxChapters.TryGetValue(key, out var v) ? v : 25;
        }

        public static int GetVerseCount(string book, int chapter)
            => _allVerses.Count(v => v.Book == book && v.Chapter == chapter);

        // 🎯 최적화: 검색 인덱스 캐싱 + 병렬 처리
        public static List<BibleVerse> SearchVerse(string query, bool isEnglishMode = false, int maxResults = 200)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<BibleVerse>();

            var keyword = query.Trim().ToLower();

            // 🎯 캐시 확인
            if (_searchCache.TryGetValue(keyword, out var cachedResults))
            {
                foreach (var verse in cachedResults)
                {
                    verse.IsEnglishVisible = isEnglishMode;
                }
                return cachedResults;
            }

            // 🎯 병렬 검색 (멀티코어 활용)
            var results = _allVerses
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Where(v =>
                    (!string.IsNullOrEmpty(v.Content) && v.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(v.Content_EN) && v.Content_EN.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                )
                .Take(maxResults)
                .ToList();

            // 🎯 캐시에 저장 (최대 50개 유지)
            if (_searchCache.Count >= MaxCacheSize)
            {
                var oldestKey = _searchCache.Keys.First();
                _searchCache.Remove(oldestKey);
            }
            _searchCache[keyword] = results;

            foreach (var verse in results)
            {
                verse.IsEnglishVisible = isEnglishMode;
            }

            return results;
        }

        public static async Task LoadBibleAsync()
        {
            if (IsDataLoaded) return;

            try
            {
                ErrorMessage = string.Empty;

                var kr = await ReadJsonFileAsync("bible.json");
                if (kr == null || kr.Count == 0)
                {
                    if (string.IsNullOrEmpty(ErrorMessage))
                    {
                        ErrorMessage = "bible.json 데이터 파싱 결과가 비어있습니다.";
                    }
                    return;
                }

                var en = await ReadJsonFileAsync("bible_en.json");

                if (en != null && en.Count > 0)
                {
                    var krBooksOrder = kr.Select(v => v.Book).Distinct().ToList();
                    var enBooksOrder = en.Select(v => v.Book).Distinct().ToList();

                    if (krBooksOrder.Count == enBooksOrder.Count)
                    {
                        var enToKrMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        for (int i = 0; i < krBooksOrder.Count; i++)
                        {
                            enToKrMap[enBooksOrder[i]] = krBooksOrder[i];
                        }

                        var enVerseDict = new Dictionary<string, string>();
                        foreach (var e in en)
                        {
                            if (enToKrMap.TryGetValue(e.Book, out var krBook))
                            {
                                string lookupKey = $"{krBook}_{e.Chapter}_{e.Verse}";
                                enVerseDict[lookupKey] = e.Content;
                            }
                        }

                        foreach (var k in kr)
                        {
                            string lookupKey = $"{k.Book}_{k.Chapter}_{k.Verse}";
                            if (enVerseDict.TryGetValue(lookupKey, out var enContent))
                            {
                                k.Content_EN = enContent;
                            }
                        }
                    }
                }

                _allVerses = kr;
                IsDataLoaded = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"치명적 오류 발생: {ex.Message}";
            }
        }

        private static async Task<List<BibleVerse>?> ReadJsonFileAsync(string fileName)
        {
            try
            {
                using var s = await FileSystem.OpenAppPackageFileAsync(fileName);
                using var r = new StreamReader(s);
                var json = await r.ReadToEndAsync();

                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dict == null) return null;

                var list = new List<BibleVerse>();
                foreach (var kvp in dict)
                {
                    string key = kvp.Key.Trim();
                    string content = kvp.Value.Trim();

                    int lastSpaceIndex = key.LastIndexOf(' ');
                    if (lastSpaceIndex == -1) continue;

                    string book = key.Substring(0, lastSpaceIndex).Trim();
                    string rest = key.Substring(lastSpaceIndex + 1).Trim();

                    string[] parts = rest.Split(':');
                    if (parts.Length != 2) continue;

                    if (int.TryParse(parts[0], out int chapter) && int.TryParse(parts[1], out int verse))
                    {
                        list.Add(new BibleVerse
                        {
                            Book = book,
                            Chapter = chapter,
                            Verse = verse,
                            Content = content
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                ErrorMessage += $"[{fileName} 로드/파싱 실패: {ex.Message}]\n";
                return null;
            }
        }

        public static string GetEnglishBookName(string koreanBook)
        {
            return koreanBook switch
            {
                "창세기" => "Genesis",
                "출애굽기" => "Exodus",
                "레위기" => "Leviticus",
                "민수기" => "Numbers",
                "신명기" => "Deuteronomy",
                "여호수아" => "Joshua",
                "사사기" => "Judges",
                "룻기" => "Ruth",
                "사무엘상" => "1 Samuel",
                "사무엘하" => "2 Samuel",
                "열왕기상" => "1 Kings",
                "열왕기하" => "2 Kings",
                "역대상" => "1 Chronicles",
                "역대하" => "2 Chronicles",
                "에스라" => "Ezra",
                "느헤미야" => "Nehemiah",
                "에스더" => "Esther",
                "욥기" => "Job",
                "시편" => "Psalms",
                "잠언" => "Proverbs",
                "전도서" => "Ecclesiastes",
                "아가" => "Song of Solomon",
                "이사야" => "Isaiah",
                "예레미야" => "Jeremiah",
                "예레미야애가" => "Lamentations",
                "에스겔" => "Ezekiel",
                "다니엘" => "Daniel",
                "호세아" => "Hosea",
                "요엘" => "Joel",
                "아모스" => "Amos",
                "오바디야" => "Obadiah",
                "요나" => "Jonah",
                "미가" => "Micah",
                "나훔" => "Nahum",
                "하박국" => "Habakkuk",
                "스바냐" => "Zephaniah",
                "학개" => "Haggai",
                "스가랴" => "Zechariah",
                "말라기" => "Malachi",
                "마태복음" => "Matthew",
                "마가복음" => "Mark",
                "누가복음" => "Luke",
                "요한복음" => "John",
                "사도행전" => "Acts",
                "로마서" => "Romans",
                "고린도전서" => "1 Corinthians",
                "고린도후서" => "2 Corinthians",
                "갈라디아서" => "Galatians",
                "에베소서" => "Ephesians",
                "빌립보서" => "Philippians",
                "골로새서" => "Colossians",
                "데살로니가전서" => "1 Thessalonians",
                "데살로니가후서" => "2 Thessalonians",
                "디모데전서" => "1 Timothy",
                "디모데후서" => "2 Timothy",
                "디도서" => "Titus",
                "빌레몬서" => "Philemon",
                "히브리서" => "Hebrews",
                "야고보서" => "James",
                "베드로전서" => "1 Peter",
                "베드로후서" => "2 Peter",
                "요한1서" => "1 John",
                "요한2서" => "2 John",
                "요한3서" => "3 John",
                "유다서" => "Jude",
                "요한계시록" => "Revelation",
                _ => koreanBook
            };
        }
    }
}
