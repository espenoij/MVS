using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using MVS.Models;
using Telerik.Windows.Documents.Fixed.FormatProviders.Pdf;
using Telerik.Windows.Documents.Fixed.Model;
using Telerik.Windows.Documents.Fixed.Model.ColorSpaces;
using Telerik.Windows.Documents.Fixed.Model.Editing;
using Telerik.Windows.Documents.Fixed.Model.Editing.Flow;
using Telerik.Windows.Documents.Fixed.Model.Editing.Tables;
using Telerik.Windows.Documents.Fixed.Model.Fonts;
using Telerik.Windows.Documents.Fixed.Model.Resources;
using TelerikImageSource = Telerik.Windows.Documents.Fixed.Model.Resources.ImageSource;
using TelerikPadding = Telerik.Windows.Documents.Primitives.Padding;

namespace MVS.Services.Reporting
{
    /// <summary>
    /// Builds a comprehensive, business-language verification report and exports
    /// it as a PDF using Telerik RadPdfProcessing (the document-processing
    /// assemblies that ship with the referenced Telerik.UI.for.Wpf.80 package).
    ///
    /// The report deliberately avoids engineering jargon: it leads with a plain
    /// summary and the final corrections, then shows charts, then the supporting
    /// per-axis tables and a glossary for anyone who wants the detail.
    /// </summary>
    public static class VerificationPdfReportExporter
    {
        private static readonly CultureInfo Ci = CultureInfo.CurrentCulture;

        private static readonly FontBase _robotoRegular;
        private static readonly FontBase _robotoBold;

        // SES Energy doc-template images (extracted from Doc-template - SES Energy.docx).
        // All three are drawn on every page by AddPageFooters in back-to-front order:
        //   1. ses-background.png — full-page semi-transparent watermark (behind content)
        //   2. ses-header.png     — teal band at the top of every page
        //   3. ses-footer.png     — teal band at the bottom + page number
        private static readonly byte[]? _sesBackgroundPng;
        private static readonly byte[]? _sesHeaderPng;
        private static readonly byte[]? _sesFooterPng;

        static VerificationPdfReportExporter()
        {
            // Use a plain family name so that FontProperties keys match between
            // RegisterFont and TryCreateFont. The TTF bytes are loaded directly
            // from the embedded WPF resources rather than relying on WPF's
            // GlyphTypeface resolver, which does not follow pack URIs.
            var family = new FontFamily("Roboto Condensed");

            RegisterRobotoFont(family, FontStyles.Normal, FontWeights.Normal,
                "pack://application:,,,/MVS;component/Fonts/RobotoCondensed-Regular.ttf");

            RegisterRobotoFont(family, FontStyles.Normal, FontWeights.Bold,
                "pack://application:,,,/MVS;component/Fonts/RobotoCondensed-Bold.ttf");

            _robotoRegular = FontsRepository.TryCreateFont(
                family, FontStyles.Normal, FontWeights.Normal, out FontBase regular)
                ? regular : FontsRepository.Helvetica;

            _robotoBold = FontsRepository.TryCreateFont(
                family, FontStyles.Normal, FontWeights.Bold, out FontBase bold)
                ? bold : FontsRepository.HelveticaBold;

            _sesBackgroundPng = LoadResourceBytes(
                "pack://application:,,,/MVS;component/Resources/DocTemplate/ses-background.png");
            _sesHeaderPng = LoadResourceBytes(
                "pack://application:,,,/MVS;component/Resources/DocTemplate/ses-header.png");
            _sesFooterPng = LoadResourceBytes(
                "pack://application:,,,/MVS;component/Resources/DocTemplate/ses-footer.png");
        }

        /// <summary>
        /// Loads a WPF pack-URI resource as raw bytes.
        /// Returns <c>null</c> when the resource is unavailable (e.g. test host).
        /// </summary>
        private static byte[]? LoadResourceBytes(string packUri)
        {
            try
            {
                var info = Application.GetResourceStream(new Uri(packUri));
                if (info == null) return null;
                using var ms = new MemoryStream();
                info.Stream.CopyTo(ms);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Reads the TTF at <paramref name="packUri"/> from the application's
        /// embedded resources and pre-registers it with RadPdfProcessing so that
        /// <see cref="FontsRepository.TryCreateFont"/> can retrieve it by name
        /// without needing WPF's GlyphTypeface resolver.
        /// </summary>
        private static void RegisterRobotoFont(
            FontFamily family, FontStyle style, FontWeight weight, string packUri)
        {
            try
            {
                var uri = new Uri(packUri);
                System.Windows.Resources.StreamResourceInfo info =
                    Application.GetResourceStream(uri);
                if (info == null) return;

                using var ms = new MemoryStream();
                info.Stream.CopyTo(ms);
                FontsRepository.RegisterFont(family, style, weight, ms.ToArray());
            }
            catch
            {
                // Fall back to Helvetica if resources are unavailable (e.g. test host).
            }
        }

        // ── Palette (design-spec: deep navy / teal / seafoam / amber / red) ──
        // Table cells: background is controllable; text color/size is not via the
        // RadPdfProcessing flow API. Headers therefore use a medium-blue tint that
        // remains readable with the dark default text color.
        // SES Energy Brand Toolkit palette (PDF / RgbColor version)
        private static readonly RgbColor ColorHeading     = new RgbColor(0x33, 0x4A, 0x5C); // SES Dark Blue Grey
        private static readonly RgbColor ColorSubHeading  = new RgbColor(0x26, 0x37, 0x46); // SES Dark Blue Grey deep
        private static readonly RgbColor ColorAccent      = new RgbColor(0x3D, 0xE6, 0xA9); // SES Energy Green
        private static readonly RgbColor ColorText        = new RgbColor(0x1A, 0x27, 0x32); // Near-black text
        private static readonly RgbColor ColorMuted       = new RgbColor(0x7F, 0x94, 0xA5); // Mid-tone grey
        private static readonly RgbColor ColorTableHeader = new RgbColor(0x33, 0x4A, 0x5C); // SES Dark Blue Grey (table headers)
        private static readonly RgbColor ColorTableHeaderText = new RgbColor(0xFF, 0xFF, 0xFF); // White text on dark header
        private static readonly RgbColor ColorRowAlt      = new RgbColor(0xF2, 0xF5, 0xF7); // Light surface alt row
        private static readonly RgbColor ColorBorder      = new RgbColor(0xD6, 0xDF, 0xE6); // Subtle divider

        // Capture-duration quality thresholds (minutes) — matches DurationStatusBanner.
        private const double MinDurationAcceptableMinutes  = 20.0;
        private const double MinDurationRecommendedMinutes = 40.0;

        /// <summary>Exports the report to a file on disk.</summary>
        public static void Export(string path, VerificationReportModel model)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
            if (model == null) throw new ArgumentNullException(nameof(model));

            using (FileStream fs = File.Create(path))
            {
                Export(fs, model);
            }
        }

        /// <summary>Exports the report to a stream (used by tests and the file overload).</summary>
        public static void Export(Stream stream, VerificationReportModel model)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (model == null) throw new ArgumentNullException(nameof(model));

            RadFixedDocument document = Build(model);
            var provider = new PdfFormatProvider();
            provider.Export(document, stream);
        }

        /// <summary>
        /// Builds the in-memory fixed document. Exposed internally so tests can
        /// assert structure (page count, etc.) without writing to disk.
        /// </summary>
        internal static RadFixedDocument Build(VerificationReportModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            // Iterative fixpoint: simulate the layout with the current forced-break set,
            // detect any section that still starts mid-page and overflows, add it to the
            // forced-break set, and repeat. Converges because we only ever add breaks.
            var forcedBreaks = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.Dictionary<string, int> sectionPages;
            while (true)
            {
                var probe = DiscoverSectionPageNumbers(model, forcedBreaks);
                // TOC page is inserted between cover and body, so shift all page numbers +1.
                sectionPages = new System.Collections.Generic.Dictionary<string, int>();
                foreach (var kv in probe.Pages) sectionPages[kv.Key] = kv.Value + 1;

                if (probe.NewSplits.Count == 0) break;   // stable — no new splits found
                foreach (string key in probe.NewSplits) forcedBreaks.Add(key);
            }

            var document = new RadFixedDocument();
            var editor   = new RadFixedDocumentEditor(document);
            ConfigureEditor(editor);

            // Cover and TOC always occupy their own pages.
            WriteTitle(editor, model);
            editor.InsertPageBreak();
            WriteTableOfContents(editor, model, sectionPages);

            // Render body sections using the stable forced-break set.
            void Section(string key, System.Action write)
            {
                if (forcedBreaks.Contains(key)) editor.InsertPageBreak();
                write();
            }

            // Executive Dashboard always occupies its own page: hard breaks before and after.
            editor.InsertPageBreak();
            WriteExecutiveDashboard(editor, model);
            WriteKeyFindings(editor, model);
            editor.InsertPageBreak();

            Section("1.  Session Overview",            () => WriteOverview(editor, model));
            Section("2.  Applied Corrections",         () => WriteFinalResults(editor, model));
            if (model.HasData)
                Section("3.  Correlation & Latency",   () => WriteCorrelationAndLatency(editor, model));
            Section("4.  Compliance Assessment",       () => WriteCompliance(editor, model));
            Section("5.  Conclusion",                  () => WriteConclusion(editor, model));
            Section("6.  Recommendations",             () => WriteRecommendations(editor, model));
            Section("7.  Observations",                () => WriteObservations(editor, model));
            Section("8.  Axis Detail", () => WriteAxisSection(editor, model));
            Section("9.  Appendices",                  () => { WriteAppendices(editor, model); WriteGlossary(editor, model); });
            Section("10. Scope & Objective",           () => WriteScope(editor, model));
            Section("11. Data Processing Methodology", () => WriteMethodology(editor, model));
            Section("12. Equipment",                   () => WriteEquipment(editor, model));
            Section("13. Test Setup",                  () => WriteTestSetup(editor, model));
            Section("14. Test Conditions",             () => WriteTestConditions(editor, model));

            WriteFooter(editor, model);
            editor.Dispose();
            AddPageFooters(document);
            return document;
        }

		private static void ConfigureEditor(RadFixedDocumentEditor editor)
		{
			editor.SectionProperties.PageSize = new Size(793, 1122);
			// Top margin cleared for the 93-DIP SES header strip; bottom margin cleared
			// for the 70-DIP SES footer strip. Left/right margins unchanged at 56.
			editor.SectionProperties.PageMargins = new TelerikPadding(56, 100, 56, 78);
		}

		// ============================================================
		// Two-pass helpers
		// ============================================================

