namespace BiblePointApp
{
    public class Bookmark
    {
        public string Book { get; set; } = string.Empty;
        public int Chapter { get; set; }
        public int Verse { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;

        // 버튼에 글씨 나오게 하는 마법의 줄!
        public string FullReference => $"{Book} {Chapter}:{Verse}";

        // 암기 여부
        public bool IsMemorized { get; set; } = false;
    }
}