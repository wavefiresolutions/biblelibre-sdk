using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using F23.StringSimilarity;

namespace BibleLibre.Sdk
{
    /// <summary>
    /// Represents the entire Bible collection, containing testaments.
    /// Provides helper methods to query for specific verses, chapters, or books.
    /// </summary>
    public class Bible
    {
        private readonly Cosine _cosine = new();
        private readonly SorensenDice _sorensenDice = new();
        private readonly JaroWinkler _jaroWinkler = new();
        
        /// <summary>
        /// The translation name, e.g., "English KJV".
        /// </summary>
        public string? Translation { get; set; }
        
        /// <summary>
        /// The status of the translation, e.g., "Public Domain".
        /// </summary>
        public string? Status { get; set; }
        
        /// <summary>
        /// A list of testaments (Old and New).
        /// </summary>
        public List<TestamentData> Testaments { get; set; }

        /// <summary>
        /// The localization data for book names and abbreviations.
        /// </summary>
        public Localization Localization { get; set; }

        public Bible()
        {
            Testaments = new List<TestamentData>();
            Localization = new Localization();
        }

        /// <summary>
        /// Performs a fuzzy search over verse text content.
        /// Returns verses whose text similarity to the query meets or exceeds <paramref name="minSimilarity"/>.
        /// Uses F23.StringSimilarity with a phrase-friendly hybrid score and boosts exact/substring matches.
        /// </summary>
        /// <param name="query">Text to search for (short phrases work best).</param>
        /// <param name="minSimilarity">Minimum similarity (0.0-1.0) required to include a verse.</param>
        /// <param name="maxResults">Maximum number of results to return (sorted by similarity desc).</param>
        /// <returns>List of matching verses sorted by descending similarity.</returns>
        public List<Verse> FuzzySearchByText(string query, double minSimilarity = 0.65, int maxResults = 50)
        {
            var results = new List<(Verse verse, double score)>();

            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<Verse>();
            }

            string normQuery = NormalizeText(query);

            // Iterate all verses
            foreach (var testament in Testaments)
            {
                foreach (var book in testament.Books)
                {
                    foreach (var chapter in book.Chapters)
                    {
                        foreach (var verse in chapter.Verses)
                        {
                            string verseText = verse.Text ?? string.Empty;
                            string normVerse = NormalizeText(verseText);

                            double similarity = CalculateSimilarity(normQuery, normVerse);

                            if (similarity >= minSimilarity)
                            {
                                // ensure verse metadata is populated
                                verse.BookNumber = book.Number;
                                verse.BookName = Localization.GetBookName(book.Number);
                                verse.ChapterNumber = chapter.Number;

                                results.Add((verse, similarity));
                            }
                        }
                    }
                }
            }

            var ordered = results
                .OrderByDescending(r => r.score)
                .ThenBy(r => r.verse.BookNumber)
                .ThenBy(r => r.verse.ChapterNumber)
                .ThenBy(r => r.verse.Number)
                .Take(maxResults)
                .Select(r => r.verse)
                .ToList();

            return ordered;
        }

        /// <summary>
        /// Unified search that supports both fuzzy reference lookup and fuzzy verse-text search.
        /// If the query looks like a Bible reference (e.g., "Jhon 3:16"), it tries fuzzy reference parsing first.
        /// If that fails (or if it does not look like a reference), it falls back to fuzzy text search.
        /// </summary>
        /// <param name="query">Reference-like input or free-text phrase.</param>
        /// <param name="minBookSimilarity">Minimum similarity for fuzzy book-name matching.</param>
        /// <param name="minTextSimilarity">Minimum similarity for fuzzy text matching.</param>
        /// <param name="maxResults">Maximum fuzzy text matches to return.</param>
        /// <returns>Matching verses.</returns>
        public List<Verse> Search(
            string query,
            double minBookSimilarity = 0.82,
            double minTextSimilarity = 0.35,
            int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<Verse>();
            }

            if (LooksLikeReference(query))
            {
                var referenceResults = GetFuzzyReference(query, minBookSimilarity);
                if (referenceResults.Count > 0)
                {
                    return referenceResults;
                }
            }

