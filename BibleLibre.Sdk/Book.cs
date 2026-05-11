using System.Collections.Generic;

namespace BibleLibre.Sdk
{
    /// <summary>
    /// Represents a single book, containing a list of chapters.
    /// </summary>
    public class Book
    {
        private Localization? _localization;
        
        public int Number { get; set; }
        
        /// <summary>
        /// Gets the name of the book based on the localization.
        /// </summary>
        public string? Name => _localization?.GetBookName(Number);
        
        /// <summary>
        /// Gets the abbreviation of the book based on the localization.
        /// </summary>
        public string? Abbreviation => _localization?.GetBookAbbreviation(Number);
        
        public List<Chapter> Chapters { get; set; }

        public Book()
        {
            Chapters = new List<Chapter>();
        }

        /// <summary>
        /// Sets the localization for this book.
        /// </summary>
        internal void SetLocalization(Localization localization)
        {
            _localization = localization;
        }
    }
}