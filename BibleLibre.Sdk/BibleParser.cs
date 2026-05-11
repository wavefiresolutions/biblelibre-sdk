using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace BibleLibre.Sdk
{
    /// <summary>
    /// Static class to load a Bible from XML sources (OSIS, USFX, Zefania, or generic xml format).
    /// </summary>
    public static class BibleParser
    {
        private enum XmlFormat
        {
            GenXml,
            Osis,
            Usfx,
            Zefania
        }

        private static readonly Dictionary<string, int> OsisBookNumbers = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Gen", 1 }, { "Exod", 2 }, { "Lev", 3 }, { "Num", 4 }, { "Deut", 5 },
            { "Josh", 6 }, { "Judg", 7 }, { "Ruth", 8 }, { "1Sam", 9 }, { "2Sam", 10 },
            { "1Kgs", 11 }, { "2Kgs", 12 }, { "1Chr", 13 }, { "2Chr", 14 }, { "Ezra", 15 },
            { "Neh", 16 }, { "Esth", 17 }, { "Job", 18 }, { "Ps", 19 }, { "Psa", 19 }, { "Psalm", 19 },
            { "Prov", 20 }, { "Eccl", 21 }, { "Song", 22 }, { "Cant", 22 }, { "Isa", 23 },
            { "Jer", 24 }, { "Lam", 25 }, { "Ezek", 26 }, { "Dan", 27 }, { "Hos", 28 },
            { "Joel", 29 }, { "Amos", 30 }, { "Obad", 31 }, { "Jonah", 32 }, { "Mic", 33 },
            { "Nah", 34 }, { "Hab", 35 }, { "Zeph", 36 }, { "Hag", 37 }, { "Zech", 38 },
            { "Mal", 39 }, { "Matt", 40 }, { "Mark", 41 }, { "Luke", 42 }, { "John", 43 },
            { "Acts", 44 }, { "Rom", 45 }, { "1Cor", 46 }, { "2Cor", 47 }, { "Gal", 48 },
            { "Eph", 49 }, { "Phil", 50 }, { "Col", 51 }, { "1Thess", 52 }, { "1Thes", 52 },
            { "2Thess", 53 }, { "2Thes", 53 }, { "1Tim", 54 }, { "2Tim", 55 }, { "Titus", 56 },
            { "Phlm", 57 }, { "Phm", 57 }, { "Heb", 58 }, { "Jas", 59 }, { "1Pet", 60 },
            { "2Pet", 61 }, { "1John", 62 }, { "2John", 63 }, { "3John", 64 }, { "Jude", 65 },
            { "Rev", 66 }
        };

        private static readonly Dictionary<string, int> UsfxBookNumbers = new(StringComparer.OrdinalIgnoreCase)
        {
            { "GEN", 1 }, { "EXO", 2 }, { "LEV", 3 }, { "NUM", 4 }, { "DEU", 5 },
            { "JOS", 6 }, { "JDG", 7 }, { "RUT", 8 }, { "1SA", 9 }, { "2SA", 10 },
            { "1KI", 11 }, { "2KI", 12 }, { "1CH", 13 }, { "2CH", 14 }, { "EZR", 15 },
            { "NEH", 16 }, { "EST", 17 }, { "JOB", 18 }, { "PSA", 19 }, { "PRO", 20 },
            { "ECC", 21 }, { "SNG", 22 }, { "ISA", 23 }, { "JER", 24 }, { "LAM", 25 },
            { "EZK", 26 }, { "DAN", 27 }, { "HOS", 28 }, { "JOL", 29 }, { "AMO", 30 },
            { "OBA", 31 }, { "JON", 32 }, { "MIC", 33 }, { "NAM", 34 }, { "HAB", 35 },
            { "ZEP", 36 }, { "HAG", 37 }, { "ZEC", 38 }, { "MAL", 39 }, { "MAT", 40 },
            { "MRK", 41 }, { "LUK", 42 }, { "JHN", 43 }, { "ACT", 44 }, { "ROM", 45 },
            { "1CO", 46 }, { "2CO", 47 }, { "GAL", 48 }, { "EPH", 49 }, { "PHP", 50 },
            { "COL", 51 }, { "1TH", 52 }, { "2TH", 53 }, { "1TI", 54 }, { "2TI", 55 },
            { "TIT", 56 }, { "PHM", 57 }, { "HEB", 58 }, { "JAS", 59 }, { "1PE", 60 },
            { "2PE", 61 }, { "1JN", 62 }, { "2JN", 63 }, { "3JN", 64 }, { "JUD", 65 },
            { "REV", 66 }
        };
        /// <summary>
        /// Loads a Bible from an XML, OSIS, or binary file path.
        /// Automatically detects the XML format (OSIS, USFX, Zefania, or generic xml) based on root element.
        /// </summary>
        /// <param name="filePath">The path to an XML Bible file (.xml, .osis.xml, .usfx.xml, .zefania.xml).</param>
        /// <param name="localizationPath">Optional path to a custom localization file.</param>
        /// <returns>A populated Bible object.</returns>
        public static Bible Load(string filePath, string? localizationPath = null)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Bible file not found.", filePath);
            }

            using (FileStream stream = File.OpenRead(filePath))
            {
                return Load(stream, localizationPath);
            }
        }

        /// <summary>
        /// Loads a Bible from an XML or OSIS string.
        /// </summary>
        /// <param name="xmlContent">The string containing the Bible XML or OSIS document.</param>
        /// <param name="localizationPath">Optional path to a custom localization file.</param>
        /// <returns>A populated Bible object.</returns>
        public static Bible LoadXml(string xmlContent, string? localizationPath = null)
        {
            using (StringReader reader = new StringReader(xmlContent))
            {
                XDocument doc = XDocument.Load(reader);
                return Parse(doc, localizationPath);
            }
        }

        /// <summary>
        /// Loads a Bible from a Stream (e.g., FileStream, MemoryStream).
        /// Automatically detects the XML format based on root element.
        /// </summary>
        /// <param name="stream">The stream containing the Bible data.</param>
        /// <param name="localizationPath">Optional path to a custom localization file.</param>
        /// <returns>A populated Bible object.</returns>
        public static Bible Load(Stream stream, string? localizationPath = null)
        {
            // Otherwise, try to parse as XML
            try
            {
                XDocument doc = XDocument.Load(stream);
                return Parse(doc, localizationPath);
            }
            catch (System.Xml.XmlException ex)
            {
                throw new ArgumentException("Failed to parse stream as XML or binary. See inner exception.", ex);
            }
        }


        /// <summary>
        /// Private helper to parse the loaded XDocument into a Bible object.
        /// </summary>
        private static Bible Parse(XDocument doc, string? localizationPath = null)
        {
            XElement? root = doc.Root;
            if (root == null)
            {
                throw new ArgumentException("XML root element is missing.");
            }

            XmlFormat format = DetectXmlFormat(root);
            switch (format)
            {
                case XmlFormat.GenXml:
                    return ParseGenericXml(root, localizationPath);
                case XmlFormat.Osis:
                    return ParseOsis(root, localizationPath);
                case XmlFormat.Usfx:
                    return ParseUsfx(root, localizationPath);
                case XmlFormat.Zefania:
                    return ParseZefania(root, localizationPath);
                default:
                    throw new ArgumentException($"Unsupported XML root element: <{root.Name.LocalName}>.");
            }
        }

        private static Bible ParseGenericXml(XElement root, string? localizationPath = null)
        {
            Bible bible = new Bible
            {
                Translation = GetAttributeValue(root, "translation"),
                Status = CombineMetadata(GetAttributeValue(root, "status"), GetAttributeValue(root, "link"))
            };

            ApplyLocalizationOverride(bible, localizationPath);

            foreach (XElement testamentNode in GetChildElements(root, "testament"))
            {
                string? testamentName = GetAttributeValue(testamentNode, "name");
                Testament testamentType = testamentName?.ToLowerInvariant() == "new" ? Testament.New : Testament.Old;

                TestamentData testament = new TestamentData
                {
                    Testament = testamentType
                };

                foreach (XElement bookNode in GetChildElements(testamentNode, "book"))
                {
                    int.TryParse(GetAttributeValue(bookNode, "number"), out int bookNum);
                    Book book = new Book { Number = bookNum };

                    foreach (XElement chapterNode in GetChildElements(bookNode, "chapter"))
                    {
                        int.TryParse(GetAttributeValue(chapterNode, "number"), out int chapNum);
                        Chapter chapter = new Chapter { Number = chapNum };

                        foreach (XElement verseNode in GetChildElements(chapterNode, "verse"))
                        {
                            int.TryParse(GetAttributeValue(verseNode, "number"), out int verseNum);
                            Verse verse = new Verse
                            {
                                Number = verseNum,
                                Text = verseNode.Value
                            };
                            chapter.Verses.Add(verse);
                        }

                        book.Chapters.Add(chapter);
                    }

                    testament.Books.Add(book);
                }

                bible.Testaments.Add(testament);
            }

            return bible;
        }

        private static Bible ParseOsis(XElement root, string? localizationPath = null)
        {
            XElement? osisText = IsElementNamed(root, "osisText")
                ? root
                : root.Descendants().FirstOrDefault(e => IsElementNamed(e, "osisText"));

            if (osisText == null)
            {
                throw new ArgumentException("OSIS document does not contain an <osisText> element.");
            }

            Bible bible = new Bible
            {
                Translation = GetOsisTranslation(osisText),
                Status = CombineMetadata(GetAttributeValue(osisText, "status"), GetOsisSource(osisText))
            };

            ApplyLocalizationOverride(bible, localizationPath);

            var seenBooks = new HashSet<int>();
            foreach (XElement bookDiv in osisText.Descendants().Where(IsOsisBookContainer))
            {
                int? bookNumber = ResolveOsisBookNumber(GetOsisBookId(bookDiv), bible.Localization);
                if (!bookNumber.HasValue || !seenBooks.Add(bookNumber.Value))
                {
                    continue;
                }

                TestamentData testament = GetOrCreateTestament(bible, bookNumber.Value <= 39 ? Testament.Old : Testament.New);
                Book? book = testament.Books.FirstOrDefault(b => b.Number == bookNumber.Value);
                if (book == null)
                {
                    book = new Book { Number = bookNumber.Value };
                    testament.Books.Add(book);
                }

                ParseOsisBookDiv(bookDiv, book);
            }

            return bible;
        }

        private static Bible ParseUsfx(XElement root, string? localizationPath = null)
        {
            Bible bible = new Bible
            {
                Translation = GetUsfxTranslation(root),
                Status = GetUsfxSource(root)
            };

            ApplyLocalizationOverride(bible, localizationPath);

            foreach (XElement bookNode in GetChildElements(root, "book"))
            {
                int? bookNumber = ResolveUsfxBookNumber(bookNode, bible.Localization);
                if (!bookNumber.HasValue || bookNumber.Value < 1 || bookNumber.Value > 66)
                {
                    continue;
                }

                TestamentData testament = GetOrCreateTestament(bible, bookNumber.Value <= 39 ? Testament.Old : Testament.New);
                Book book = new Book { Number = bookNumber.Value };
                ParseUsfxBook(bookNode, book);
                if (book.Chapters.Count > 0)
                {
                    testament.Books.Add(book);
                }
            }

            return bible;
        }

        private static Bible ParseZefania(XElement root, string? localizationPath = null)
        {
            XElement? information = GetChildElements(root, "INFORMATION").FirstOrDefault();
            string? source = information != null
                ? CombineMetadata(GetChildElementValue(information, "source"), GetChildElementValue(information, "rights"))
                : null;

            Bible bible = new Bible
            {
                Translation = GetChildElementValue(information, "title") ?? GetAttributeValue(root, "biblename"),
                Status = source ?? GetAttributeValue(root, "status")
            };

            ApplyLocalizationOverride(bible, localizationPath);

            foreach (XElement bookNode in GetChildElements(root, "BIBLEBOOK"))
            {
                int? bookNumber = ResolveZefaniaBookNumber(bookNode, bible.Localization);
                if (!bookNumber.HasValue || bookNumber.Value < 1 || bookNumber.Value > 66)
                {
                    continue;
                }

                TestamentData testament = GetOrCreateTestament(bible, bookNumber.Value <= 39 ? Testament.Old : Testament.New);
                Book book = new Book { Number = bookNumber.Value };

                foreach (XElement chapterNode in GetChildElements(bookNode, "CHAPTER"))
                {
                    int chapterNumber = ReadNumberAttribute(chapterNode, "cnumber") ?? 1;
                    Chapter chapter = GetOrCreateChapter(book, chapterNumber);

                    foreach (XElement verseNode in GetChildElements(chapterNode, "VERS"))
                    {
                        int verseNumber = ReadNumberAttribute(verseNode, "vnumber") ?? 1;
                        Verse? verse = chapter.Verses.FirstOrDefault(v => v.Number == verseNumber);
                        if (verse == null)
                        {
                            verse = new Verse { Number = verseNumber };
                            chapter.Verses.Add(verse);
                        }

                        verse.Text = NormalizeOsisText(verseNode.Value);
                    }

                    chapter.Verses = chapter.Verses.OrderBy(v => v.Number).ToList();
                }

                book.Chapters = book.Chapters.OrderBy(c => c.Number).ToList();
                if (book.Chapters.Count > 0)
                {
                    testament.Books.Add(book);
                }
            }

            foreach (TestamentData testament in bible.Testaments)
            {
                testament.Books = testament.Books.OrderBy(b => b.Number).ToList();
            }

            return bible;
        }

        private static void ParseUsfxBook(XElement bookNode, Book book)
        {
            UsfxParseContext context = new UsfxParseContext(book);
            TraverseUsfxNodes(bookNode.Nodes(), context);
            context.FinalizeActiveVerse();
        }

        private static void TraverseUsfxNodes(IEnumerable<XNode> nodes, UsfxParseContext context)
        {
            foreach (XNode node in nodes)
            {
                if (node is XText textNode)
                {
                    context.AppendText(textNode.Value);
                    continue;
                }

                if (node is XCData cdataNode)
                {
                    context.AppendText(cdataNode.Value);
                    continue;
                }

                if (node is not XElement element)
                {
                    continue;
                }

                string localName = element.Name.LocalName;
                if (string.Equals(localName, "c", StringComparison.OrdinalIgnoreCase))
                {
                    context.FinalizeActiveVerse();
                    int? chapter = ReadNumberAttribute(element, "id");
                    if (chapter.HasValue)
                    {
                        context.CurrentChapterNumber = chapter.Value;
                    }
                    continue;
                }

                if (string.Equals(localName, "v", StringComparison.OrdinalIgnoreCase))
                {
                    int verseNumber = ReadNumberAttribute(element, "id") ?? 1;
                    context.StartVerse(context.CurrentChapterNumber > 0 ? context.CurrentChapterNumber : 1, verseNumber);
                    TraverseUsfxNodes(element.Nodes(), context);
                    continue;
                }

                if (string.Equals(localName, "ve", StringComparison.OrdinalIgnoreCase))
                {
                    context.FinalizeActiveVerse();
                    continue;
                }

                // Notes and references are excluded from verse text content.
                if (string.Equals(localName, "f", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(localName, "x", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(localName, "fig", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                TraverseUsfxNodes(element.Nodes(), context);
            }
        }

        private static void ParseOsisBookDiv(XElement bookDiv, Book book)
        {
            OsisParseContext context = new OsisParseContext(book);
            TraverseOsisNodes(bookDiv.Nodes(), context);
            context.FinalizeActiveVerse();
        }

        private static void TraverseOsisNodes(IEnumerable<XNode> nodes, OsisParseContext context)
        {
            foreach (XNode node in nodes)
            {
                if (node is XText textNode)
                {
                    context.AppendText(textNode.Value);
                    continue;
                }

                if (node is XCData cdataNode)
                {
                    context.AppendText(cdataNode.Value);
                    continue;
                }

                if (node is XElement element)
                {
                    HandleOsisElement(element, context);
                }
            }
        }

        private static void HandleOsisElement(XElement element, OsisParseContext context)
        {
            string localName = element.Name.LocalName;

            if (string.Equals(localName, "chapter", StringComparison.OrdinalIgnoreCase))
            {
                int? chapterNumber = ResolveOsisChapterNumber(element, context.CurrentChapterNumber);
                if (chapterNumber.HasValue)
                {
                    context.CurrentChapterNumber = chapterNumber.Value;
                }

                TraverseOsisNodes(element.Nodes(), context);
                return;
            }

            if (string.Equals(localName, "verse", StringComparison.OrdinalIgnoreCase))
            {
                string? sId = GetOsisAttributeValue(element, "sID");
                string? eId = GetOsisAttributeValue(element, "eID");
                string? osisId = NormalizeOsisId(GetOsisAttributeValue(element, "osisID", "osisRef", "id"));

                if (!string.IsNullOrWhiteSpace(eId))
                {
                    context.FinishVerse(eId);
                    TraverseOsisNodes(element.Nodes(), context);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(sId))
                {
                    string verseKey = NormalizeOsisId(sId) ?? sId;
                    int chapterNumber = ResolveVerseChapterNumber(element, context.CurrentChapterNumber);
                    int verseNumber = ResolveOsisVerseNumber(element) ?? 0;
                    context.StartVerse(verseKey, chapterNumber, verseNumber);
                    TraverseOsisNodes(element.Nodes(), context);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(osisId))
                {
                    int chapterNumber = ResolveVerseChapterNumber(element, context.CurrentChapterNumber);
                    int verseNumber = ResolveOsisVerseNumber(element) ?? 0;
                    context.StartVerse(osisId, chapterNumber, verseNumber);
                    TraverseOsisNodes(element.Nodes(), context);
                    context.FinalizeActiveVerse();
                    return;
                }
            }

            TraverseOsisNodes(element.Nodes(), context);
        }

        private static TestamentData GetOrCreateTestament(Bible bible, Testament testament)
        {
            TestamentData? testamentData = bible.Testaments.FirstOrDefault(t => t.Testament == testament);
            if (testamentData == null)
            {
                testamentData = new TestamentData { Testament = testament };
                bible.Testaments.Add(testamentData);
            }

            return testamentData;
        }

        private static string? GetOsisTranslation(XElement osisText)
        {
            string? osisIdWork = GetOsisAttributeValue(osisText, "osisIDWork", "osisWork");

            XElement? work = osisText.Descendants().FirstOrDefault(e =>
                IsElementNamed(e, "work") &&
                string.Equals(GetOsisAttributeValue(e, "osisWork"), osisIdWork, StringComparison.OrdinalIgnoreCase));

            if (work == null)
            {
                work = osisText.Descendants().FirstOrDefault(e =>
                    IsElementNamed(e, "work") &&
                    !string.Equals(GetOsisAttributeValue(e, "osisWork"), "bible", StringComparison.OrdinalIgnoreCase));
            }

            if (work == null)
            {
                work = osisText.Descendants().FirstOrDefault(e => IsElementNamed(e, "work"));
            }

            if (work != null)
            {
                string? title = GetChildElements(work, "title")
                    .Where(e => string.Equals(GetOsisAttributeValue(e, "type"), "x-vernacular", StringComparison.OrdinalIgnoreCase))
                    .Select(e => NormalizeOsisText(e.Value))
                    .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

                if (string.IsNullOrWhiteSpace(title))
                {
                    title = GetChildElements(work, "title")
                        .Select(e => NormalizeOsisText(e.Value))
                        .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
                }

                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }

                string? workId = GetOsisAttributeValue(work, "osisWork", "osisIDWork", "osisID");
                if (!string.IsNullOrWhiteSpace(workId))
                {
                    return workId;
                }
            }

            if (!string.IsNullOrWhiteSpace(osisIdWork))
            {
                return osisIdWork;
            }

            return null;
        }

        private static string? GetOsisSource(XElement osisText)
        {
            XElement? work = osisText.Descendants().FirstOrDefault(e =>
                IsElementNamed(e, "work") &&
                !string.Equals(GetOsisAttributeValue(e, "osisWork"), "bible", StringComparison.OrdinalIgnoreCase));

            if (work == null)
            {
                return null;
            }

            string? rights = GetChildElements(work, "rights")
                .Select(e => NormalizeOsisText(e.Value))
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            string? publisher = GetChildElements(work, "publisher")
                .Select(e => NormalizeOsisText(e.Value))
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            string? identifier = GetChildElements(work, "identifier")
                .Select(e => NormalizeOsisText(e.Value))
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            return CombineMetadata(rights, CombineMetadata(publisher, identifier));
        }

        private static string? GetUsfxTranslation(XElement root)
        {
            XElement? firstScriptureBook = GetChildElements(root, "book")
                .FirstOrDefault(e => ResolveUsfxBookNumber(e, new Localization()).HasValue);

            string? idText = firstScriptureBook?
                .Descendants()
                .FirstOrDefault(e => IsElementNamed(e, "id"))?
                .Value;

            if (!string.IsNullOrWhiteSpace(idText))
            {
                return NormalizeOsisText(idText);
            }

            return null;
        }

        private static string? GetUsfxSource(XElement root)
        {
            string? language = root.Descendants()
                .FirstOrDefault(e => IsElementNamed(e, "languageCode"))?
                .Value;

            if (string.IsNullOrWhiteSpace(language))
            {
                return null;
            }

            return $"language:{NormalizeOsisText(language)}";
        }

        private static int? ResolveOsisBookNumber(string? osisId, Localization localization)
        {
            string? bookCode = ExtractOsisBookCode(osisId);
            if (string.IsNullOrWhiteSpace(bookCode))
            {
                return null;
            }

            if (OsisBookNumbers.TryGetValue(bookCode, out int osisBookNumber))
            {
                return osisBookNumber;
            }

            int? localizedBookNumber = localization.GetBookNumber(bookCode);
            if (localizedBookNumber.HasValue)
            {
                return localizedBookNumber;
            }

            string normalizedBookCode = bookCode.Replace(".", string.Empty).Replace(" ", string.Empty);
            return localization.GetBookNumber(normalizedBookCode);
        }

        private static int? ResolveUsfxBookNumber(XElement bookNode, Localization localization)
        {
            string? code = GetAttributeValue(bookNode, "id");
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            string normalized = code.Trim().Replace(" ", string.Empty).ToUpperInvariant();
            if (UsfxBookNumbers.TryGetValue(normalized, out int bookNumber))
            {
                return bookNumber;
            }

            int? localized = localization.GetBookNumber(normalized);
            if (localized.HasValue)
            {
                return localized;
            }

            return localization.GetBookNumber(code);
        }

        private static int? ResolveZefaniaBookNumber(XElement bookNode, Localization localization)
        {
            int? directNumber = ReadNumberAttribute(bookNode, "bnumber");
            if (directNumber.HasValue)
            {
                return directNumber;
            }

            string? shortName = GetAttributeValue(bookNode, "bsname");
            if (!string.IsNullOrWhiteSpace(shortName))
            {
                int? byShortName = localization.GetBookNumber(shortName);
                if (byShortName.HasValue)
                {
                    return byShortName;
                }
            }

            string? name = GetAttributeValue(bookNode, "bname");
            if (!string.IsNullOrWhiteSpace(name))
            {
                return localization.GetBookNumber(name);
            }

            return null;
        }

        private static int? ResolveOsisChapterNumber(XElement element, int currentChapterNumber)
        {
            string? nValue = GetOsisAttributeValue(element, "n");
            if (TryParseLeadingInt(nValue, out int chapterNumber))
            {
                return chapterNumber;
            }

            string? osisId = NormalizeOsisId(GetOsisAttributeValue(element, "osisID", "osisRef", "id"));
            if (!string.IsNullOrWhiteSpace(osisId))
            {
                string[] parts = osisId.Split('.');
                if (parts.Length >= 2 && TryParseLeadingInt(parts[1], out chapterNumber))
                {
                    return chapterNumber;
                }
            }

            return currentChapterNumber > 0 ? currentChapterNumber : null;
        }

        private static int? ResolveOsisVerseNumber(XElement element)
        {
            string? nValue = GetOsisAttributeValue(element, "n");
            if (TryParseLeadingInt(nValue, out int verseNumber))
            {
                return verseNumber;
            }

            string? osisId = NormalizeOsisId(GetOsisAttributeValue(element, "osisID", "osisRef", "id"));
            if (!string.IsNullOrWhiteSpace(osisId))
            {
                string[] parts = osisId.Split('.');
                if (parts.Length >= 3 && TryParseLeadingInt(parts[2], out verseNumber))
                {
                    return verseNumber;
                }
            }

            return null;
        }

        private static int ResolveVerseChapterNumber(XElement element, int currentChapterNumber)
        {
            string? osisId = NormalizeOsisId(GetOsisAttributeValue(element, "osisID", "osisRef", "id"));
            if (!string.IsNullOrWhiteSpace(osisId))
            {
                string[] parts = osisId.Split('.');
                if (parts.Length >= 2 && TryParseLeadingInt(parts[1], out int chapterNumber))
                {
                    return chapterNumber;
                }
            }

            return currentChapterNumber > 0 ? currentChapterNumber : 1;
        }

        private static string? ExtractOsisBookCode(string? osisId)
        {
            string? normalized = NormalizeOsisId(osisId);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            int dotIndex = normalized.IndexOf('.');
            return dotIndex >= 0 ? normalized.Substring(0, dotIndex) : normalized;
        }

        private static string? NormalizeOsisId(string? osisId)
        {
            if (string.IsNullOrWhiteSpace(osisId))
            {
                return null;
            }

            string trimmed = osisId.Trim();
            int bangIndex = trimmed.IndexOf('!');
            if (bangIndex >= 0)
            {
                trimmed = trimmed.Substring(0, bangIndex);
            }

            return trimmed;
        }

        private static bool TryParseLeadingInt(string? input, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            int length = 0;
            while (length < input.Length && char.IsDigit(input[length]))
            {
                length++;
            }

            if (length == 0)
            {
                return false;
            }

            return int.TryParse(input.Substring(0, length), out value);
        }

        private static int? ReadNumberAttribute(XElement element, params string[] attributes)
        {
            string? value = GetOsisAttributeValue(element, attributes);
            if (TryParseLeadingInt(value, out int number))
            {
                return number;
            }

            return null;
        }

        private static string? GetChildElementValue(XElement? element, string localName)
        {
            if (element == null)
            {
                return null;
            }

            XElement? child = GetChildElements(element, localName).FirstOrDefault();
            if (child == null)
            {
                return null;
            }

            return NormalizeOsisText(child.Value);
        }

        private static string? CombineMetadata(string? primary, string? secondary)
        {
            string? normalizedPrimary = string.IsNullOrWhiteSpace(primary) ? null : NormalizeOsisText(primary);
            string? normalizedSecondary = string.IsNullOrWhiteSpace(secondary) ? null : NormalizeOsisText(secondary);

            if (normalizedPrimary == null)
            {
                return normalizedSecondary;
            }

            if (normalizedSecondary == null || string.Equals(normalizedPrimary, normalizedSecondary, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPrimary;
            }

            return $"{normalizedPrimary} | {normalizedSecondary}";
        }

        private static XmlFormat DetectXmlFormat(XElement root)
        {
            if (IsElementNamed(root, "bible"))
            {
                return XmlFormat.GenXml;
            }

            if (IsElementNamed(root, "osis") || IsElementNamed(root, "osisText") || root.Descendants().Any(e => IsElementNamed(e, "osisText")))
            {
                return XmlFormat.Osis;
            }

            if (IsElementNamed(root, "usfx") || root.Descendants().Any(e => IsElementNamed(e, "book") && GetAttributeValue(e, "id") != null) && root.Descendants().Any(e => IsElementNamed(e, "v")))
            {
                return XmlFormat.Usfx;
            }

            if (IsElementNamed(root, "XMLBIBLE") || root.Descendants().Any(e => IsElementNamed(e, "BIBLEBOOK")) && root.Descendants().Any(e => IsElementNamed(e, "CHAPTER")))
            {
                return XmlFormat.Zefania;
            }

            throw new ArgumentException($"Unsupported XML root element: <{root.Name.LocalName}>.");
        }

        private static void ApplyLocalizationOverride(Bible bible, string? localizationPath)
        {
            if (!string.IsNullOrEmpty(localizationPath) && File.Exists(localizationPath))
            {
                bible.Localization.LoadFromFile(localizationPath);
            }
        }

        private static string NormalizeOsisText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
        }

        private static bool IsElementNamed(XElement element, string localName)
        {
            return string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<XElement> GetChildElements(XElement element, string localName)
        {
            return element.Elements().Where(e => IsElementNamed(e, localName));
        }

        private static string? GetAttributeValue(XElement element, string localName)
        {
            return element.Attributes().FirstOrDefault(a => string.Equals(a.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))?.Value;
        }

        private static string? GetOsisAttributeValue(XElement element, params string[] localNames)
        {
            foreach (string localName in localNames)
            {
                string? value = GetAttributeValue(element, localName);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static bool IsOsisBookContainer(XElement element)
        {
            if (!IsElementNamed(element, "div"))
            {
                return false;
            }

            string? type = GetOsisAttributeValue(element, "type");
            if (string.Equals(type, "book", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "x-book", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string? bookId = GetOsisBookId(element);
            string? bookCode = ExtractOsisBookCode(bookId);
            return !string.IsNullOrWhiteSpace(bookCode) && OsisBookNumbers.ContainsKey(bookCode);
        }

        private static string? GetOsisBookId(XElement element)
        {
            return GetOsisAttributeValue(element, "osisID", "osisRef", "id");
        }

        private static Chapter GetOrCreateChapter(Book book, int chapterNumber)
        {
            Chapter? chapter = book.Chapters.FirstOrDefault(c => c.Number == chapterNumber);
            if (chapter == null)
            {
                chapter = new Chapter { Number = chapterNumber };
                book.Chapters.Add(chapter);
            }

            return chapter;
        }

        private sealed class OsisParseContext
        {
            private readonly Book _book;
            private readonly Dictionary<string, OsisVerseBuffer> _activeVerses = new Dictionary<string, OsisVerseBuffer>(StringComparer.OrdinalIgnoreCase);
            private readonly List<string> _verseOrder = new List<string>();

            public OsisParseContext(Book book)
            {
                _book = book;
            }

            public int CurrentChapterNumber { get; set; }

            public void StartVerse(string verseId, int chapterNumber, int verseNumber)
            {
                FinalizeActiveVerse();

                string normalizedVerseId = NormalizeOsisId(verseId) ?? verseId;
                if (verseNumber <= 0)
                {
                    verseNumber = InferVerseNumber(normalizedVerseId);
                }

                if (chapterNumber <= 0)
                {
                    chapterNumber = InferChapterNumber(normalizedVerseId);
                }

                if (chapterNumber > 0)
                {
                    CurrentChapterNumber = chapterNumber;
                }

                OsisVerseBuffer buffer = new OsisVerseBuffer(chapterNumber, verseNumber);
                _activeVerses[normalizedVerseId] = buffer;
                _verseOrder.Add(normalizedVerseId);
            }

            public void AppendText(string text)
            {
                if (_verseOrder.Count == 0 || string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                OsisVerseBuffer current = _activeVerses[_verseOrder[_verseOrder.Count - 1]];
                current.Text.Append(text);
            }

            public void FinishVerse(string verseId)
            {
                string? normalizedVerseId = NormalizeOsisId(verseId);
                if (string.IsNullOrWhiteSpace(normalizedVerseId))
                {
                    FinalizeActiveVerse();
                    return;
                }

                int index = _verseOrder.FindLastIndex(v => string.Equals(v, normalizedVerseId, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                {
                    FinalizeActiveVerse();
                    return;
                }

                FinalizeVerse(_verseOrder[index]);
            }

            public void FinalizeActiveVerse()
            {
                if (_verseOrder.Count == 0)
                {
                    return;
                }

                FinalizeVerse(_verseOrder[_verseOrder.Count - 1]);
            }

            private void FinalizeVerse(string verseId)
            {
                if (!_activeVerses.TryGetValue(verseId, out OsisVerseBuffer? buffer))
                {
                    return;
                }

                Chapter chapter = GetOrCreateChapter(_book, buffer.ChapterNumber > 0 ? buffer.ChapterNumber : (CurrentChapterNumber > 0 ? CurrentChapterNumber : 1));
                Verse? existingVerse = chapter.Verses.FirstOrDefault(v => v.Number == buffer.VerseNumber);
                if (existingVerse == null)
                {
                    existingVerse = new Verse { Number = buffer.VerseNumber };
                    chapter.Verses.Add(existingVerse);
                }

                existingVerse.Text = NormalizeOsisText(buffer.Text.ToString());

                _activeVerses.Remove(verseId);
                _verseOrder.RemoveAll(v => string.Equals(v, verseId, StringComparison.OrdinalIgnoreCase));
            }

            private static int InferChapterNumber(string verseId)
            {
                string? normalized = NormalizeOsisId(verseId);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    return 1;
                }

                string[] parts = normalized.Split('.');
                if (parts.Length >= 3 && TryParseLeadingInt(parts[1], out int chapterNumber))
                {
                    return chapterNumber;
                }

                return 1;
            }

            private static int InferVerseNumber(string verseId)
            {
                string? normalized = NormalizeOsisId(verseId);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    return 1;
                }

                string[] parts = normalized.Split('.');
                if (parts.Length >= 3 && TryParseLeadingInt(parts[2], out int verseNumber))
                {
                    return verseNumber;
                }

                return 1;
            }
        }

        private sealed class OsisVerseBuffer(int chapterNumber, int verseNumber)
        {
            public int ChapterNumber { get; } = chapterNumber;
            public int VerseNumber { get; } = verseNumber;
            public StringBuilder Text { get; } = new StringBuilder();
        }

        private sealed class UsfxParseContext
        {
            private readonly Book _book;
            private StringBuilder? _activeText;

            public UsfxParseContext(Book book)
            {
                _book = book;
            }

            public int CurrentChapterNumber { get; set; }
            public int CurrentVerseNumber { get; set; }

            public void StartVerse(int chapterNumber, int verseNumber)
            {
                FinalizeActiveVerse();
                CurrentChapterNumber = chapterNumber > 0 ? chapterNumber : 1;
                CurrentVerseNumber = verseNumber > 0 ? verseNumber : 1;
                _activeText = new StringBuilder();
            }

            public void AppendText(string text)
            {
                if (_activeText == null || string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                _activeText.Append(text);
            }

            public void FinalizeActiveVerse()
            {
                if (_activeText == null)
                {
                    return;
                }

                Chapter chapter = GetOrCreateChapter(_book, CurrentChapterNumber > 0 ? CurrentChapterNumber : 1);
                Verse? verse = chapter.Verses.FirstOrDefault(v => v.Number == (CurrentVerseNumber > 0 ? CurrentVerseNumber : 1));
                if (verse == null)
                {
                    verse = new Verse { Number = CurrentVerseNumber > 0 ? CurrentVerseNumber : 1 };
                    chapter.Verses.Add(verse);
                }

                verse.Text = NormalizeOsisText(_activeText.ToString());
                _activeText = null;
            }
        }
    }
}