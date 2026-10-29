using DocumentFormat.OpenXml.Wordprocessing;
using SearchBuddy.Infrastructure.TextExtractors;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Infrastructure.Tests.TextExtractors
{
    public class PlainTextExtractorTests
    {
        private readonly PlainTextExtractor _extractor = new();
        private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
        private readonly string TestFilePath = Path.Combine(AppContext.BaseDirectory, "TextExtractors", "TestFiles", "txt_test.txt");
        private readonly string TextText = "Test tekst txt zażółć gęślą jaźń";

        [Fact]
        public async Task ExtractAsync_FileOpenForWritingByAnotherProcess_StillReadsIt()
        {
            using var heldOpen = new FileStream(TestFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            var text = await _extractor.ExtractAsync(TestFilePath, _ct);

            Assert.False(string.IsNullOrWhiteSpace(text));
        }

        [Fact]
        public async Task ExtractAsync_ConvertsCorrectly()
        {
            var text = await _extractor.ExtractAsync(TestFilePath, _ct);

            Assert.Equal(TextText, text);
        }

        [Fact]
        public async Task ExtractAsync_PreservesPolishCharacters()
        {
            var text = await _extractor.ExtractAsync(TestFilePath, _ct);

            Assert.Contains("zażółć gęślą jaźń", text);
        }

        [Fact]
        public async Task ExtractAsync_PreservesSpaces()
        {
            var text = await _extractor.ExtractAsync(TestFilePath, _ct);
            var splited = text.Split(' ');

            Assert.Equal(6, splited.Length);
        }

        [Fact]
        public async Task ExtractAsync_PreservesWordsOrder()
        {
            var text = await _extractor.ExtractAsync(TestFilePath, _ct);
            var splited = text.Split(' ');

            Assert.Equal("tekst", splited[1]);
            Assert.Equal("zażółć", splited[3]);
            Assert.Equal("jaźń", splited[5]);
        }
    }
}
