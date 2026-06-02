using SQLite;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Storage;
using BiblePointApp.Models;
using System;
using System.Linq;

namespace BiblePointApp
{
    public class WritingProgress
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Book { get; set; } = "";
        public int Chapter { get; set; }
        public int Verse { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class DatabaseHelper
    {
        private SQLiteAsyncConnection? _db;
        private readonly string _dbPath;

        public DatabaseHelper(string dbPath)
        {
            _dbPath = dbPath;
        }

        private async Task Init()
        {
            if (_db != null) return;
            _db = new SQLiteAsyncConnection(_dbPath);
            try
            {
                await _db.CreateTableAsync<WritingProgress>();
                await _db.CreateTableAsync<PrayerItem>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database init error: {ex.Message}");
            }
        }

        /// <summary>
        /// 성경 검색: 로컬 SQLite를 시도하고 실패하면 메모리(BibleService)로 폴백
        /// </summary>
        public async Task<List<BibleVerse>> SearchBibleAsync(string searchtext)
        {
            if (string.IsNullOrWhiteSpace(searchtext)) return new List<BibleVerse>();

            await Init();

            try
            {
                if (_db != null)
                {
                    string q = "SELECT Book, Chapter, Verse, Content, Content_EN FROM BibleTable WHERE Content LIKE ? OR Content_EN LIKE ?";
                    var rows = await _db.QueryAsync<BibleVerse>(q, $"%{searchtext}%", $"%{searchtext}%");
                    if (rows != null && rows.Count > 0) return rows;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SQLite search failed, fallback to in-memory search: {ex.Message}");
            }

            // fallback: BibleService 데이터 검색
            try
            {
                var list = new List<BibleVerse>();
                if (BibleService.AllVerses != null)
                {
                    var lower = searchtext.Trim();
                    foreach (var v in BibleService.AllVerses)
                    {
                        if ((!string.IsNullOrEmpty(v.Content) && v.Content.Contains(lower, StringComparison.OrdinalIgnoreCase))
                            || (!string.IsNullOrEmpty(v.Content_EN) && v.Content_EN.Contains(lower, StringComparison.OrdinalIgnoreCase)))
                        {
                            list.Add(v);
                        }
                    }
                }
                return list;
            }
            catch
            {
                return new List<BibleVerse>();
            }
        }

        // Bookmarks stored in Preferences (legacy)
        public Task<List<Bookmark>> GetBookmarksAsync()
        {
            string json = Preferences.Default.Get("MyBookmarks", "[]");
            return Task.FromResult(JsonSerializer.Deserialize<List<Bookmark>>(json) ?? new List<Bookmark>());
        }

        public async Task SaveProgress(string book, int chapter, int verse)
        {
            await Init();
            try
            {
                if (_db != null)
                {
                    var existing = await _db.Table<WritingProgress>()
                                            .Where(x => x.Book == book && x.Chapter == chapter && x.Verse == verse)
                                            .FirstOrDefaultAsync();
                    if (existing == null)
                    {
                        await _db.InsertAsync(new WritingProgress { Book = book, Chapter = chapter, Verse = verse, IsCompleted = true });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveProgress DB error: {ex.Message}");
            }

            Preferences.Default.Set("LastBook", book);
            Preferences.Default.Set("LastChapter", chapter);
            Preferences.Default.Set("LastVerse", verse);
        }

        public async Task<bool> IsVerseCompleted(string book, int chapter, int verse)
        {
            await Init();
            try
            {
                if (_db != null)
                {
                    var record = await _db.Table<WritingProgress>()
                                          .Where(x => x.Book == book && x.Chapter == chapter && x.Verse == verse)
                                          .FirstOrDefaultAsync();
                    return record != null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsVerseCompleted DB error: {ex.Message}");
            }
            return false;
        }

        // ============================
        //   Prayer methods (single set)
        // ============================
        public async Task<List<PrayerItem>> GetPrayersAsync()
        {
            await Init();
            try
            {
                if (_db != null)
                {
                    var list = await _db.Table<PrayerItem>().ToListAsync();
                    list.Reverse(); // 최근 항목 먼저
                    return list;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPrayersAsync DB error: {ex.Message}");
            }

            // fallback
            return new List<PrayerItem>();
        }

        public async Task<int> SavePrayerAsync(PrayerItem item)
        {
            if (item == null) return 0;
            await Init();

            try
            {
                if (_db != null)
                {
                    // int Id 기준: Id == 0 이면 신규, 아니면 업데이트
                    if (item.Id != 0)
                    {
                        return await _db.UpdateAsync(item);
                    }
                    else
                    {
                        return await _db.InsertAsync(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SavePrayerAsync error: {ex.Message}");
            }

            return 0;
        }

        public async Task<int> DeletePrayerAsync(PrayerItem item)
        {
            if (item == null) return 0;
            await Init();
            try
            {
                if (_db != null) return await _db.DeleteAsync(item);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeletePrayerAsync error: {ex.Message}");
            }
            return 0;
        }
    }
}