		/// <summary>
		/// Pass 1 of the two-pass build: builds the document body without a Table of
		/// Contents and records the 1-based page number on which each section begins.
		/// The caller offsets these numbers by +1 before using them in the TOC because
		/// the TOC page itself is inserted between the cover and the body sections.
		/// </summary>
		/// <summary>
		/// Simulates the body layout using <paramref name="forcedBreaks"/> and returns:
		/// <list type="bullet">
		/// <item><description><c>Pages</c> — 1-based start page for each section (for the TOC).</description></item>
		/// <item><description><c>NewSplits</c> — sections that started mid-page and still overflowed
		/// a boundary even with the current <paramref name="forcedBreaks"/> applied. The caller adds
		/// these to <paramref name="forcedBreaks"/> and probes again until the set is empty.</description></item>
		/// </list>
		/// </summary>
		private static (
				System.Collections.Generic.Dictionary<string, int> Pages,
				System.Collections.Generic.HashSet<string> NewSplits)
			DiscoverSectionPageNumbers(
				VerificationReportModel model,
				System.Collections.Generic.IReadOnlySet<string> forcedBreaks)
		{
			// Ground-truth pagination measurement.
			//
			// Telerik commits pages LAZILY while the RadFixedDocumentEditor is alive:
			// doc.Pages.Count only counts fully committed pages and does not include the
			// partially filled page currently being written. Reading it mid-write is
			// therefore unreliable (a single later page break can flush several pages at
			// once). The only trustworthy measurement is taken AFTER the editor is
			// disposed, which commits every page including the last partial one.
			//
			// So we build the probe document, dispose the editor, then scan the finished
			// pages: each section begins with a Heading(...) rendered as an uppercase bar,
			// so we locate the first page whose text contains that heading. A section
			// "spans multiple pages" when the next section's heading (or the end of the
			// document for the last section) lands on a later page than where it started.
			var doc = new RadFixedDocument();

			// Ordered record of every section as it is written, with the exact heading
			// text used to locate it in the finished document and whether it is guaranteed
			// to start at the top of a fresh page (forced break or first body section).
			var order = new System.Collections.Generic.List<(string Key, string Heading, bool StartsFresh)>();

			using (var ed = new RadFixedDocumentEditor(doc))
			{
				ConfigureEditor(ed);

				bool firstBodySection = true;

				void Section(string key, string heading, System.Action write)
				{
					bool forced = forcedBreaks.Contains(key);
					if (forced) ed.InsertPageBreak();

					bool startsFresh = forced || firstBodySection;
					firstBodySection = false;

					order.Add((key, heading, startsFresh));
					write();
				}

				WriteTitle(ed, model);

				// Executive Dashboard is written with hard breaks on both sides, exactly
				// as Build() does. It is intentionally outside Section() so it is never
				// added to forcedBreaks and never contributes a spurious extra break in
				// subsequent iterations that would misalign all following section pages.
				ed.InsertPageBreak();
				WriteExecutiveDashboard(ed, model);
				WriteKeyFindings(ed, model);
				ed.InsertPageBreak();

				Section("1.  Session Overview",            "1. Session Overview",            () => WriteOverview(ed, model));
				Section("2.  Applied Corrections",         "2. Applied Corrections",         () => WriteFinalResults(ed, model));

				if (model.HasData)
					Section("3.  Correlation & Latency",   "3. Correlation and Latency",     () => WriteCorrelationAndLatency(ed, model));

				Section("4.  Compliance Assessment",       "4. Compliance Assessment",       () => WriteCompliance(ed, model));
				Section("5.  Conclusion",                  "5. Conclusion",                  () => WriteConclusion(ed, model));
				Section("6.  Recommendations",             "6. Recommendations",             () => WriteRecommendations(ed, model));
				Section("7.  Observations",                "7. Observations",                () => WriteObservations(ed, model));

                Section("8.  Axis Detail", "8. Axis Detail", () => WriteAxisSection(ed, model));

				Section("9.  Appendices",                  "9. Appendices",                  () => { WriteAppendices(ed, model); WriteGlossary(ed, model); });
				Section("10. Scope & Objective",           "10. Scope and Objective",        () => WriteScope(ed, model));
				Section("11. Data Processing Methodology", "11. Data Processing Methodology", () => WriteMethodology(ed, model));
				Section("12. Equipment",                   "12. Equipment",                  () => WriteEquipment(ed, model));
				Section("13. Test Setup",                  "13. Test Setup",                 () => WriteTestSetup(ed, model));
				Section("14. Test Conditions",             "14. Test Conditions",            () => WriteTestConditions(ed, model));

				WriteFooter(ed, model);
			} // dispose commits every page, including the final partial one

			// Whitespace-stripped, case-sensitive text of every committed page. The
			// heading bar is uppercase, so this never collides with mixed-case body text.
			int totalPages = doc.Pages.Count;
			var pageText = new string[totalPages];
			for (int i = 0; i < totalPages; i++)
				pageText[i] = GetPageText(doc.Pages[i]);

			// 1-based start page (in this TOC-less probe document) for every section.
			var pages     = new System.Collections.Generic.Dictionary<string, int>();
			var newSplits = new System.Collections.Generic.HashSet<string>();

			int FindHeadingPage(string heading, int fromPageIndex)
			{
				string needle = StripWhitespace(heading.ToUpperInvariant());
				for (int i = System.Math.Max(0, fromPageIndex); i < totalPages; i++)
					if (pageText[i].Contains(needle, System.StringComparison.Ordinal))
						return i + 1; // 1-based

				return -1;
			}

			// Locate each section's start page by scanning forward from the previous
			// section's start (headings appear in document order).
			int searchFrom = 0;
			var startPages = new int[order.Count];
			for (int s = 0; s < order.Count; s++)
			{
				int start = FindHeadingPage(order[s].Heading, searchFrom);
				if (start < 0) start = searchFrom + 1; // defensive fallback
				startPages[s] = start;
				pages[order[s].Key] = start;
				searchFrom = start - 1;
			}

			// A section genuinely spans multiple pages when its own content crosses a
			// page boundary. The next section's start page marks where this section ends.
			//
			// When the next section is force-broken we insert an explicit page break
			// before it, which always advances to a fresh page. That means the forced
			// break itself "consumes" one page transition, and this section's content
			// actually ends on the page BEFORE the break: (nextStart - 1). A genuine
			// split is therefore (nextStart - 1) > start, i.e. nextStart > start + 1.
			//
			// When the next section is NOT force-broken, both sections share a page or
			// the next starts on the very next page. A split is simply nextStart > start.
			//
			// This avoids the cascade where suppressing the boundary entirely (the
			// previous approach) masked genuine overflows of this section.
			for (int s = 0; s < order.Count; s++)
			{
				int start     = startPages[s];
				int nextStart = (s + 1 < order.Count) ? startPages[s + 1] : totalPages + 1;

				bool nextForceBroken    = (s + 1 < order.Count) && forcedBreaks.Contains(order[s + 1].Key);
				int  sectionEnd         = nextForceBroken ? nextStart - 1 : nextStart;
				bool spansMultiplePages = sectionEnd > start;

				// Only force a break for sections that start mid-page (i.e. not already
				// fresh) and genuinely overflow. Fresh-starting sections are excluded so
				// the fixpoint set only ever grows and the loop terminates.
				if (!order[s].StartsFresh && spansMultiplePages)
					newSplits.Add(order[s].Key);
			}

			return (pages, newSplits);
		}

		/// <summary>Concatenates all text on a committed page with whitespace removed.</summary>
		private static string GetPageText(RadFixedPage page)
		{
			var sb = new System.Text.StringBuilder();
			foreach (var element in page.Content)
				if (element is Telerik.Windows.Documents.Fixed.Model.Text.TextFragment fragment)
					sb.Append(fragment.Text);

			return StripWhitespace(sb.ToString());
		}

		private static string StripWhitespace(string value)
		{
			var sb = new System.Text.StringBuilder(value.Length);
			foreach (char c in value)
				if (!char.IsWhiteSpace(c))
					sb.Append(c);

			return sb.ToString();
		}

		/// <summary>
		/// Post-processes every page of the built document to:
		/// <list type="bullet">
		///   <item>Stamp the SES Energy header strip (teal band, from Doc-template) at the top.</item>
		///   <item>Stamp the SES Energy footer strip (teal band) at the bottom.</item>
		///   <item>Draw a centred page number ("— N —") over the footer strip.</item>
		/// </list>
		/// Called after the flow editor has been disposed so all pages are committed.
		/// </summary>
		private static void AddPageFooters(RadFixedDocument document)
		{
            const string HeaderTitleText = "MRU Verification Report";
			// Pre-create Telerik image sources once; null-safe when resources are absent (test host).
			TelerikImageSource? backgroundImage = null;
			TelerikImageSource? headerImage     = null;
			TelerikImageSource? footerImage     = null;

			if (_sesBackgroundPng != null)
				using (var ms = new MemoryStream(_sesBackgroundPng))
					backgroundImage = new TelerikImageSource(ms);

			if (_sesHeaderPng != null)
				using (var ms = new MemoryStream(_sesHeaderPng))
					headerImage = new TelerikImageSource(ms);

			if (_sesFooterPng != null)
				using (var ms = new MemoryStream(_sesFooterPng))
					footerImage = new TelerikImageSource(ms);

			// Header strip pixel size: 1240 × 145 px (A4 @ 150 DPI). Displayed at full page
			// width; height is proportional: 145 / 1754 × 1122 ≈ 93 DIP.
			const double HeaderH = 93.0;
			// Footer strip pixel size: 1240 × 110 px. Height: 110 / 1754 × 1122 ≈ 70 DIP.
			const double FooterH = 70.0;

			int total = document.Pages.Count;
			for (int i = 0; i < total; i++)
			{
				var page = document.Pages[i];
				double pageW = page.Size.Width;
				double pageH = page.Size.Height;

				// FixedContentEditor uses the same top-left, Y-downward coordinate system as the
				// flow editor (confirmed by the original page-number placement at y = pageH - 28).
				// Each draw operation gets its own fresh editor so Position.Translate always
					// starts from (0, 0) and is not compounded by previous draws.

					// --- Full-page background watermark (drawn first, behind everything) ---
					if (backgroundImage != null)
					{
						var fceB = new FixedContentEditor(page);
						fceB.Position.Translate(0, 0);
						fceB.DrawImage(backgroundImage, new Size(pageW, pageH));
					}

					// --- Header strip: top of page → y = 0 ---
					if (headerImage != null)
					{
						var fceH = new FixedContentEditor(page);
						fceH.Position.Translate(0, 0);
						fceH.DrawImage(headerImage, new Size(pageW, HeaderH));
					}

                    // --- Report title inside the repeated page header area ---
                    {
                        const double headerTitleWidth = 260;
                        const double headerTitleHeight = 20;
                        const double headerTitleLeftMargin = 56;
                        double headerTitleTop = (HeaderH - headerTitleHeight) / 2.0;

                        var fceT = new FixedContentEditor(page);
                        fceT.Position.Translate(headerTitleLeftMargin, headerTitleTop);

                        var headerBlock = new Block();
                        headerBlock.HorizontalAlignment = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Left;
                        headerBlock.TextProperties.Font = _robotoBold;
                        headerBlock.TextProperties.FontSize = 15;
                        headerBlock.GraphicProperties.FillColor = ColorHeading;
                        headerBlock.InsertText(HeaderTitleText);
                        fceT.DrawBlock(headerBlock, new Size(headerTitleWidth, headerTitleHeight));
                    }

					// --- Footer strip: bottom of page → y = pageH - FooterH ---
					if (footerImage != null)
					{
						var fceF = new FixedContentEditor(page);
						fceF.Position.Translate(0, pageH - FooterH);
						fceF.DrawImage(footerImage, new Size(pageW, FooterH));
					}

					// --- Page number centred vertically inside the footer strip ---
					{
						double cx = pageW / 2.0;
						double py = pageH - FooterH + (FooterH - 12) / 2.0; // mid of footer strip

						var fceP = new FixedContentEditor(page);
						fceP.Position.Translate(cx - 24, py);

						var block = new Block();
						block.HorizontalAlignment         = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Center;
						block.TextProperties.Font         = _robotoRegular;
						block.TextProperties.FontSize     = 8;
						block.GraphicProperties.FillColor = new RgbColor(0x1A, 0x27, 0x32); // dark on teal
						block.InsertText($"\u2014 {i + 1} \u2014");
						fceP.DrawBlock(block, new Size(48, 12));
					}
					}
				}



