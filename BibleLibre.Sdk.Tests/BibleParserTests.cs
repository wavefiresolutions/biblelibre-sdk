using Xunit.Abstractions;

namespace BibleLibre.Sdk.Tests;

public class ParserFormatTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Load_Osis_UsesVernacularWorkTitleForTranslation()
    {
        string filePath = GetFormatPath("eng-kjv.osis.xml");

        Bible bible = BibleParser.Load(filePath);

        Assert.Equal("King James Version", bible.Translation);
    }

    [Theory]
    [InlineData("eng-kjv.osis.xml", 1, 1, 1, "In the beginning")]
    [InlineData("eng-web.usfx.xml", 1, 1, 1, "In the beginning")]
    [InlineData("eng-ylt.zefania.xml", 40, 1, 1, "A roll of the birth")]
    [InlineData("TagalogTLABBible.xml", 1, 1, 1, "Nang pasimula")]
    public void Load_SupportedFormats_ParsesCoreStructureAndVerseText(
        string fileName,
        int bookNumber,
        int chapterNumber,
        int verseNumber,
        string expectedContains)
    {
        string filePath = GetFormatPath(fileName);

        Bible bible = BibleParser.Load(filePath);

        Assert.NotNull(bible);
        Assert.NotEmpty(bible.Testaments);

        Verse? verse = bible.GetVerse(bookNumber, chapterNumber, verseNumber);
        Assert.NotNull(verse);
        _output.WriteLine($"{verse.BookName} {verse.ChapterNumber}:{verse.Number} {verse.Text}");
        Assert.Contains(expectedContains, verse.Text ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_FromFile_ParsesCoreStructureAndVerseText()
    {
        string filePath = GetFormatPath("eng-kjv.osis.xml");

        Bible bible = await BibleParser.LoadAsync(filePath);

        Assert.NotNull(bible);
        Assert.NotEmpty(bible.Testaments);

        Verse? verse = bible.GetVerse(1, 1, 1);
        Assert.NotNull(verse);
        _output.WriteLine($"{verse.BookName} {verse.ChapterNumber}:{verse.Number} {verse.Text}");
        Assert.Contains("In the beginning", verse.Text ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_FromStream_ParsesCoreStructureAndVerseText()
    {
        string filePath = GetFormatPath("eng-kjv.osis.xml");
        await using FileStream fs = File.OpenRead(filePath);

        Bible bible = await BibleParser.LoadAsync(fs);

        Assert.NotNull(bible);
        Verse? verse = bible.GetVerse(1, 1, 1);
        Assert.NotNull(verse);
        Assert.Contains("In the beginning", verse.Text ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadXmlAsync_FromString_ParsesCoreStructureAndVerseText()
    {
        string filePath = GetFormatPath("eng-kjv.osis.xml");
        string xml = File.ReadAllText(filePath);

        Bible bible = await BibleParser.LoadXmlAsync(xml);

        Assert.NotNull(bible);
        Verse? verse = bible.GetVerse(1, 1, 1);
        Assert.NotNull(verse);
        Assert.Contains("In the beginning", verse.Text ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("John 3:16")]
    [InlineData("Jhn. 3:16")]
    [InlineData("John.3.16")]
    public void Get_SupportsStandardAndOsisReferences(string reference)
    {
        Bible bible = BibleParser.Load(GetFormatPath("eng-kjv.osis.xml"));

        List<Verse> verses = bible.Get(reference);

        Assert.Single(verses);
        Verse verse = verses[0];
        Assert.Equal(43, verse.BookNumber);
        Assert.Equal(3, verse.ChapterNumber);
        Assert.Equal(16, verse.Number);
    }

    [Fact]
    public void GetFuzzyReference_SupportsOsisStyleWithMisspelledBook()
    {
        Bible bible = BibleParser.Load(GetFormatPath("eng-kjv.osis.xml"));

        List<Verse> verses = bible.GetFuzzyReference("Jhon.3.16");

        Assert.Single(verses);
        Verse verse = verses[0];
        Assert.Equal(43, verse.BookNumber);
        Assert.Equal(3, verse.ChapterNumber);
        Assert.Equal(16, verse.Number);
    }

    [Fact]
    public void Search_SupportsOsisStyleReferenceInUnifiedSearch()
    {
        Bible bible = BibleParser.Load(GetFormatPath("eng-kjv.osis.xml"));

        List<Verse> verses = bible.Search("Jhn.3.16");

        Assert.Single(verses);
        Verse verse = verses[0];
        Assert.Equal(43, verse.BookNumber);
        Assert.Equal(3, verse.ChapterNumber);
        Assert.Equal(16, verse.Number);
    }

    [Theory]
    [InlineData("eng-kjv.osis.xml")]
    [InlineData("eng-web.usfx.xml")]
    [InlineData("eng-ylt.zefania.xml")]
    [InlineData("TagalogTLABBible.xml")]
    public void Load_SupportedFormats_ExtractsSharedMetadata(string fileName)
    {
        string filePath = GetFormatPath(fileName);

        Bible bible = BibleParser.Load(filePath);
        _output.WriteLine($"{bible.Translation}: {bible.Status}");
        Assert.False(string.IsNullOrWhiteSpace(bible.Translation));
        Assert.False(string.IsNullOrWhiteSpace(bible.Status));
    }

    private static string GetFormatPath(string fileName)
    {
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "BibleLibre.Sdk.Tests",
            "bible-formats",
            fileName));

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Test bible format file was not found: {path}");
        }

        return path;
    }
}