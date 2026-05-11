using System.Collections.Generic;

namespace BibleLibre.Sdk
{
    /// <summary>
    /// Represents the testament type (Old or New Testament).
    /// </summary>
    public enum Testament
    {
        Old,
        New
    }

    /// <summary>
    /// Represents testament data, containing a list of books.
    /// </summary>
    public class TestamentData
    {
        public Testament Testament { get; set; }
        public List<Book> Books { get; set; }

        public TestamentData()
        {
            Books = new List<Book>();
        }
    }
}