		/// <summary>
		/// Table of Contents page — lists all report sections with dotted leader lines.
		/// Inserted immediately after the cover page so readers can navigate the document.
		/// </summary>
		private static void WriteTableOfContents(
			RadFixedDocumentEditor editor,
			VerificationReportModel model,
			System.Collections.Generic.Dictionary<string, int> pageNumbers)
		{
			// InsertPageBreak is called by Build() before this method.
			Heading(editor, "Table of Contents");

			var entries = new (string title, string key)[]
			{
				("Executive Dashboard",             "Executive Dashboard"),
				("1.  Session Overview",            "1.  Session Overview"),
				("2.  Applied Corrections",         "2.  Applied Corrections"),
				("3.  Correlation & Latency",       "3.  Correlation & Latency"),
				("4.  Compliance Assessment",       "4.  Compliance Assessment"),
				("5.  Conclusion",                  "5.  Conclusion"),
				("6.  Recommendations",             "6.  Recommendations"),
				("7.  Observations",                "7.  Observations"),
				("8.  Axis Detail",                 "8.  Axis Detail"),
				("9.  Appendices",                  "9.  Appendices"),
				("10. Scope & Objective",           "10. Scope & Objective"),
				("11. Data Processing Methodology", "11. Data Processing Methodology"),
				("12. Equipment",                   "12. Equipment"),
				("13. Test Setup",                  "13. Test Setup"),
				("14. Test Conditions",             "14. Test Conditions"),
			};

			const double tocWidth = 681;
			const double labelCol = 590;
			const double numCol   = tocWidth - labelCol;

			var tocTable = new Table { Borders = new TableBorders(new Border(0, ColorBorder)) };
			tocTable.DefaultCellProperties.Padding = new Thickness(6, 4, 6, 4);

			int rowIdx = 0;
			foreach (var (title, key) in entries)
			{
				bool isAlt = (rowIdx % 2) == 1;
				TableRow row = tocTable.Rows.AddTableRow();

				// Section title cell
				TableCell titleCell = row.Cells.AddTableCell();
				titleCell.PreferredWidth = labelCol;
				if (isAlt) titleCell.Background = ColorRowAlt;
				Block titleBlock = titleCell.Blocks.AddBlock();
				titleBlock.SpacingBefore = 0;
				titleBlock.SpacingAfter  = 0;
				titleBlock.TextProperties.Font     = _robotoBold;
				titleBlock.TextProperties.FontSize = 10.5;
				titleBlock.GraphicProperties.FillColor = ColorHeading;
				titleBlock.InsertText(title);

				// Page number cell (right-aligned)
				TableCell numCell = row.Cells.AddTableCell();
				numCell.PreferredWidth = numCol;
				if (isAlt) numCell.Background = ColorRowAlt;
				Block numBlock = numCell.Blocks.AddBlock();
				numBlock.SpacingBefore = 0;
				numBlock.SpacingAfter  = 0;
				numBlock.HorizontalAlignment = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Right;
				numBlock.TextProperties.Font     = _robotoRegular;
				numBlock.TextProperties.FontSize = 10.5;
				numBlock.GraphicProperties.FillColor = ColorText;
				string pageStr = (pageNumbers != null && pageNumbers.TryGetValue(key, out int pg))
					? pg.ToString(Ci) : "\u2014";
				numBlock.InsertText(pageStr);

				rowIdx++;
			}

			editor.ParagraphProperties.SpacingAfter = 6;
			editor.InsertTable(tocTable);

			Paragraph(editor,
				"This report is structured results-first. Executive findings, corrections and compliance " +
				"appear in sections 1\u20137. Supporting technical detail (axis statistics, appendices, " +
				"methodology and equipment) follows in sections 8\u201314.",
				9.5, ColorMuted, spacingBefore: 10, spacingAfter: 6);
		}

private static void WriteTitle(RadFixedDocumentEditor editor, VerificationReportModel model)
		{
			// Logo: left-aligned, compact, above the full-page cover panel
			if (model.LogoPng != null)

			{
				const int logoW = 220;
				const int logoH = 51; // 220 / 4.284 aspect (SES Energy logo 497x116)
				using (var ms = new MemoryStream(model.LogoPng))
				{
					var logoImage = new TelerikImageSource(ms);
					editor.ParagraphProperties.SpacingBefore    = 0;
					editor.ParagraphProperties.SpacingAfter     = 12;
					editor.ParagraphProperties.HorizontalAlignment = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Left;
					editor.InsertParagraph();
					editor.InsertImageInline(logoImage, new Size(logoW, logoH));
				}
				editor.ParagraphProperties.HorizontalAlignment = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Left;
			}

			// Premium full-page SES cover panel (900x760 GDI -> 681x575 in PDF)
			if (model.CoverBannerPng != null)
			{
				editor.ParagraphProperties.SpacingBefore = 0;
				editor.ParagraphProperties.SpacingAfter  = 0;
				InsertImage(editor, model.CoverBannerPng, 681, 575);
			}
			else
			{
				SetText(editor, _robotoBold, 22, ColorHeading);
				editor.ParagraphProperties.SpacingAfter = 2;
				editor.InsertParagraph();
				editor.InsertRun("MOTION REFERENCE UNIT VERIFICATION REPORT");

				SetText(editor, _robotoRegular, 12, ColorMuted);
				editor.ParagraphProperties.SpacingAfter = 12;
				editor.InsertParagraph();
				editor.InsertRun(model.ProjectName);
				TealRule(editor);

				var rows = new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("Project / reference",    Dash(model.ProjectName)),
					new KeyValuePair<string, string>("Vessel",                 Dash(model.VesselName)),
					new KeyValuePair<string, string>("Operator / surveyor",    Dash(model.Operator)),
					new KeyValuePair<string, string>("Location",               Dash(model.Location)),
					new KeyValuePair<string, string>("Capture start",          Dash(model.StartTime)),
					new KeyValuePair<string, string>("Capture end",            Dash(model.EndTime)),
					new KeyValuePair<string, string>("Duration",               Dash(model.Duration)),
					new KeyValuePair<string, string>("Report generated (UTC)", model.GeneratedUtc.ToString("yyyy-MM-dd HH:mm", Ci)),
				};
				InsertKeyValueTable(editor, rows);
			}
		}


        /// <summary>
        /// Page 2: 2×3 KPI dashboard panel — six large-number cards for instant status read.
        /// </summary>
        private static void WriteExecutiveDashboard(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "Executive Dashboard");
            Introduction(editor,
                "Summary of verification outcome, correction status and key data-quality indicators for a rapid executive review.");

            if (model.ExecutiveDashboardPng != null)
            {
                editor.ParagraphProperties.SpacingAfter = 8;
                // Dashboard image: 900×220 GDI → 681×166 PDF
                InsertImage(editor, model.ExecutiveDashboardPng, 681, 317);
            }

