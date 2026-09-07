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

            var document = new RadFixedDocument();
            var editor   = new RadFixedDocumentEditor(document);

            // A4 portrait with comfortable margins.
            editor.SectionProperties.PageSize    = new Size(793, 1122);
            editor.SectionProperties.PageMargins = new TelerikPadding(56);

            // ── Page 1: Cover / identification ──────────────────────────────
            WriteTitle(editor, model);

            // ── Page 2: Executive dashboard ──────────────────────────────────
            WriteExecutiveDashboard(editor, model);

            // ── Body sections ─────────────────────────────────────────────────
            // 1. Scope and objective
            WriteScope(editor, model);
            // 2. Equipment
            WriteEquipment(editor, model);
            // 3. Test setup
            WriteTestSetup(editor, model);
            // 4. Test conditions
            WriteTestConditions(editor, model);
            // 5. Data processing methodology
            WriteMethodology(editor, model);
            // 6. Overview / session summary
            WriteOverview(editor, model);
            // 7. Results — recommended/applied corrections
            WriteFinalResults(editor, model);
            // 7b. Correlation and latency
            WriteCorrelationAndLatency(editor, model);
            // 7c. Result graphics (deviation + means bar charts)
            WriteCharts(editor, model);
            // 8. Supporting per-axis statistics
            WriteAxisDetails(editor, model);
            // 9. Observations
            WriteObservations(editor, model);
            // 10. Compliance assessment
            WriteCompliance(editor, model);
            // 11. Conclusion
            WriteConclusion(editor, model);
            // 12. Recommendations
            WriteRecommendations(editor, model);
            // 13. Appendices / glossary
            WriteAppendices(editor, model);
            WriteGlossary(editor, model);

            WriteFooter(editor, model);

            return document;
        }

        // ============================================================
        // Sections
        // ============================================================

		private static void WriteTitle(RadFixedDocumentEditor editor, VerificationReportModel model)
		{
			// Logo: left-aligned, compact, above the full-page cover panel
			if (model.LogoPng != null)
			{
				const int logoW = 220;
				const int logoH = 59; // 220 / 3.717 aspect
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
            editor.InsertPageBreak();
            Heading(editor, "Executive Dashboard");

            if (model.ExecutiveDashboardPng != null)
            {
                editor.ParagraphProperties.SpacingAfter = 8;
                // Dashboard image: 900×220 GDI → 681×166 PDF
                InsertImage(editor, model.ExecutiveDashboardPng, 681, 166);
            }

            if (!model.HasData)
            {
                Paragraph(editor,
                    "No measurement data was captured. Acquire reference and vessel motion data, then re-generate.",
                    10.5, ColorMuted, spacingAfter: 6);
            }
        }

        private static void WriteScope(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "1. Scope and Objective");

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
            Heading(editor, "2. Equipment");
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
            Heading(editor, "3. Test Setup");
            MruReportMetadata m = model.Metadata ?? new MruReportMetadata();

            InsertKeyValueTable(editor, new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("DUT installation location", Dash(m.DutInstallationLocation)),
                new KeyValuePair<string, string>("Reference installation location", Dash(m.ReferenceInstallationLocation)),
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
            Heading(editor, "4. Test Conditions");
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
            Heading(editor, "5. Data Processing Methodology");

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
            editor.InsertPageBreak();
            Heading(editor, "6. Session Overview");

            // Visual session info cards (900x180 GDI -> 681x136 PDF)
			if (model.SessionOverviewPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 10;
				InsertImage(editor, model.SessionOverviewPng, 681, 136);
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
			Heading(editor, "7. Results — Applied Corrections");

			// Hero correction cards (900×280 GDI → 681×212 PDF)
			if (model.CorrectionCardsPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 12;
				InsertImage(editor, model.CorrectionCardsPng, 681, 212);
			}

			// Bullet charts panel (deviation vs. reference scale) (900×210 GDI → 681×158 PDF)
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

            Heading(editor, "Result Graphics");

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
            editor.InsertPageBreak();
            Heading(editor, "8. Supporting Detail per Axis");

            foreach (VerificationAxisKind axis in AllAxes())
            {
                AxisStatistics reference = model.RefStats(axis);
                AxisStatistics test      = model.TestStats(axis);
                AxisStatistics dev       = model.DevStats(axis);
                string         unit      = model.Unit(axis);

                // ── Per-axis summary banner (900×96 GDI → 681×72 PDF) ─────────────
                byte[] summaryPng = model.AxisSummaryPng(axis);
                if (summaryPng != null)
                {
                    editor.ParagraphProperties.SpacingBefore = 8;
                    editor.ParagraphProperties.SpacingAfter  = 6;
                    InsertImage(editor, summaryPng, 681, 72);
                }
                else
                {
                    VerificationStatus fallbackStatus = VerificationAssessment.Classify(axis, reference, test, dev);
                    Paragraph(editor,
                        model.AxisTitle(axis) + "  \u2014  " + VerificationAssessment.StatusLabel(fallbackStatus),
                        12, ColorHeading, spacingBefore: 8, spacingAfter: 2, bold: true);
                }

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

        private static void WriteCorrelationAndLatency(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            if (!model.HasData)
                return;

            Heading(editor, "Correlation and Latency");
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
        }

        private static void WriteObservations(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            editor.InsertPageBreak();
            Heading(editor, "9. Observations");

            string observations = model.Metadata?.Observations;
            Paragraph(editor,
                string.IsNullOrWhiteSpace(observations)
                    ? "No specific observations were recorded during the verification."
                    : observations,
                10.5, ColorText, spacingAfter: 6);
        }

		private static void WriteCompliance(RadFixedDocumentEditor editor, VerificationReportModel model)
		{
			Heading(editor, "10. Compliance Assessment");

			// Compliance scorecards (900×200 GDI → 681×151 PDF)
			if (model.ComplianceScorecardsPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 10;
				InsertImage(editor, model.ComplianceScorecardsPng, 681, 151);
			}

			// Data quality confidence panel (900×260 GDI → 681×197 PDF)
			if (model.ConfidencePanelPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 10;
				InsertImage(editor, model.ConfidencePanelPng, 681, 197);
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
            Heading(editor, "11. Conclusion");
            Paragraph(editor, ConclusionSentence(model), 10.5, ColorText, spacingAfter: 6);
        }

        private static void WriteRecommendations(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "12. Recommendations");

            string recommendations = model.Metadata?.Recommendations;
            Paragraph(editor,
                string.IsNullOrWhiteSpace(recommendations)
                    ? (model.HasCorrectionApplied
                        ? "Apply and retain the corrections listed in this report. Re-verify periodically and after any " +
                          "change to the installation or firmware."
                        : "Apply the recommended corrections listed in this report to the vessel unit, then re-verify to " +
                          "confirm agreement with the reference.")
                    : recommendations,
                10.5, ColorText, spacingAfter: 6);
        }

        private static void WriteAppendices(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "13. Appendices");

            string notes = model.Metadata?.AppendixNotes;
            Paragraph(editor,
                string.IsNullOrWhiteSpace(notes)
                    ? "No additional appendix material was provided."
                    : notes,
                10.5, ColorText, spacingAfter: 6);
        }

        private static void WriteGlossary(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "What the Numbers Mean");
            Paragraph(editor, VerificationAssessment.Glossary(VerificationAxisKind.Pitch), 9.5, ColorText, spacingAfter: 6);
        }

        private static void WriteFooter(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            TealRule(editor);
            Paragraph(editor,
                "Generated " + model.GeneratedUtc.ToString("yyyy-MM-dd HH:mm", Ci) +
                " UTC \u2014 Motion Verification System  |  MRU Verification Report",
                8.5, ColorMuted, spacingBefore: 4);
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
                verdict = "No acceptance criteria were entered, so a formal pass/fail verdict is not stated; the " +
                          "measured deviations and recommended corrections are reported for engineering review.";
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
