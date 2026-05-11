namespace BibleLibre.Sdk
{
    /// <summary>
    /// Represents a single verse of the Bible.
    /// </summary>
    public class Verse
    {
        public int Number { get; set; }
        public string? Text { get; set; }
        
        /// <summary>
        /// The book number (e.g., 1 for Genesis, 40 for Matthew).
        /// </summary>
        public int BookNumber { get; set; }
        
        /// <summary>
        /// The book name (e.g., "Genesis", "Matthew").
        /// </summary>
        public string? BookName { get; set; }
        
        /// <summary>
        /// The chapter number.
        /// </summary>
        public int ChapterNumber { get; set; }
    }
}