            return FuzzySearchByText(query, minTextSimilarity, maxResults);
        }

        // Normalize text: lowercase, remove punctuation except letters/numbers/space, collapse whitespace
        private static string NormalizeText(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            string lowered = input.ToLowerInvariant();
            // remove punctuation (keep unicode letters and digits and spaces)
            string cleaned = Regex.Replace(lowered, "[^\\p{L}\\p{Nd} ]+", " ");
            // collapse whitespace
            string collapsed = Regex.Replace(cleaned, "\\s+", " ").Trim();
            return collapsed;
        }

        private double CalculateSimilarity(string query, string verse)
        {
            if (string.IsNullOrEmpty(query) && string.IsNullOrEmpty(verse)) return 1.0;
            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(verse)) return 0.0;

            // Phrase queries should strongly match when the normalized verse contains the normalized query.
            if (verse.Contains(query, System.StringComparison.Ordinal))
            {
                return 1.0;
            }

            double cosine = _cosine.Similarity(query, verse);
            double dice = _sorensenDice.Similarity(query, verse);

            // Weighted blend: cosine tends to work well for sentence-level overlap,
            // Sorensen-Dice helps when overlap is partial but meaningful.
            return (cosine * 0.7) + (dice * 0.3);
        }

        private static bool LooksLikeReference(string query)
        {
            // Heuristic: reference-like strings contain chapter:verse shape.
            return Regex.IsMatch(query, @"\d+\s*:\s*\d+");
        }

        /// <summary>
        /// Gets all books in the Bible in order (no nested chapter/verse info).
        /// </summary>
        /// <returns>A list of all books.</returns>
        public List<Book> GetBooks()
        {
            var books = Testaments.SelectMany(t => t.Books).ToList();
            foreach (var book in books)
            {
                book.SetLocalization(Localization);
            }
            return books;
        }

        /// <summary>
        /// Gets all books in a specific testament (no nested chapter/verse info).
        /// </summary>
        /// <param name="testament">The testament (Old or New).</param>
        /// <returns>A list of books in the specified testament.</returns>
        public List<Book> GetBooks(Testament testament)
        {
            var books = Testaments
                .Where(t => t.Testament == testament)
                .SelectMany(t => t.Books)
                .ToList();
            foreach (var book in books)
            {
                book.SetLocalization(Localization);
            }
            return books;
        }

        /// <summary>
        /// Gets all chapters in a specific book (no nested verse info).
        /// </summary>
        /// <param name="book">The book.</param>
        /// <returns>A list of chapters in the book.</returns>
        public List<Chapter> GetChapters(Book book)
        {
            return book.Chapters;
        }

        /// <summary>
        /// Gets all verses in a specific chapter (with verse text).
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <returns>A list of verses in the chapter.</returns>
        public List<Verse> GetVerses(Chapter chapter)
        {
            // We need to find which book this chapter belongs to
            foreach (var testament in Testaments)
            {
                foreach (var book in testament.Books)
                {
                    if (book.Chapters.Contains(chapter))
                    {
                        // Found the book, populate verse metadata
                        foreach (var verse in chapter.Verses)
                        {
                            verse.BookNumber = book.Number;
                            verse.BookName = Localization.GetBookName(book.Number);
                            verse.ChapterNumber = chapter.Number;
                        }
                        return chapter.Verses;
                    }
                }
            }
            
            return chapter.Verses;
        }

        /// <summary>
        /// Gets a specific book by its global number.
        /// </summary>
        /// <param name="bookNumber">The book number (e.g., 1 for Genesis, 40 for Matthew).</param>
        /// <returns>The Book object, or null if not found.</returns>
        public Book? GetBook(int bookNumber)
        {
            // Search all testaments and flatten the list of books
            var book = Testaments.SelectMany(t => t.Books)
                             .FirstOrDefault(b => b.Number == bookNumber);
            if (book != null)
            {
                book.SetLocalization(Localization);
            }
            return book;
        }

        /// <summary>
        /// Gets a specific chapter by its book and chapter number.
        /// </summary>
        /// <param name="bookNumber">The book number.</param>
        /// <param name="chapterNumber">The chapter number.</param>
        /// <returns>The Chapter object, or null if not found.</returns>
        public Chapter? GetChapter(int bookNumber, int chapterNumber)
        {
            Book? book = GetBook(bookNumber);
            return book?.Chapters.FirstOrDefault(c => c.Number == chapterNumber);
        }

        /// <summary>
        /// Gets a specific verse by its book, chapter, and verse number.
        /// </summary>
        /// <param name="bookNumber">The book number.</param>
        /// <param name="chapterNumber">The chapter number.</param>
        /// <param name="verseNumber">The verse number.</param>
        /// <returns>The Verse object, or null if not found.</returns>
        public Verse? GetVerse(int bookNumber, int chapterNumber, int verseNumber)
        {
            Chapter? chapter = GetChapter(bookNumber, chapterNumber);
            Verse? verse = chapter?.Verses.FirstOrDefault(v => v.Number == verseNumber);
            
            if (verse != null)
            {
                verse.BookNumber = bookNumber;
                verse.BookName = Localization.GetBookName(bookNumber);
                verse.ChapterNumber = chapterNumber;
            }
            
            return verse;
        }

        /// <summary>
        /// Gets a specific book by its name.
        /// </summary>
        /// <param name="bookName">The name of the book (e.g., "Genesis", "Matthew").</param>
        /// <returns>The Book object, or null if not found.</returns>
        public Book? GetBook(string bookName)
        {
            int? bookNumber = Localization.GetBookNumber(bookName);
            if (bookNumber.HasValue)
            {
                return GetBook(bookNumber.Value);
            }
            return null;
        }

        /// <summary>
        /// Gets a specific chapter by its book name and chapter number.
        /// </summary>
        /// <param name="bookName">The name of the book.</param>
        /// <param name="chapterNumber">The chapter number.</param>
        /// <returns>The Chapter object, or null if not found.</returns>
        public Chapter? GetChapter(string bookName, int chapterNumber)
        {
            int? bookNumber = Localization.GetBookNumber(bookName);
            if (bookNumber.HasValue)
            {
                return GetChapter(bookNumber.Value, chapterNumber);
            }
            return null;
        }

        /// <summary>
        /// Gets a specific verse by its book name, chapter, and verse number.
        /// </summary>
        /// <param name="bookName">The name of the book (e.g., "Genesis", "Matthew").</param>
        /// <param name="chapterNumber">The chapter number.</param>
        /// <param name="verseNumber">The verse number.</param>
        /// <returns>The Verse object, or null if not found.</returns>
        public Verse? GetVerse(string bookName, int chapterNumber, int verseNumber)
        {
            int? bookNumber = Localization.GetBookNumber(bookName);
            if (bookNumber.HasValue)
            {
                return GetVerse(bookNumber.Value, chapterNumber, verseNumber);
            }
            return null;
        }

        /// <summary>
        /// Gets one or more verses using a quick search string format.
        /// Supports formats like "JN 3:16", "JOHN 3:1-2", "JN 3:1,4,5-6".
        /// </summary>
        /// <param name="reference">The verse reference string (case-insensitive).</param>
        /// <returns>A list of verses matching the reference, or an empty list if not found.</returns>
        public List<Verse> Get(string reference)
        {
            List<Verse> results = new List<Verse>();
            
            if (string.IsNullOrWhiteSpace(reference))
            {
                return results;
            }

            // Parse the reference string
            // Expected format: "BookName Chapter:Verse" or "BookName Chapter:Verse-Verse" or "BookName Chapter:Verse,Verse,Verse-Verse"
            string trimmedRef = reference.Trim();
            
            // Split by space to separate book name from chapter:verse
            int lastSpaceIndex = trimmedRef.LastIndexOf(' ');
            if (lastSpaceIndex < 0)
            {
                return results; // Invalid format
            }

            string bookPart = trimmedRef.Substring(0, lastSpaceIndex).Trim();
            string chapterVersePart = trimmedRef.Substring(lastSpaceIndex + 1).Trim();

            // Get book number
            int? bookNumber = Localization.GetBookNumber(bookPart);
            if (!bookNumber.HasValue)
            {
                return results; // Book not found
            }

            // Get book name for populating the verse
            string? bookName = Localization.GetBookName(bookNumber.Value);

            // Split chapter and verses
            string[] chapterVerseSplit = chapterVersePart.Split(':');
            if (chapterVerseSplit.Length != 2)
            {
                return results; // Invalid format
            }

            if (!int.TryParse(chapterVerseSplit[0], out int chapterNumber))
            {
                return results; // Invalid chapter number
            }

            string versePart = chapterVerseSplit[1];
            
            // Parse verse specifications (can be single, range, or comma-separated)
            string[] verseSpecs = versePart.Split(',');
            
            foreach (string verseSpec in verseSpecs)
            {
                string trimmedSpec = verseSpec.Trim();
                
                if (trimmedSpec.Contains('-'))
                {
                    // Range: "1-5"
                    string[] rangeParts = trimmedSpec.Split('-');
                    if (rangeParts.Length == 2 &&
                        int.TryParse(rangeParts[0].Trim(), out int startVerse) &&
                        int.TryParse(rangeParts[1].Trim(), out int endVerse))
                    {
                        for (int v = startVerse; v <= endVerse; v++)
                        {
                            Verse? verse = GetVerse(bookNumber.Value, chapterNumber, v);
                            if (verse != null)
                            {
                                verse.BookNumber = bookNumber.Value;
                                verse.BookName = bookName;
                                verse.ChapterNumber = chapterNumber;
                                results.Add(verse);
                            }
                        }
                    }
                }
                else
                {
                    // Single verse
                    if (int.TryParse(trimmedSpec, out int verseNumber))
                    {
                        Verse? verse = GetVerse(bookNumber.Value, chapterNumber, verseNumber);
                        if (verse != null)
                        {
                            verse.BookNumber = bookNumber.Value;
                            verse.BookName = bookName;
                            verse.ChapterNumber = chapterNumber;
                            results.Add(verse);
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Gets one or more verses using a quick search string format, with fuzzy book-name fallback.
        /// Chapter and verse parsing remains strict; only the book part is fuzzy-matched when exact lookup fails.
        /// Supports formats like "Jhon 3:16", "Genessis 1:1-3", "Romans 8:28".
        /// </summary>
        /// <param name="reference">The verse reference string.</param>
        /// <param name="minBookSimilarity">Minimum similarity for fuzzy book resolution (0.0-1.0).</param>
        /// <returns>A list of verses matching the reference, or an empty list if not found.</returns>
        public List<Verse> GetFuzzyReference(string reference, double minBookSimilarity = 0.82)
        {
            List<Verse> results = new List<Verse>();

            if (string.IsNullOrWhiteSpace(reference))
            {
                return results;
            }

            string trimmedRef = reference.Trim();
            int lastSpaceIndex = trimmedRef.LastIndexOf(' ');
            if (lastSpaceIndex < 0)
            {
                return results;
            }

            string bookPart = trimmedRef.Substring(0, lastSpaceIndex).Trim();
            string chapterVersePart = trimmedRef.Substring(lastSpaceIndex + 1).Trim();

            int? bookNumber = Localization.GetBookNumber(bookPart);
            if (!bookNumber.HasValue)
            {
                bookNumber = ResolveBookNumberFuzzy(bookPart, minBookSimilarity);
            }

            if (!bookNumber.HasValue)
            {
                return results;
            }

            string? bookName = Localization.GetBookName(bookNumber.Value);

            string[] chapterVerseSplit = chapterVersePart.Split(':');
            if (chapterVerseSplit.Length != 2)
            {
                return results;
            }

            if (!int.TryParse(chapterVerseSplit[0], out int chapterNumber))
            {
                return results;
            }

            string versePart = chapterVerseSplit[1];
            string[] verseSpecs = versePart.Split(',');

            foreach (string verseSpec in verseSpecs)
            {
                string trimmedSpec = verseSpec.Trim();

                if (trimmedSpec.Contains('-'))
                {
                    string[] rangeParts = trimmedSpec.Split('-');
                    if (rangeParts.Length == 2 &&
                        int.TryParse(rangeParts[0].Trim(), out int startVerse) &&
                        int.TryParse(rangeParts[1].Trim(), out int endVerse))
                    {
                        for (int v = startVerse; v <= endVerse; v++)
                        {
                            Verse? verse = GetVerse(bookNumber.Value, chapterNumber, v);
                            if (verse != null)
                            {
                                verse.BookNumber = bookNumber.Value;
                                verse.BookName = bookName;
                                verse.ChapterNumber = chapterNumber;
                                results.Add(verse);
                            }
                        }
                    }
                }
                else
                {
                    if (int.TryParse(trimmedSpec, out int verseNumber))
                    {
                        Verse? verse = GetVerse(bookNumber.Value, chapterNumber, verseNumber);
                        if (verse != null)
                        {
                            verse.BookNumber = bookNumber.Value;
                            verse.BookName = bookName;
                            verse.ChapterNumber = chapterNumber;
                            results.Add(verse);
                        }
                    }
                }
            }

            return results;
        }

        private int? ResolveBookNumberFuzzy(string bookPart, double minBookSimilarity)
        {
            string normalizedInput = NormalizeText(bookPart);
            if (string.IsNullOrWhiteSpace(normalizedInput))
            {
                return null;
            }

            int? bestBookNumber = null;
            double bestScore = 0.0;

            for (int i = 1; i <= 66; i++)
            {
                string? fullName = Localization.GetBookName(i);
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    continue;
                }

                double score = CalculateBookSimilarity(normalizedInput, fullName);

                foreach (string abbreviation in Localization.GetBookAbbreviations(i))
                {
                    double abbrScore = CalculateBookSimilarity(normalizedInput, abbreviation);
                    if (abbrScore > score)
                    {
                        score = abbrScore;
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestBookNumber = i;
                }
            }

            return bestScore >= minBookSimilarity ? bestBookNumber : null;
        }

        private double CalculateBookSimilarity(string normalizedInput, string candidate)
        {
            string normalizedCandidate = NormalizeText(candidate);
            if (string.IsNullOrWhiteSpace(normalizedCandidate))
            {
                return 0.0;
            }

            if (normalizedCandidate == normalizedInput)
            {
                return 1.0;
            }

            return _jaroWinkler.Similarity(normalizedInput, normalizedCandidate);
        }
    }
}