            if (!model.HasData)
            {
                Paragraph(editor,
                    "No measurement data was captured. Acquire reference and vessel motion data, then re-generate.",
                    10.5, ColorMuted, spacingAfter: 6);
            }
        }

        /// <summary>
        /// "Key Findings" executive call-outs — a scannable checklist of the outcome,
        /// placed up front so a reader grasps the result in seconds. Each line is built
        /// from the same measured values used elsewhere in the report (no new numbers).
        /// </summary>
        private static void WriteKeyFindings(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            if (!model.HasData)
            {
                Paragraph(editor,
                    "No measurement data was captured, so no findings are available.",
                    10.5, ColorMuted, spacingAfter: 6);
                return;
            }

            var findings = new List<string>
            {
                "Verification completed successfully."
            };

            if (model.HasCorrectionApplied)
                findings.Add("Recommended corrections applied.");
            else
                findings.Add("Recommended corrections are ready to apply to the vessel unit.");

            if (model.SampleCount > 0)
                findings.Add(string.Format(Ci, "{0:N0} samples collected.", model.SampleCount));

            if (!string.IsNullOrWhiteSpace(model.Duration) &&
                TimeSpan.TryParse(model.Duration, out TimeSpan captureSpan))
                findings.Add(string.Format(Ci, "{0:F1} minute capture duration.", captureSpan.TotalMinutes));

            double worstOutlier = model.WorstOutlierPercent;
            if (!double.IsNaN(worstOutlier))
            {
                if (worstOutlier <= VerificationAssessment.OutlierAcceptablePercent)
                    findings.Add("Data quality exceeded target thresholds.");
                else
                    findings.Add(string.Format(Ci, "Maximum outlier rate {0:F1} %.", worstOutlier));
            }

            // Per-axis correction values
            void AddCorrectionFinding(VerificationAxisKind axis, string axisLabel)
            {
                double corr = model.RecommendedCorrection(axis);
                if (!double.IsNaN(corr))
                {
                    string unit   = model.Unit(axis);
                    string status = model.HasCorrectionApplied ? "applied" : "recommended";
                    findings.Add(string.Format(Ci, "{0} correction {1}: {2:+0.000;-0.000;0.000} {3}",
                        axisLabel, status, corr, unit));
                }
            }
            AddCorrectionFinding(VerificationAxisKind.Pitch, "Pitch");
            AddCorrectionFinding(VerificationAxisKind.Roll,  "Roll");
            AddCorrectionFinding(VerificationAxisKind.Heave, "Heave");

            var table = new Table { Borders = new TableBorders(new Border(0, ColorBorder)) };
            table.DefaultCellProperties.Padding = new Thickness(0);

            foreach (string finding in findings)
            {
                TableRow row = table.Rows.AddTableRow();

                // Narrow green status marker cell (the "✅" indicator).
                TableCell marker = row.Cells.AddTableCell();
                marker.PreferredWidth = 22;
                marker.Background     = ColorAccent;
                marker.Padding        = new Thickness(0, 5, 0, 5);
                Block markerBlock = marker.Blocks.AddBlock();
                markerBlock.HorizontalAlignment = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Center;
                markerBlock.TextProperties.Font       = _robotoBold;
                markerBlock.TextProperties.FontSize   = 11;
                markerBlock.GraphicProperties.FillColor = ColorTableHeaderText;
                markerBlock.InsertText("\u2713");

                TableCell textCell = row.Cells.AddTableCell();
                textCell.PreferredWidth = 659;
                textCell.Padding        = new Thickness(10, 5, 10, 5);
                Block textBlock = textCell.Blocks.AddBlock();
                textBlock.TextProperties.Font       = _robotoRegular;
                textBlock.TextProperties.FontSize   = 10.5;
                textBlock.GraphicProperties.FillColor = ColorText;
                textBlock.InsertText(finding);
            }

            editor.ParagraphProperties.SpacingAfter = 6;
            editor.InsertTable(table);
        }

        private static void WriteScope(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "10. Scope and Objective");
            Introduction(editor,
                "Verification scope, intended outcome and applicable references defining the purpose and boundaries of this report.");

            MruReportMetadata m = model.Metadata;
            Paragraph(editor,
                string.IsNullOrWhiteSpace(m?.TestObjective)
                    ? "Verification of the vessel-installed MRU against a calibrated reference unit. " +
                      "Corrections for pitch, roll, and heave are determined and applied."
                    : m.TestObjective,
                11, ColorText, spacingAfter: 8);

            if (!string.IsNullOrWhiteSpace(m?.ApplicableStandards))
            {
                Paragraph(editor, "Applicable standards and references", 11, ColorHeading, spacingBefore: 4, spacingAfter: 2, bold: true);
                Paragraph(editor, m.ApplicableStandards, 10.5, ColorText, spacingAfter: 6);
            }
        }

        private static void WriteEquipment(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "12. Equipment");
            Introduction(editor,
                "Equipment used for the verification, including the vessel-installed MRU, the reference unit and any supporting hardware.");

            MruReportMetadata m = model.Metadata ?? new MruReportMetadata();

            Paragraph(editor, "MRU under test (vessel-installed)", 12, ColorHeading, spacingBefore: 2, spacingAfter: 4, bold: true);
            InsertKeyValueTable(editor, new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Manufacturer", Dash(m.DutManufacturer)),
                new KeyValuePair<string, string>("Model", Dash(m.DutModel)),
                new KeyValuePair<string, string>("Serial number", Dash(m.DutSerialNumber)),
                new KeyValuePair<string, string>("Firmware version", Dash(m.DutFirmwareVersion)),
            });

            Paragraph(editor, "Reference MRU", 12, ColorHeading, spacingBefore: 10, spacingAfter: 4, bold: true);
            InsertKeyValueTable(editor, new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Manufacturer", Dash(m.ReferenceManufacturer)),
                new KeyValuePair<string, string>("Model", Dash(m.ReferenceModel)),
                new KeyValuePair<string, string>("Serial number", Dash(m.ReferenceSerialNumber)),
                new KeyValuePair<string, string>("Firmware version", Dash(m.ReferenceFirmwareVersion)),
                new KeyValuePair<string, string>("Calibration date", m.ReferenceCalibrationDate.HasValue
                    ? m.ReferenceCalibrationDate.Value.ToString("yyyy-MM-dd", Ci) : "-"),
                new KeyValuePair<string, string>("Calibration certificate", Dash(m.ReferenceCalibrationCertificateNumber)),
            });

            if (!string.IsNullOrWhiteSpace(m.AdditionalEquipment))
            {
                Paragraph(editor, "Additional equipment", 11, ColorHeading, spacingBefore: 10, spacingAfter: 2, bold: true);
                Paragraph(editor, m.AdditionalEquipment, 10.5, ColorText, spacingAfter: 6);
            }
        }

        private static void WriteTestSetup(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "13. Test Setup");
            Introduction(editor,
                "Installation arrangement, sensor geometry, synchronization approach and data acquisition settings used during the verification capture.");

            MruReportMetadata m = model.Metadata ?? new MruReportMetadata();

            InsertKeyValueTable(editor, new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Vessel MRU installation location", Dash(m.DutInstallationLocation)),
                new KeyValuePair<string, string>("Reference MRU installation location", Dash(m.ReferenceInstallationLocation)),
                new KeyValuePair<string, string>("Mounting arrangement", Dash(m.MountingArrangement)),
                new KeyValuePair<string, string>("Coordinate system", Dash(m.CoordinateSystem)),
                new KeyValuePair<string, string>("Sensor separation", Dash(m.SensorSeparation)),
                new KeyValuePair<string, string>("Data acquisition method", Dash(m.DataAcquisitionMethod)),
                new KeyValuePair<string, string>("Synchronization method", Dash(m.SynchronizationMethod)),
                new KeyValuePair<string, string>("Sample rate", m.SampleRateHz.HasValue
                    ? string.Format(Ci, "{0:0.###} Hz", m.SampleRateHz.Value) : "-"),
                new KeyValuePair<string, string>("Logging configuration", Dash(m.LoggingConfiguration)),
                new KeyValuePair<string, string>("Sensor setup (logged)", string.IsNullOrWhiteSpace(model.InputSetup) ? "-" : model.InputSetup),
            });
        }

        private static void WriteTestConditions(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "14. Test Conditions");
            Introduction(editor,
                "Environmental and operational conditions recorded during the verification to provide context for the measured vessel and reference responses.");

            MruReportMetadata m = model.Metadata ?? new MruReportMetadata();

            InsertKeyValueTable(editor, new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Loading condition", Dash(m.LoadingCondition)),
                new KeyValuePair<string, string>("Vessel speed", Dash(m.VesselSpeed)),
                new KeyValuePair<string, string>("Operational mode", Dash(m.OperationalMode)),
                new KeyValuePair<string, string>("Sea state", Dash(m.SeaState)),
                new KeyValuePair<string, string>("Wind conditions", Dash(m.WindConditions)),
                new KeyValuePair<string, string>("Wave conditions", Dash(m.WaveConditions)),
                new KeyValuePair<string, string>("Current conditions", Dash(m.CurrentConditions)),
            });

            if (!string.IsNullOrWhiteSpace(m.EnvironmentalNotes))
            {
                Paragraph(editor, "Environmental notes", 11, ColorHeading, spacingBefore: 10, spacingAfter: 2, bold: true);
                Paragraph(editor, m.EnvironmentalNotes, 10.5, ColorText, spacingAfter: 6);
            }
        }

        private static void WriteMethodology(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "11. Data Processing Methodology");
            Introduction(editor,
                "Summary of synchronization, filtering, statistical treatment and comparison methods used to derive the reported verification metrics.");

            Paragraph(editor,
                "Reference and vessel channels are time-aligned sample-by-sample. " +
                "Per-sample deviation (vessel minus reference) is computed on each axis; " +
                "the mean deviation is the recommended correction. " +
                "Agreement is quantified by Pearson correlation; timing offset by cross-correlation.",
                10.5, ColorText, spacingAfter: 6);

            MruReportMetadata m = model.Metadata;
            if (!string.IsNullOrWhiteSpace(m?.TimeSynchronizationNotes))
            {
                Paragraph(editor, "Time synchronization", 11, ColorHeading, spacingBefore: 4, spacingAfter: 2, bold: true);
                Paragraph(editor, m.TimeSynchronizationNotes, 10.5, ColorText, spacingAfter: 6);
            }
            if (!string.IsNullOrWhiteSpace(m?.FilteringNotes))
            {
                Paragraph(editor, "Filtering", 11, ColorHeading, spacingBefore: 4, spacingAfter: 2, bold: true);
                Paragraph(editor, m.FilteringNotes, 10.5, ColorText, spacingAfter: 6);
            }
            if (!string.IsNullOrWhiteSpace(m?.DataProcessingNotes))
            {
                Paragraph(editor, "Additional processing notes", 11, ColorHeading, spacingBefore: 4, spacingAfter: 2, bold: true);
                Paragraph(editor, m.DataProcessingNotes, 10.5, ColorText, spacingAfter: 6);
            }
        }

        private static void WriteOverview(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "1. Session Overview");
            Introduction(editor,
                "Overview of the verification session, capture timing, operator details, data volume and current correction status.");

            // Visual session info cards (900x180 GDI -> 681x136 PDF)
			if (model.SessionOverviewPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 10;
				InsertImage(editor, model.SessionOverviewPng, 681, 166);
			}

            var rows = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Location",         Dash(model.Location)),
				new KeyValuePair<string, string>("Sensor setup",     string.IsNullOrWhiteSpace(model.InputSetup) ? "\u2014" : model.InputSetup),
				new KeyValuePair<string, string>("Duration",         Dash(model.Duration)),
				new KeyValuePair<string, string>("Samples averaged", model.SampleCount.ToString("N0", Ci)),
				new KeyValuePair<string, string>("Correction",       model.HasCorrectionApplied
					? "Applied" + (string.IsNullOrWhiteSpace(model.CorrectionAppliedAt) ? string.Empty : " (" + model.CorrectionAppliedAt + ")")
					: "Not yet applied"),
            };
            InsertKeyValueTable(editor, rows);

            if (!string.IsNullOrWhiteSpace(model.Comments))
            {
                Paragraph(editor, "Notes", 11, ColorHeading, spacingBefore: 10, spacingAfter: 2, bold: true);
                Paragraph(editor, model.Comments, 10.5, ColorText, spacingAfter: 8);
            }
        }

		private static void WriteFinalResults(RadFixedDocumentEditor editor, VerificationReportModel model)
		{
			Heading(editor, "2. Applied Corrections");
            Introduction(editor,
                "Recommended and applied corrections for pitch, roll and heave, together with their implementation status across the vessel unit.");

			// Hero correction cards (900×280 GDI → 681×212 PDF)
			if (model.CorrectionCardsPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 12;
				InsertImage(editor, model.CorrectionCardsPng, 681, 317);
			}
			else
			{
				// PDF-native correction cards when pre-rendered image is unavailable
				InsertCorrectionCardsNative(editor, model);
			}

			// Bullet charts panel
			if (model.BulletChartsPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 10;
				InsertImage(editor, model.BulletChartsPng, 681, 158);
			}

			// Supporting corrections table
			var table = NewTable();
			AddHeaderRow(table, "Axis", "Recommended", "Applied", "Status");

			int index = 0;
			foreach (VerificationAxisKind axis in AllAxes())
			{
				string unit        = model.Unit(axis);
				double recommended = model.RecommendedCorrection(axis);
				double applied     = model.AppliedCorrection(axis);
				string status      = model.HasCorrectionApplied
					? (Math.Abs(applied - recommended) < 1e-6 ? "Applied as recommended" : "Applied (adjusted)")
					: "Pending";

				AddBodyRow(table, index++,
					model.AxisTitle(axis),
					Format(recommended, unit),
					model.HasCorrectionApplied ? Format(applied, unit) : "—",
					status);
			}

			editor.InsertTable(table);
		}

        private static void WriteCharts(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            if (model.DeviationChartPng == null && model.MeansChartPng == null)
                return;

            if (model.DeviationChartPng != null)
            {
                Paragraph(editor, "Calculated deviation per axis (vessel unit vs. reference):",
                    10, ColorMuted, spacingAfter: 4);
                InsertImage(editor, model.DeviationChartPng, 681, 270);
            }

            if (model.MeansChartPng != null)
            {
                Paragraph(editor, "Reference vs. vessel mean per axis:",
                    10, ColorMuted, spacingBefore: 10, spacingAfter: 4);
                InsertImage(editor, model.MeansChartPng, 681, 315);
            }
        }

        private static void WriteAxisDetails(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            bool firstAxis = true;
            foreach (VerificationAxisKind axis in AllAxes())
            {
                // Generous whitespace + teal rule clearly separates each axis section
                if (!firstAxis)
                {
                    SetText(editor, _robotoRegular, 4, ColorBorder);
                    editor.ParagraphProperties.SpacingBefore = 24;
                    editor.ParagraphProperties.SpacingAfter  = 0;
                    editor.InsertParagraph();
                    editor.InsertRun(" ");
                    TealRule(editor);
                    SetText(editor, _robotoRegular, 4, ColorBorder);
                    editor.ParagraphProperties.SpacingBefore = 10;
                    editor.ParagraphProperties.SpacingAfter  = 0;
                    editor.InsertParagraph();
                    editor.InsertRun(" ");
                }
                firstAxis = false;

                AxisStatistics reference = model.RefStats(axis);
                AxisStatistics test      = model.TestStats(axis);
                AxisStatistics dev       = model.DevStats(axis);
                string         unit      = model.Unit(axis);

                // ── Axis Summary Panel — answer-first card before the detailed table ──
                VerificationStatus axisStatus = VerificationAssessment.Classify(axis, reference, test, dev);
                InsertAxisSummaryCard(editor, model.AxisTitle(axis), axisStatus,
                    model.RecommendedCorrection(axis), model.AppliedCorrection(axis), model.HasCorrectionApplied, unit, dev);

                // Increase the gap so the detail table reads as a separate block under the summary card.
                editor.ParagraphProperties.SpacingBefore = 16;
                editor.ParagraphProperties.SpacingAfter  = 8;

                var table = NewTable();
                AddHeaderRow(table, "Metric", "Reference", "Vessel", "Deviation");
                AddBodyRow(table, 0, "Mean", Stat(reference?.Mean, unit), Stat(test?.Mean, unit), Stat(dev?.Mean, unit));
                AddBodyRow(table, 1, "Std. dev (sigma)", Stat(reference?.StdDev, unit), Stat(test?.StdDev, unit), Stat(dev?.StdDev, unit));
                AddBodyRow(table, 2, "Minimum", Stat(reference?.Min, unit), Stat(test?.Min, unit), Stat(dev?.Min, unit));
                AddBodyRow(table, 3, "Maximum", Stat(reference?.Max, unit), Stat(test?.Max, unit), Stat(dev?.Max, unit));
                AddBodyRow(table, 4, "RMS", Stat(reference?.Rms, unit), Stat(test?.Rms, unit), Stat(dev?.Rms, unit));
                AddBodyRow(table, 5, "Outliers", Percent(reference?.OutlierPercent), Percent(test?.OutlierPercent), Percent(dev?.OutlierPercent));
                AddBodyRow(table, 6, "Samples",
                    (reference?.SampleCount ?? 0).ToString("N0", Ci),
                    (test?.SampleCount ?? 0).ToString("N0", Ci),
                    (dev?.SampleCount ?? 0).ToString("N0", Ci));

                editor.InsertTable(table);
            }
        }


        private static void WriteAxisSection(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "8. Axis Detail");
            Introduction(editor,
                "Detailed per-axis statistics, comparison charts and correction values for pitch, roll and heave across the verification dataset.");

            WriteCharts(editor, model);
            WriteAxisDetails(editor, model);
        }

        private static void WriteCorrelationAndLatency(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            if (!model.HasData)
                return;

            Heading(editor, "3. Correlation and Latency");
            Introduction(editor,
                "Correlation strength and timing-offset metrics comparing reference and vessel motion signals across the measured axes.");
            Paragraph(editor,
                "Correlation: 1.00 = perfect agreement. Latency: positive = vessel lags reference.",
                10.5, ColorText, spacingAfter: 8);

            // ── Correlation bars panel ─────────────────────────────────────────
            if (model.CorrelationBarsPng != null)
            {
                editor.ParagraphProperties.SpacingAfter = 10;
                InsertImage(editor, model.CorrelationBarsPng, 681, 196);
            }

            var table = NewTable();
            AddHeaderRow(table, "Axis", "Correlation", "Estimated latency");

            int index = 0;
            foreach (VerificationAxisKind axis in AllAxes())
            {
                PairedSeriesStatistics pair = model.PairStats(axis);
                AddBodyRow(table, index++,
                    model.AxisTitle(axis),
                    Correlation(pair?.Correlation),
                    Latency(pair?.EstimatedLatencySeconds));
            }

            editor.InsertTable(table);

            // Transparency note: reconcile "poor correlation" with a successful verification.
            Paragraph(editor,
                "Correlation values are reported for transparency and are not used as the primary indicator of " +
                "correction quality. Verification confidence is determined primarily by sample count, capture " +
                "duration, signal quality, and statistical consistency.",
                9.5, ColorMuted, spacingBefore: 8, spacingAfter: 6);
        }

        private static void WriteObservations(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "7. Observations");
            Introduction(editor,
                "Additional notes recorded during the verification that may help explain conditions, limitations or noteworthy aspects of the capture.");

            string observations = model.Metadata?.Observations;
            Paragraph(editor,
                string.IsNullOrWhiteSpace(observations)
                    ? "No additional observations were recorded."
                    : observations,
                10.5, ColorText, spacingAfter: 6);
        }

		private static void WriteCompliance(RadFixedDocumentEditor editor, VerificationReportModel model)
		{
			Heading(editor, "4. Compliance Assessment");
            Introduction(editor,
                "Assessment of sample count, capture duration and signal quality against the recommended criteria for reliable verification results.");

			// VERIFICATION QUALITY summary banner (900×160 GDI → 681×121 PDF)
				if (model.ComplianceSummaryBannerPng != null)
				{
					editor.ParagraphProperties.SpacingAfter = 8;
					InsertImage(editor, model.ComplianceSummaryBannerPng, 681, 121);
				}

				// KPI row: Confidence | Samples | Duration | Outliers (900×100 GDI → 681×75 PDF)
				if (model.ComplianceScorecardsPng != null)
				{
					editor.ParagraphProperties.SpacingAfter = 10;
					InsertImage(editor, model.ComplianceScorecardsPng, 681, 75);
				}

            var table = NewTable();
            AddHeaderRow(table, "Criterion", "Acceptable (min)", "Good (target)", "Measured", "Status");

            int index = 0;

            // Samples
            {
                int samples = model.SampleCount;
                string status =
                    samples >= VerificationAssessment.MinSamplesGood       ? "Good" :
                    samples >= VerificationAssessment.MinSamplesAcceptable ? "Acceptable" :
                    samples > 0                                            ? "Insufficient" : "No data";
                AddBodyRow(table, index++,
                    "Samples",
                    string.Format(Ci, "\u2265\u00A0{0:N0}", VerificationAssessment.MinSamplesAcceptable),
                    string.Format(Ci, "\u2265\u00A0{0:N0}", VerificationAssessment.MinSamplesGood),
                    samples > 0 ? samples.ToString("N0", Ci) : "-",
                    status);
            }

            // Capture duration
            {
                double actualMin = 0;
                if (!string.IsNullOrWhiteSpace(model.Duration) &&
                    TimeSpan.TryParse(model.Duration, out TimeSpan ts))
                    actualMin = ts.TotalMinutes;
                string status =
                    actualMin >= MinDurationRecommendedMinutes ? "Good" :
                    actualMin >= MinDurationAcceptableMinutes  ? "Acceptable" :
                    actualMin > 0                             ? "Insufficient" : "No data";
                AddBodyRow(table, index++,
                    "Capture duration",
                    string.Format(Ci, "\u2265\u00A0{0:F0} min", MinDurationAcceptableMinutes),
                    string.Format(Ci, "\u2265\u00A0{0:F0} min", MinDurationRecommendedMinutes),
                    actualMin > 0 ? string.Format(Ci, "{0:F1} min", actualMin) : "-",
                    status);
            }

            // Max outlier
            {
                double worst = model.WorstOutlierPercent;
                string status =
                    double.IsNaN(worst)                                         ? "No data" :
                    worst <= VerificationAssessment.OutlierAcceptablePercent    ? "Good" :
                    worst <= VerificationAssessment.OutlierAttentionPercent     ? "Acceptable" :
                                                                                  "Too noisy";
                AddBodyRow(table, index++,
                    "Max outlier",
                    string.Format(Ci, "\u2264\u00A0{0:F0}\u00A0%", VerificationAssessment.OutlierAttentionPercent),
                    string.Format(Ci, "\u2264\u00A0{0:F0}\u00A0%", VerificationAssessment.OutlierAcceptablePercent),
                    !double.IsNaN(worst) ? string.Format(Ci, "{0:F1}\u00A0%", worst) : "-",
                    status);
            }

            editor.InsertTable(table);

            // Operator discussion / assessment narrative.
            string discussion = model.Metadata?.AcceptanceCriteriaDiscussion;
            if (!string.IsNullOrWhiteSpace(discussion))
            {
                Paragraph(editor, "Assessment", 11, ColorHeading, spacingBefore: 10, spacingAfter: 2, bold: true);
                Paragraph(editor, discussion, 10.5, ColorText, spacingAfter: 6);
            }

            string specs = model.Metadata?.ManufacturerSpecifications;
            if (!string.IsNullOrWhiteSpace(specs))
            {
                Paragraph(editor, "Manufacturer specifications", 11, ColorHeading, spacingBefore: 10, spacingAfter: 2, bold: true);
                Paragraph(editor, specs, 10.5, ColorText, spacingAfter: 6);
            }
        }

        private static void WriteConclusion(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "5. Conclusion");
            Introduction(editor,
                "Overall verification outcome and interpretation of the calculated corrections based on the measured agreement between vessel and reference data.");
            Paragraph(editor, ConclusionSentence(model), 10.5, ColorText, spacingAfter: 6);
        }

        private static void WriteRecommendations(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "6. Recommendations");
            Introduction(editor,
                "Recommended follow-up actions based on the verification outcome, correction status and any identified data-quality limitations.");

            string recommendations = model.Metadata?.Recommendations;
            Paragraph(editor,
                string.IsNullOrWhiteSpace(recommendations)
                    ? (model.HasCorrectionApplied
                        ? "Maintain the applied corrections and repeat verification following installation changes, " +
                          "firmware updates or periodic maintenance activities."
                        : "Apply the recommended corrections listed in this report to the vessel unit, then re-verify to " +
                          "confirm agreement with the reference.")
                    : recommendations,
                10.5, ColorText, spacingAfter: 6);
        }

        private static void WriteAppendices(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "9. Appendices");
            Introduction(editor,
                "Supporting reference material and explanatory notes that complement the verification results without affecting the reported calculations or conclusions.");

            string notes = model.Metadata?.AppendixNotes;
            Paragraph(editor,
                string.IsNullOrWhiteSpace(notes)
                    ? "No additional appendix material was provided."
                    : notes,
                10.5, ColorText, spacingAfter: 6);
        }

        private static void WriteGlossary(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "Metric Definitions");

            Paragraph(editor,
                "A quick reference guide for interpreting the metrics in this report.",
                9.5, ColorMuted, spacingBefore: 0, spacingAfter: 10);

            var terms = new (string Term, string Definition)[]
            {
                ("Reference MRU",  "The trusted baseline motion sensor. Its mean reading over the capture period is used as the reference value for all comparisons."),
                ("Vessel MRU",     "The unit being verified. Its mean reading is compared against the Reference MRU to calculate the deviation on each axis."),
                ("Deviation",      "Vessel minus Reference on each axis. This is the key output of the verification \u2014 the value used to derive the recommended correction."),
                ("\u03C3 (Sigma)", "Standard deviation: how much the signal varies around its mean. Lower values indicate a stable, consistent signal."),
                ("Min / Max",      "The most extreme values observed during the capture period. Large spreads may indicate transient disturbances or vessel manoeuvres."),
                ("RMS",            "Root-Mean-Square magnitude of the signal. Useful for assessing average energy in the motion signal across the capture."),
                ("Outliers",       "Percentage of samples flagged as anomalous by Tukey\u2019s 1.5\u00D7IQR rule. High rates reduce confidence in the deviation estimate."),
                ("Samples",        "Number of data points included in the statistical calculations. More samples produce a more reliable correction estimate."),
            };

            for (int i = 0; i < terms.Length; i += 2)
            {
                string t2 = i + 1 < terms.Length ? terms[i + 1].Term       : string.Empty;
                string d2 = i + 1 < terms.Length ? terms[i + 1].Definition : string.Empty;
                InsertGlossaryRow(editor, terms[i].Term, terms[i].Definition, t2, d2);
            }

            TealRule(editor);
            Paragraph(editor, "Can the Deviation Be Trusted?", 11, ColorHeading,
                spacingBefore: 10, spacingAfter: 4, bold: true);
            Paragraph(editor,
                string.Format(Ci,
                    "Trustworthiness depends on input data quality, not the deviation value. " +
                    "Samples: \u2265 {0:N0} \u2192 usable, \u2265 {1:N0} \u2192 good. " +
                    "Outliers: \u2264 {2:F0}% \u2192 good, \u2264 {3:F0}% \u2192 usable, > {3:F0}% \u2192 too noisy.",
                    VerificationAssessment.MinSamplesAcceptable,
                    VerificationAssessment.MinSamplesGood,
                    VerificationAssessment.OutlierAcceptablePercent,
                    VerificationAssessment.OutlierAttentionPercent),
                9.5, ColorText, spacingAfter: 6);
        }

        private static void WriteFooter(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            TealRule(editor);
            Paragraph(editor,
                "Generated " + model.GeneratedUtc.ToString("yyyy-MM-dd HH:mm", Ci) +
                " UTC | Motion Verification System",
                8.5, ColorMuted, spacingBefore: 4);
        }

        // ============================================================
        // Presentation helpers — card layouts and section dividers
        // ============================================================

        /// <summary>
        /// Lightweight axis sub-heading: light blue-grey band with Energy Green left accent.
        /// Used inside the Axis Detail section to clearly separate Pitch / Roll / Heave.
        /// </summary>
        private static void AxisSubheading(RadFixedDocumentEditor editor, string axisTitle)
        {
            var headTable = new Table { Borders = new TableBorders(new Border(0, ColorBorder)) };
            headTable.DefaultCellProperties.Padding = new Thickness(10, 7, 10, 7);
            TableRow  headRow  = headTable.Rows.AddTableRow();
            TableCell headCell = headRow.Cells.AddTableCell();
            headCell.PreferredWidth = 681;
            headCell.Background     = new RgbColor(0xE4, 0xEC, 0xF2);
            headCell.Borders        = new TableCellBorders(new Border(5, ColorAccent), null, null, null);
            Block headBlock = headCell.Blocks.AddBlock();
            headBlock.SpacingBefore = 0;
            headBlock.SpacingAfter  = 0;
            headBlock.TextProperties.Font     = _robotoBold;
            headBlock.TextProperties.FontSize = 12;
            headBlock.GraphicProperties.FillColor = ColorHeading;
            headBlock.InsertText(axisTitle.ToUpperInvariant());
            editor.ParagraphProperties.SpacingBefore = 0;
            editor.ParagraphProperties.SpacingAfter  = 4;
            editor.InsertTable(headTable);
        }

        /// <summary>
        /// Compact horizontal banner rendered immediately above each axis statistics table.
        /// Row 0 (header): axis title left + status verdict right, dark background, teal 4 pt left accent.
        /// Row 1 (body):   left column — correction hero value (18 pt teal) and outlier summary;
        ///                 right column — sample count and reliability rating.
        /// Total width 681 pt; SpacingAfter reduced to 6 pt so the banner sits close to its table.
        /// </summary>
		private static void InsertAxisSummaryCard(
			RadFixedDocumentEditor editor,
			string axisTitle,
			VerificationStatus status,
			double recommended,
			double applied,
			bool hasApplied,
			string unit,
			AxisStatistics dev)
		{
			double heroVal   = (hasApplied && !double.IsNaN(applied)) ? applied : recommended;
			string corrStr   = double.IsNaN(heroVal) ? "\u2014"
				: string.Format(Ci, "{0:+0.000;-0.000;0.000} {1}", heroVal, unit);
			string corrLabel = hasApplied ? "APPLIED CORRECTION" : "RECOMMENDED CORRECTION";
			string samplesStr = (dev?.SampleCount ?? 0) > 0
				? dev.SampleCount.ToString("N0", Ci) : "\u2014";
			string outlierStr = dev != null && !double.IsNaN(dev.OutlierPercent)
				? string.Format(Ci, "{0:0.0}%", dev.OutlierPercent) : "\u2014";
			string reliability = status == VerificationStatus.Good       ? "HIGH"
							   : status == VerificationStatus.Acceptable ? "MEDIUM" : "REVIEW";
			string statusLabel = VerificationAssessment.StatusLabel(status);
			string statusLine  = status == VerificationStatus.Good       ? "\u2713  " + statusLabel.ToUpper()
							   : status == VerificationStatus.Acceptable ? "\u25cf  " + statusLabel.ToUpper()
							   : "\u26a0  " + statusLabel.ToUpper();

			RgbColor statusColor = status == VerificationStatus.Good       ? new RgbColor(0x34, 0xD3, 0x89)
								 : status == VerificationStatus.Acceptable ? new RgbColor(0xFF, 0xB8, 0x4D)
								 : new RgbColor(0xFF, 0x6B, 0x6B);

			var labelColor = new RgbColor(0xA0, 0xB4, 0xC4);

			RgbColor outlierColor = dev == null || double.IsNaN(dev.OutlierPercent) ? labelColor
				: dev.OutlierPercent <= VerificationAssessment.OutlierAcceptablePercent ? new RgbColor(0x34, 0xD3, 0x89)
				: dev.OutlierPercent <= VerificationAssessment.OutlierAttentionPercent  ? new RgbColor(0xFF, 0xB8, 0x4D)
				: new RgbColor(0xFF, 0x6B, 0x6B);

			var subtleDivider = new Border(0.5, new RgbColor(0x4A, 0x60, 0x72));

			// ── Compact horizontal banner: 2 rows, 681 pt total ──────────────────────
			// Row 0: header bar  — axis name (left) + status verdict (right)
			// Row 1: body strip  — Correction hero (left col) | Samples + Reliability (right col)
			var banner = new Table
			{
				Borders    = new TableBorders(new Border(1, ColorBorder)),
				LayoutType = TableLayoutType.FixedWidth,
			};
			banner.DefaultCellProperties.Padding = new Thickness(0);

			// ── Row 0: axis title + status (same dark background, no inner divider) ──
			{
				TableRow hr = banner.Rows.AddTableRow();

				// Left: axis name — 440 pt, teal 4 pt left accent
				TableCell titleCell = hr.Cells.AddTableCell();
				titleCell.PreferredWidth = 440;
				titleCell.Background     = ColorHeading;
				titleCell.Padding        = new Thickness(14, 0, 10, 0);
				titleCell.Borders        = new TableCellBorders(new Border(4, ColorAccent), null, null, null);

				Block titleBlock = titleCell.Blocks.AddBlock();
				titleBlock.SpacingBefore = 11;
				titleBlock.SpacingAfter  = 11;
				titleBlock.TextProperties.Font     = _robotoBold;
				titleBlock.TextProperties.FontSize = 16;
				titleBlock.GraphicProperties.FillColor = new RgbColor(0xFF, 0xFF, 0xFF);
				titleBlock.InsertText(axisTitle.ToUpperInvariant());

				// Right: status verdict — 241 pt, no extra accent
				TableCell statusCell = hr.Cells.AddTableCell();
				statusCell.PreferredWidth = 241;
				statusCell.Background     = ColorHeading;
				statusCell.Padding        = new Thickness(14, 0, 14, 0);
				statusCell.Borders        = new TableCellBorders(null, null, null, null);

				Block statusBlock = statusCell.Blocks.AddBlock();
				statusBlock.SpacingBefore = 11;
				statusBlock.SpacingAfter  = 11;
				statusBlock.TextProperties.Font     = _robotoBold;
				statusBlock.TextProperties.FontSize = 13;
				statusBlock.GraphicProperties.FillColor = statusColor;
				statusBlock.InsertText(statusLine);
			}

			// ── Row 1: two-column body ────────────────────────────────────────────────
			// Left  (390 pt): correction hero value + outlier summary
			// Right (291 pt): sample count + reliability rating
			{
				TableRow dr = banner.Rows.AddTableRow();

				// Left column — correction (hero) and outliers
				TableCell leftCell = dr.Cells.AddTableCell();
				leftCell.PreferredWidth = 390;
				leftCell.Background     = ColorHeading;
				leftCell.Padding        = new Thickness(14, 0, 16, 0);
				leftCell.Borders        = new TableCellBorders(null, null, null, null);

				Block corrLabelBlock = leftCell.Blocks.AddBlock();
				corrLabelBlock.SpacingBefore = 11;
				corrLabelBlock.SpacingAfter  = 3;
				corrLabelBlock.TextProperties.Font     = _robotoRegular;
				corrLabelBlock.TextProperties.FontSize = 8.5;
				corrLabelBlock.GraphicProperties.FillColor = labelColor;
				corrLabelBlock.InsertText(corrLabel);

				Block corrValueBlock = leftCell.Blocks.AddBlock();
				corrValueBlock.SpacingBefore = 0;
				corrValueBlock.SpacingAfter  = 6;
				corrValueBlock.TextProperties.Font     = _robotoBold;
				corrValueBlock.TextProperties.FontSize = 24;
				corrValueBlock.GraphicProperties.FillColor = ColorAccent;
				corrValueBlock.InsertText(corrStr);

				Block outlierBlock = leftCell.Blocks.AddBlock();
				outlierBlock.SpacingBefore = 0;
				outlierBlock.SpacingAfter  = 11;
				outlierBlock.TextProperties.Font     = _robotoRegular;
				outlierBlock.TextProperties.FontSize = 11;
				outlierBlock.GraphicProperties.FillColor = outlierColor;
				outlierBlock.InsertText("Outliers  \u00b7  " + outlierStr);

				// Right column — samples and reliability
				TableCell rightCell = dr.Cells.AddTableCell();
				rightCell.PreferredWidth = 291;
				rightCell.Background     = ColorHeading;
				rightCell.Padding        = new Thickness(14, 0, 14, 0);
				rightCell.Borders        = new TableCellBorders(null, subtleDivider, null, null);

				Block samplesLabelBlock = rightCell.Blocks.AddBlock();
				samplesLabelBlock.SpacingBefore = 11;
				samplesLabelBlock.SpacingAfter  = 3;
				samplesLabelBlock.TextProperties.Font     = _robotoRegular;
				samplesLabelBlock.TextProperties.FontSize = 8.5;
				samplesLabelBlock.GraphicProperties.FillColor = labelColor;
				samplesLabelBlock.InsertText("SAMPLES");

				Block samplesValueBlock = rightCell.Blocks.AddBlock();
				samplesValueBlock.SpacingBefore = 0;
				samplesValueBlock.SpacingAfter  = 6;
				samplesValueBlock.TextProperties.Font     = _robotoBold;
				samplesValueBlock.TextProperties.FontSize = 18;
				samplesValueBlock.GraphicProperties.FillColor = new RgbColor(0xFF, 0xFF, 0xFF);
				samplesValueBlock.InsertText(samplesStr);

				Block reliabilityBlock = rightCell.Blocks.AddBlock();
				reliabilityBlock.SpacingBefore = 0;
				reliabilityBlock.SpacingAfter  = 11;
				reliabilityBlock.TextProperties.Font     = _robotoRegular;
				reliabilityBlock.TextProperties.FontSize = 11;
				reliabilityBlock.GraphicProperties.FillColor = statusColor;
				reliabilityBlock.InsertText("Reliability  \u00b7  " + reliability);
			}

			editor.ParagraphProperties.SpacingBefore = 16;
			editor.ParagraphProperties.SpacingAfter  = 6;
			editor.InsertTable(banner);
		}

        /// <summary>
        /// PDF-native three-card correction panel (Pitch / Roll / Heave).
        /// Rendered when the pre-built <c>CorrectionCardsPng</c> image is not available.
        /// </summary>
        private static void InsertCorrectionCardsNative(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            var table = new Table
            {
                Borders    = new TableBorders(new Border(0, ColorBorder)),
                LayoutType = TableLayoutType.FixedWidth,
            };
            table.DefaultCellProperties.Padding = new Thickness(0);

            TableRow row = table.Rows.AddTableRow();
            bool firstCard = true;

            foreach (VerificationAxisKind axis in AllAxes())
            {
                if (!firstCard)
                {
                    // Gap column between cards
                    TableCell gap = row.Cells.AddTableCell();
                    gap.PreferredWidth = 10;
                    gap.Background     = new RgbColor(0xFF, 0xFF, 0xFF);
                    gap.Borders        = new TableCellBorders(new Border(0, ColorBorder));
                    gap.Blocks.AddBlock().InsertText(" ");
                }
                firstCard = false;

                double recommended = model.RecommendedCorrection(axis);
                double applied     = model.AppliedCorrection(axis);
                string unit        = model.Unit(axis);
                // Applied value is the focal point; fall back to recommended when not yet applied
                double heroVal  = (model.HasCorrectionApplied && !double.IsNaN(applied)) ? applied : recommended;
                string corrStr  = double.IsNaN(heroVal) ? "\u2014"
                    : string.Format(Ci, "{0:+0.000;-0.000;0.000} {1}", heroVal, unit);
                string badge    = model.HasCorrectionApplied ? "\u2713  APPLIED" : "\u2713  RECOMMENDED";

                // 3 cards × 220 pt + 2 gaps × 10 pt = 680 pt (≈681)
                TableCell card = row.Cells.AddTableCell();
                card.PreferredWidth = 220;
                card.Background     = new RgbColor(0x1A, 0x2A, 0x3A); // dark card body matching GDI render
                card.Borders        = new TableCellBorders(new Border(5, ColorAccent), null, null, null);

                var ha = Telerik.Windows.Documents.Fixed.Model.Editing.Flow.HorizontalAlignment.Center;

                // Axis label — e.g. "PITCH CORRECTION"
                Block axisLabel = card.Blocks.AddBlock();
                axisLabel.SpacingBefore           = 18;
                axisLabel.SpacingAfter            = 6;
                axisLabel.HorizontalAlignment     = ha;
                axisLabel.TextProperties.Font     = _robotoBold;
                axisLabel.TextProperties.FontSize = 11;
                axisLabel.GraphicProperties.FillColor = ColorAccent;
                axisLabel.InsertText(model.AxisTitle(axis).ToUpperInvariant() + " CORRECTION");

                // Hero correction value — large and white
                Block valueBlock = card.Blocks.AddBlock();
                valueBlock.SpacingBefore           = 10;
                valueBlock.SpacingAfter            = 14;
                valueBlock.HorizontalAlignment     = ha;
                valueBlock.TextProperties.Font     = _robotoBold;
                valueBlock.TextProperties.FontSize = 28;
                valueBlock.GraphicProperties.FillColor = new RgbColor(0xFF, 0xFF, 0xFF);
                valueBlock.InsertText(corrStr);

                // Green "✓ APPLIED" badge
                Block badgeBlock = card.Blocks.AddBlock();
                badgeBlock.SpacingBefore           = 0;
                badgeBlock.SpacingAfter            = 18;
                badgeBlock.HorizontalAlignment     = ha;
                badgeBlock.TextProperties.Font     = _robotoBold;
                badgeBlock.TextProperties.FontSize = 10;
                badgeBlock.GraphicProperties.FillColor = new RgbColor(0x18, 0x7C, 0x4E); // dark green
                badgeBlock.InsertText(badge);
            }

            editor.ParagraphProperties.SpacingBefore = 8;
            editor.ParagraphProperties.SpacingAfter  = 14;
            editor.InsertTable(table);
        }

        /// <summary>
        /// Inserts one row of two side-by-side glossary definition cards.
        /// Pass empty strings for <paramref name="term2"/> / <paramref name="def2"/> to leave
        /// the right cell blank (used when the term count is odd).
        /// </summary>
        private static void InsertGlossaryRow(
            RadFixedDocumentEditor editor,
            string term1, string def1,
            string term2, string def2)
        {
            var table = new Table
            {
                Borders    = new TableBorders(new Border(0, ColorBorder)),
                LayoutType = TableLayoutType.FixedWidth,
            };
            table.DefaultCellProperties.Padding = new Thickness(0);

            TableRow row = table.Rows.AddTableRow();

            // Two cards of 335 pt each with a 10 pt gap: 335 + 10 + 336 = 681
            for (int col = 0; col < 2; col++)
            {
                if (col == 1)
                {
                    TableCell gap = row.Cells.AddTableCell();
                    gap.PreferredWidth = 10;
                    gap.Background     = new RgbColor(0xFF, 0xFF, 0xFF);
                    gap.Borders        = new TableCellBorders(new Border(0, ColorBorder));
                    gap.Blocks.AddBlock().InsertText(" ");
                }

                string term = col == 0 ? term1 : term2;
                string def  = col == 0 ? def1  : def2;
                int    cardW = col == 0 ? 335 : 336;

                TableCell card = row.Cells.AddTableCell();
                card.PreferredWidth = cardW;

                if (string.IsNullOrEmpty(term))
                {
                    card.Background = new RgbColor(0xFF, 0xFF, 0xFF);
                    card.Borders    = new TableCellBorders(new Border(0, ColorBorder));
                    card.Blocks.AddBlock().InsertText(" ");
                    continue;
                }

                card.Background = new RgbColor(0xF2, 0xF5, 0xF7);
                card.Borders    = new TableCellBorders(new Border(3, ColorAccent), null, null, null);

                Block tb = card.Blocks.AddBlock();
                tb.SpacingBefore = 9;
                tb.SpacingAfter  = 3;
                tb.TextProperties.Font     = _robotoBold;
                tb.TextProperties.FontSize = 10;
                tb.GraphicProperties.FillColor = ColorHeading;
                tb.InsertText(term);

                Block db = card.Blocks.AddBlock();
                db.SpacingBefore = 0;
                db.SpacingAfter  = 9;
                db.TextProperties.Font     = _robotoRegular;
                db.TextProperties.FontSize = 9;
                db.GraphicProperties.FillColor = ColorText;
                db.InsertText(def ?? string.Empty);
            }

            editor.ParagraphProperties.SpacingBefore = 0;
            editor.ParagraphProperties.SpacingAfter  = 6;
            editor.InsertTable(table);
        }

        // ============================================================
        // Building blocks
        // ============================================================

        private static IEnumerable<VerificationAxisKind> AllAxes()
        {
            yield return VerificationAxisKind.Pitch;
            yield return VerificationAxisKind.Roll;
            yield return VerificationAxisKind.Heave;
        }

        private static string OverviewSentence(VerificationReportModel model)
        {
            if (!model.HasData)
            {
                return "No measurement data was captured for this project, so a verification result is not available. " +
                       "Capture reference and vessel motion data, then generate the report again.";
            }

            string applied = model.HasCorrectionApplied
                ? "The recommended corrections have been applied to the vessel unit."
                : "The recommended corrections have not yet been applied.";

            return string.Format(Ci,
                "This report verifies the vessel motion unit against a trusted reference using {0:N0} averaged samples " +
                "over a {1} capture. The calculated orientation corrections are listed below. {2}",
                model.SampleCount,
                string.IsNullOrWhiteSpace(model.Duration) ? "completed" : model.Duration,
                applied);
        }

        private static string ConclusionSentence(VerificationReportModel model)
        {
            if (!model.HasData)
            {
                return "No measurement data was available, so no verification conclusion can be drawn. " +
                       "Capture reference and vessel motion data and regenerate the report.";
            }

            int assessed = 0, passed = 0, conditional = 0, failed = 0;
            foreach (VerificationAxisKind axis in AllAxes())
            {
                switch (model.Compliance(axis))
                {
                    case ComplianceResult.Pass: assessed++; passed++; break;
                    case ComplianceResult.Conditional: assessed++; conditional++; break;
                    case ComplianceResult.Fail: assessed++; failed++; break;
                }
            }

            string verdict;
            if (assessed == 0)
            {
                verdict = "No formal acceptance criteria were defined. Measured deviations and recommended " +
                          "corrections are therefore presented for engineering review.";
            }
            else if (failed > 0)
            {
                verdict = string.Format(Ci,
                    "{0} of {1} assessed axes did not meet the acceptance criteria. Apply the recommended corrections " +
                    "and re-verify before relying on the vessel unit.", failed, assessed);
            }
            else if (conditional > 0)
            {
                verdict = string.Format(Ci,
                    "All {0} assessed axes met the acceptance criteria, with {1} within the conditional margin. " +
                    "Applying the recommended corrections is advised.", assessed, conditional);
            }
            else
            {
                verdict = string.Format(Ci,
                    "All {0} assessed axes met the acceptance criteria. The vessel unit agrees with the reference " +
                    "within the stated limits.", assessed);
            }

            string correction = model.HasCorrectionApplied
                ? " The recommended corrections have been applied to the vessel unit."
                : " The recommended corrections have not yet been applied.";

            return verdict + correction;
        }

        /// <summary>
        /// Full-width SES-branded section heading: Dark Blue Grey band with
        /// Energy Green left accent and bold white (via block ForegroundColor) text.
        /// </summary>
        private static void Heading(RadFixedDocumentEditor editor, string text)
        {
            // Spacer before the heading band
            SetText(editor, _robotoRegular, 4, ColorBorder);
            editor.ParagraphProperties.SpacingBefore = 8;
            editor.ParagraphProperties.SpacingAfter  = 0;
            editor.InsertParagraph();
            editor.InsertRun(" ");

            // Full-width Dark Blue Grey heading bar with Energy Green left border
            var headTable = new Table { Borders = new TableBorders(new Border(0, ColorBorder)) };
            headTable.DefaultCellProperties.Padding = new Thickness(10, 6, 10, 6);
            TableRow  headRow  = headTable.Rows.AddTableRow();
            TableCell headCell = headRow.Cells.AddTableCell();
            headCell.PreferredWidth = 681;
            headCell.Background     = ColorHeading;
            headCell.Borders        = new TableCellBorders(new Border(5, ColorAccent), null, null, null);
            Block headBlock = headCell.Blocks.AddBlock();
            headBlock.SpacingBefore = 0;
            headBlock.SpacingAfter  = 0;
            headBlock.TextProperties.Font     = _robotoBold;
            headBlock.TextProperties.FontSize = 12;
            headBlock.GraphicProperties.FillColor = ColorTableHeaderText; // white text on Dark Blue Grey
            headBlock.InsertText(text.ToUpperInvariant());
            editor.InsertTable(headTable);

            SetText(editor, _robotoRegular, 10, ColorText);
            editor.ParagraphProperties.SpacingBefore = 0;
            editor.ParagraphProperties.SpacingAfter  = 4;
        }

        private static void Introduction(RadFixedDocumentEditor editor, string text)
        {
            Paragraph(editor, text, 9.5, ColorMuted, spacingAfter: 8);
        }

        /// <summary>Thin Energy Green horizontal rule — used as a visual section divider.</summary>
        private static void TealRule(RadFixedDocumentEditor editor)
        {
            var table = new Table { Borders = new TableBorders(new Border(0, ColorBorder)) };
            table.DefaultCellProperties.Padding = new Thickness(0);
            TableRow  row  = table.Rows.AddTableRow();
            TableCell cell = row.Cells.AddTableCell();
            cell.PreferredWidth = 681;
            cell.Background     = ColorAccent;
            cell.Borders        = new TableCellBorders(null, new Border(2, ColorAccent), null, null);
            cell.Blocks.AddBlock().InsertText(" ");
            editor.InsertTable(table);
        }

        private static void Paragraph(RadFixedDocumentEditor editor, string text, double size, RgbColor color,
                                      double spacingBefore = 0, double spacingAfter = 6, bool bold = false)
        {
            SetText(editor, bold ? _robotoBold : _robotoRegular, size, color);
            editor.ParagraphProperties.SpacingBefore = spacingBefore;
            editor.ParagraphProperties.SpacingAfter = spacingAfter;
            editor.InsertParagraph();
            editor.InsertRun(text ?? string.Empty);
        }

        private static void HorizontalRule(RadFixedDocumentEditor editor)
        {
            var table = new Table { Borders = new TableBorders(new Border(0, ColorBorder)) };
            table.DefaultCellProperties.Padding = new Thickness(0);
            TableRow row  = table.Rows.AddTableRow();
            TableCell cell = row.Cells.AddTableCell();
            cell.PreferredWidth = 681;
            cell.Borders = new TableCellBorders(null, new Border(0.5, ColorBorder), null, null);
            cell.Blocks.AddBlock().InsertText(" ");
            editor.InsertTable(table);
        }

        private static void SetText(RadFixedDocumentEditor editor, FontBase font, double size, RgbColor color)
        {
            editor.CharacterProperties.Font = font;
            editor.CharacterProperties.FontSize = size;
            editor.CharacterProperties.ForegroundColor = color;
        }

        private static void InsertImage(RadFixedDocumentEditor editor, byte[] png, double width, double height)
        {
            using (var ms = new MemoryStream(png))
            {
                var image = new TelerikImageSource(ms);
                editor.ParagraphProperties.SpacingAfter = 8;
                editor.InsertParagraph();
                editor.InsertImageInline(image, new Size(width, height));
            }
        }

        // ── Tables ──

        private static Table NewTable()
        {
            var table = new Table
            {
                Borders    = new TableBorders(new Border(0.5, ColorBorder)),
                LayoutType = TableLayoutType.FixedWidth,
            };
            table.DefaultCellProperties.Padding = new Thickness(6, 4, 6, 4);
            return table;
        }

        private static void AddHeaderRow(Table table, params string[] cells)
        {
            TableRow row = table.Rows.AddTableRow();
            foreach (string text in cells)
            {
                TableCell cell = row.Cells.AddTableCell();
                cell.Background = ColorTableHeader;
                cell.Borders    = new TableCellBorders(
                    new Border(0, ColorTableHeader),
                    new Border(0, ColorTableHeader),
                    new Border(0, ColorTableHeader),
                    new Border(1, ColorBorder));
                Block block = cell.Blocks.AddBlock();
                ApplyCellTextHeader(block);
                InsertHeaderText(block, text);
            }
        }

        private static void AddBodyRow(Table table, int index, params string[] cells)
        {
            TableRow row = table.Rows.AddTableRow();
            bool alt = (index % 2) == 1;
            for (int i = 0; i < cells.Length; i++)
            {
                TableCell cell = row.Cells.AddTableCell();
                if (alt) cell.Background = ColorRowAlt;
                Block block = cell.Blocks.AddBlock();
                FontBase font = i == 0 ? _robotoBold : _robotoRegular;
                ApplyCellText(block, font);
                block.InsertText(cells[i] ?? string.Empty);
            }
        }

        private static void InsertKeyValueTable(RadFixedDocumentEditor editor, List<KeyValuePair<string, string>> rows)
        {
            var table = NewTable();
            int index = 0;
            foreach (KeyValuePair<string, string> kv in rows)
            {
                TableRow row = table.Rows.AddTableRow();
                bool alt = (index++ % 2) == 1;

                TableCell key = row.Cells.AddTableCell();
                key.PreferredWidth = 170;
                if (alt) key.Background = ColorRowAlt;
                Block keyBlock = key.Blocks.AddBlock();
                ApplyCellText(keyBlock, _robotoBold);
                keyBlock.InsertText(kv.Key);

                TableCell val = row.Cells.AddTableCell();
                val.PreferredWidth = 510;
                if (alt) val.Background = ColorRowAlt;
                Block valBlock = val.Blocks.AddBlock();
                ApplyCellText(valBlock, _robotoRegular);
                valBlock.InsertText(kv.Value ?? string.Empty);
            }
            editor.InsertTable(table);
        }

        // Only the font (bold vs regular) can be set on table-cell text via the
        // public RadPdfProcessing API; size and colour fall back to the document
        // defaults (dark text on light/shaded cells).
        private static void ApplyCellText(Block block, FontBase font)
        {
            block.SpacingAfter  = 0;
            block.SpacingBefore = 0;
            block.TextProperties.Font = font;
        }

        private static void ApplyCellTextHeader(Block block)
        {
            block.SpacingAfter  = 0;
            block.SpacingBefore = 0;
            block.TextProperties.Font         = _robotoBold;
            block.GraphicProperties.FillColor = ColorTableHeaderText; // white text on Dark Blue Grey header
        }

        private static void InsertHeaderText(Block block, string text)
        {
            block.InsertText(text);
        }

        // ── Formatting helpers ──

        private static string Dash(string s) => string.IsNullOrWhiteSpace(s) ? "-" : s;

        private static string Format(double value, string unit)
        {
            if (double.IsNaN(value)) return "-";
            return string.Format(Ci, "{0:+0.000;-0.000;0.000} {1}", value, unit);
        }

        private static string Stat(double? value, string unit)
        {
            if (value == null || double.IsNaN(value.Value)) return "-";
            return string.Format(Ci, "{0:0.000} {1}", value.Value, unit);
        }

        private static string Percent(double? value)
        {
            if (value == null || double.IsNaN(value.Value)) return "-";
            return string.Format(Ci, "{0:0.0} %", value.Value);
        }

        private static string Correlation(double? value)
        {
            if (value == null || double.IsNaN(value.Value)) return "-";
            return string.Format(Ci, "{0:0.000}", value.Value);
        }

        private static string Latency(double? seconds)
        {
            if (seconds == null || double.IsNaN(seconds.Value)) return "-";
            double ms = seconds.Value * 1000.0;
            return string.Format(Ci, "{0:+0;-0;0} ms", ms);
        }
    }
}
