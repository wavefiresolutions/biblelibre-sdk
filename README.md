# BibleLibre.Sdk

A C# library for parsing and loading Bible data from multiple XML formats.

## Features

- **Multi-Format XML Support**: Load Bible data from OSIS, USFX, Zefania, and Generic XML formats
- **Format Auto-Detection**: Automatically detects XML format (OSIS, USFX, Zefania, or nonstandard) from root element
- **Query Methods**: Easy-to-use methods to query verses, chapters, and books
- **Verse Metadata**: All verses include book name, book number, and chapter number metadata
- **Book Name Support**: Query using book names (e.g., "Genesis", "Matthew") or abbreviations (e.g., "Gen", "Jn") - case-insensitive
- **Multiple Abbreviations**: Each book supports multiple abbreviations (e.g., "John", "Joh", "Jhn", "Jn")
- **Period-Flexible Matching**: Abbreviations work with or without periods (e.g., "Gen" and "Gen." both work)
- **Testament Enum**: Testament type is now an enum (Old/New)
- **Quick Search**: Search for verses using natural reference formats like "JN 3:16" or "JOHN 3:1-5"
- **Fuzzy Reference Search**: Tolerates misspelled book names (e.g., "Jhon 3:16") via `GetFuzzyReference(...)`
- **Fuzzy Text Search**: Search verse content by similarity using `FuzzySearchByText(...)`
- **Unified Search API**: One `Search(...)` method that supports both fuzzy reference and fuzzy text input
- **Editable Localization**: Load custom book names and abbreviations from a text file

## Projects

### BibleLibre.Sdk
The core library that provides Bible parsing and loading functionality.

### ConsoleSample
A sample application demonstrating how to use the library.

## Usage

### Loading a Bible (XML Formats)

```csharp
using BibleLibre.Sdk;

// Load from custom Generic XML
var bible = BibleParser.Load("EnglishKJV.xml");

// Load from OSIS XML - same API
var osisBible = BibleParser.Load("EnglishKJV.osis.xml");

// Load from USFX XML
var usfxBible = BibleParser.Load("EnglishKJV.usfx.xml");

// Load from Zefania XML
var zefaniaBible = BibleParser.Load("EnglishKJV.zefania.xml");

// Load with custom localization file
var bible = BibleParser.Load("EnglishKJV.xml", "localization.txt");

// The format is auto-detected
```

### Querying Verses

```csharp
// Get verse by book number
var verse = bible.GetVerse(1, 1, 1); // Genesis 1:1
Console.WriteLine(verse?.Text);

// Get verse by book name (case-insensitive)
var verse = bible.GetVerse("Genesis", 1, 1);
var verse2 = bible.GetVerse("genesis", 1, 1); // Also works
Console.WriteLine(verse?.Text);

// Get verse by abbreviation (case-insensitive)
var verse3 = bible.GetVerse("Gen", 1, 1);
var verse4 = bible.GetVerse("GEN", 1, 1); // Also works

// Quick search with reference string
var verses = bible.Get("JN 3:16"); // Single verse
var verses2 = bible.Get("JOHN 3:1-2"); // Verse range
var verses3 = bible.Get("JN 3:1,4,5-6"); // Multiple verses and ranges

// Fuzzy reference search (book typo tolerant)
var fuzzyRef = bible.GetFuzzyReference("Jhon 3:16");

// Fuzzy verse content search
var fuzzyText = bible.FuzzySearchByText("for god so loved the world", minSimilarity: 0.35, maxResults: 5);

// Unified search: auto-routes to fuzzy reference or fuzzy text search
var search1 = bible.Search("Jhon 3:16");
var search2 = bible.Search("in the beginning was the word", maxResults: 3);

// Verses returned include book and chapter metadata
foreach (var verse in verses)
{
    Console.WriteLine($"{verse.BookName} {verse.ChapterNumber}:{verse.Number}");
    Console.WriteLine($"Book #{verse.BookNumber}: {verse.Text}");
}

// Get a chapter
var chapter = bible.GetChapter("John", 3);

// Get a book
var book = bible.GetBook("Romans");
```

### Getting Books, Chapters, and Verses

```csharp
// Get all books in the Bible (returns list without nested chapters/verses)
var allBooks = bible.GetBooks();
Console.WriteLine($"Total books: {allBooks.Count}");
foreach (var book in allBooks)
{
    Console.WriteLine($"{book.Number}. {book.Name} ({book.Abbreviation})");
}

// Get books by testament
var oldTestamentBooks = bible.GetBooks(Testament.Old);
var newTestamentBooks = bible.GetBooks(Testament.New);

// Get all chapters in a book (returns list without nested verses)
var genesis = bible.GetBook(1);
var chapters = bible.GetChapters(genesis);

// Get all verses in a chapter (with text)
var genesis1 = bible.GetChapter(1, 1);
var verses = bible.GetVerses(genesis1);
```

### Custom Localization

Localization is managed per Bible instance and can be customized in two ways:

1. **Load a custom localization file when loading the Bible:**
```csharp
// Load Bible with custom localization
var bible = BibleParser.Load("EnglishKJBible.xml", "localization.txt");
```

2. **Load custom localization on an existing Bible instance:**
```csharp
// Load Bible from any supported XML format
var bible = BibleParser.Load("EnglishKJBible.xml");

// Load custom localization
bible.Localization.LoadFromFile("localization.txt");
```

Localization file format (one book per line, with multiple comma-separated abbreviations):
```
# Format: number fullname abbreviation1, abbreviation2, abbreviation3, ...
1 Genesis Gen., Ge., Gn.
2 Exodus Ex., Exod., Exo.
...
43 John John, Joh, Jhn, Jn
...
```

A complete reference file generated from `BibleLibre.Sdk/Localization.cs` is included in the repo:
- `localization.complete.reference.txt`

**Features:**
- Multiple abbreviations per book (e.g., John supports "John", "Joh", "Jhn", "Jn")
- Abbreviations work with or without periods (e.g., "Gen" and "Gen." both work)
- Case-insensitive matching (e.g., "GEN", "Gen", "gen" all work)
- Each Bible instance has its own localization settings

### Fuzzy Search and Unified Search

The library includes three complementary search APIs:

- `GetFuzzyReference(...)`: Fuzzy book-name matching (strict chapter/verse parsing)
- `FuzzySearchByText(...)`: Similarity search over verse text content
- `Search(...)`: Unified method that first tries fuzzy reference parsing for reference-like input (`3:16` pattern), then falls back to fuzzy text search

```csharp
// Fuzzy reference (book typo tolerance)
var byReference = bible.GetFuzzyReference("Jhon 3:16");

// Fuzzy content search
var byText = bible.FuzzySearchByText("for god so loved the world", minSimilarity: 0.35, maxResults: 5);

// Unified search
var smart1 = bible.Search("Jhon 3:16");
var smart2 = bible.Search("for god so loved the world", maxResults: 3);
```


## Building

```bash
dotnet build
```

## Running the Sample

```bash
dotnet run --project ConsoleSample
```

## License

This is released under the MIT license, see the LICENSE.md file for details.