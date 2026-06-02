using System;
using SQLite;

namespace BiblePointApp.Models
{
    // 통일된 PrayerItem 모델 (SQLite용으로 int PK 사용)
    public class PrayerItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        // 작성 시각
        public DateTime StartDate { get; set; }

        // 완료 시각 (미완료이면 null)
        public DateTime? AnswerDate { get; set; }

        // 완료 여부
        public bool IsCompleted { get; set; }
    }
}