using System.Collections.Generic;

namespace BibleLibre.Sdk
{
    /// <summary>
    /// Represents a single chapter, containing a list of verses.
    /// </summary>
    public class Chapter
    {
        public int Number { get; set; }
        public List<Verse> Verses { get; set; }

        public Chapter()
        {
            Verses = new List<Verse>();
        }
    }
}