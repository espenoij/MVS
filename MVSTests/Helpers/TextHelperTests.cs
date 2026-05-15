using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MVSTests.Helpers
{
    [TestClass]
    public class TextHelperTests
    {
        // ── Wrap ─────────────────────────────────────────────────────────────

        [TestMethod]
        public void Wrap_NullInput_ReturnsEmpty()
        {
            Assert.AreEqual("", TextHelper.Wrap(null));
        }

        [TestMethod]
        public void Wrap_ShortText_ReturnsSingleLineWithTrailingNewline()
        {
            Assert.AreEqual("hello world\n", TextHelper.Wrap("hello world", 80));
        }

        [TestMethod]
        public void Wrap_LongText_BreaksOnWordBoundary()
        {
            string wrapped = TextHelper.Wrap("aaa bbb ccc", 7);
            // Greedy fit: "aaa bbb" then "ccc" — each followed by '\n'.
            Assert.AreEqual("aaa bbb\nccc\n", wrapped);
        }

        // ── EscapeControlChars ───────────────────────────────────────────────

        [TestMethod]
        public void EscapeControlChars_NullInput_ReturnsEmpty()
        {
            Assert.AreEqual("", TextHelper.EscapeControlChars(null));
        }

        [TestMethod]
        public void EscapeControlChars_EscapesCommonWhitespace()
        {
            Assert.AreEqual(@"line\r\ntab\there", TextHelper.EscapeControlChars("line\r\ntab\there"));
        }

        [TestMethod]
        public void EscapeControlChars_EscapesNullAndC0Range()
        {
            // \u0000 is escaped; printable text passes through.
            string escaped = TextHelper.EscapeControlChars("ab\u0000cd");
            Assert.AreEqual(@"ab\u0000cd", escaped);
        }

        // ── EscapeSpace / UnescapeSpace ──────────────────────────────────────

        [TestMethod]
        public void EscapeSpace_RoundTripsWithUnescapeSpace()
        {
            string original = "hello world here";
            string escaped = TextHelper.EscapeSpace(original);
            Assert.IsFalse(escaped.Contains(" "));
            Assert.AreEqual(original, TextHelper.UnescapeSpace(escaped));
        }

        [TestMethod]
        public void EscapeSpace_NullReturnsEmpty()
        {
            Assert.AreEqual("", TextHelper.EscapeSpace(null));
            Assert.AreEqual("", TextHelper.UnescapeSpace(null));
        }

        // ── RemoveLinefeed / RemoveNewLine ───────────────────────────────────

        [TestMethod]
        public void RemoveLinefeed_StripsCrLfPairs()
        {
            Assert.AreEqual("ab", TextHelper.RemoveLinefeed("a\r\nb"));
        }

        [TestMethod]
        public void RemoveNewLine_StripsLfOnly()
        {
            Assert.AreEqual("ab", TextHelper.RemoveNewLine("a\nb"));
            // CR is preserved (only \n is removed).
            Assert.AreEqual("a\rb", TextHelper.RemoveNewLine("a\rb"));
        }

        [TestMethod]
        public void RemoveLinefeed_NullReturnsEmpty()
        {
            Assert.AreEqual("", TextHelper.RemoveLinefeed(null));
            Assert.AreEqual("", TextHelper.RemoveNewLine(null));
        }
    }
}
