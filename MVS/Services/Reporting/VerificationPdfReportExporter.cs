using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using MVS.Models;
using Telerik.Windows.Documents.Fixed.FormatProviders.Pdf;
using Telerik.Windows.Documents.Fixed.Model;
using Telerik.Windows.Documents.Fixed.Model.ColorSpaces;
using Telerik.Windows.Documents.Fixed.Model.Editing;
using Telerik.Windows.Documents.Fixed.Model.Editing.Flow;
using Telerik.Windows.Documents.Fixed.Model.Editing.Tables;
using Telerik.Windows.Documents.Fixed.Model.Fonts;
using Telerik.Windows.Documents.Fixed.Model.Resources;
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

        // ── Palette (matches the on-screen review styling) ──
        // NOTE: RadPdfProcessing only lets us set the *font* (bold/regular) on
        // table-cell text, not its size or colour; those are controllable only for
        // text written through the editor's CharacterProperties. Table structure is
        // therefore conveyed with cell background shading and bold header text.
        private static readonly RgbColor ColorHeading = new RgbColor(0, 51, 102);
        private static readonly RgbColor ColorText = new RgbColor(40, 40, 40);
        private static readonly RgbColor ColorMuted = new RgbColor(110, 110, 110);
        private static readonly RgbColor ColorTableHeader = new RgbColor(215, 226, 238);
        private static readonly RgbColor ColorRowAlt = new RgbColor(242, 246, 250);
        private static readonly RgbColor ColorBorder = new RgbColor(210, 210, 210);

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
            var editor = new RadFixedDocumentEditor(document);

            // A4 portrait with comfortable margins.
            editor.SectionProperties.PageSize = new Size(793, 1122);
            editor.SectionProperties.PageMargins = new TelerikPadding(56);

            // 1. Title / identification
            WriteTitle(editor, model);
            // 2. Scope and objective
            WriteScope(editor, model);
            // 3. Equipment
            WriteEquipment(editor, model);
            // 4. Test setup
            WriteTestSetup(editor, model);
            // 5. Test conditions
            WriteTestConditions(editor, model);
            // 6. Data processing methodology
            WriteMethodology(editor, model);
            // 7. Executive summary + capture overview
            WriteOverview(editor, model);
            // 8. Results - recommended/applied corrections
            WriteFinalResults(editor, model);
            // 8b. Correlation and latency
            WriteCorrelationAndLatency(editor, model);
            // 8c. Result graphics
            WriteCharts(editor, model);
            // 9. Supporting per-axis statistics
            WriteAxisDetails(editor, model);
            // 10. Observations
            WriteObservations(editor, model);
            // 11. Compliance assessment
            WriteCompliance(editor, model);
            // 12. Conclusion
            WriteConclusion(editor, model);
            // 13. Recommendations
            WriteRecommendations(editor, model);
            // 14. Appendices / glossary
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
            SetText(editor, FontsRepository.HelveticaBold, 22, ColorHeading);
            editor.ParagraphProperties.SpacingAfter = 2;
            editor.InsertParagraph();
            editor.InsertRun("Motion Reference Unit Verification Report");

            SetText(editor, FontsRepository.Helvetica, 12, ColorMuted);
            editor.ParagraphProperties.SpacingAfter = 12;
            editor.InsertParagraph();
            editor.InsertRun(model.ProjectName);

            HorizontalRule(editor);

            var rows = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Project / reference", Dash(model.ProjectName)),
                new KeyValuePair<string, string>("Vessel", Dash(model.VesselName)),
                new KeyValuePair<string, string>("Operator / surveyor", Dash(model.Operator)),
                new KeyValuePair<string, string>("Location", Dash(model.Location)),
                new KeyValuePair<string, string>("Report generated", model.GeneratedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", Ci)),
            };
            InsertKeyValueTable(editor, rows);
        }

        private static void WriteScope(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            Heading(editor, "1. Scope and Objective");

            MruReportMetadata m = model.Metadata;
            Paragraph(editor,
                string.IsNullOrWhiteSpace(m?.TestObjective)
                    ? "This report documents the verification of a vessel-installed Motion Reference Unit (MRU) " +
                      "against a calibrated reference MRU. The objective is to quantify the agreement between the " +
                      "two units in pitch, roll and heave, and to determine any orientation corrections required " +
                      "for the vessel unit."
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
                "Reference and vessel motion channels are logged simultaneously and time-aligned sample-by-sample. " +
                "For each axis the deviation (vessel minus reference) is computed per sample, and descriptive " +
                "statistics (mean, standard deviation, minimum, maximum and RMS) are calculated over the capture. " +
                "The mean deviation on each axis is taken as the recommended orientation correction for the vessel unit. " +
                "Agreement between the two units is additionally quantified by the Pearson correlation coefficient, and " +
                "the relative timing is estimated by cross-correlation (see Correlation and Latency).",
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
            Heading(editor, "6. Executive Summary");

            Paragraph(editor, OverviewSentence(model), 11, ColorText, spacingAfter: 10);

            var rows = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Project", model.ProjectName),
                new KeyValuePair<string, string>("Operator", Dash(model.Operator)),
                new KeyValuePair<string, string>("Vessel", Dash(model.VesselName)),
                new KeyValuePair<string, string>("Location", Dash(model.Location)),
                new KeyValuePair<string, string>("Sensor setup", string.IsNullOrWhiteSpace(model.InputSetup) ? "-" : model.InputSetup),
                new KeyValuePair<string, string>("Capture start", Dash(model.StartTime)),
                new KeyValuePair<string, string>("Capture end", Dash(model.EndTime)),
                new KeyValuePair<string, string>("Duration", Dash(model.Duration)),
                new KeyValuePair<string, string>("Samples averaged", model.SampleCount.ToString("N0", Ci)),
                new KeyValuePair<string, string>("Correction applied", model.HasCorrectionApplied
                    ? "Yes" + (string.IsNullOrWhiteSpace(model.CorrectionAppliedAt) ? string.Empty : " (" + model.CorrectionAppliedAt + ")")
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
            Heading(editor, "7. Results - Recommended Corrections");

            Paragraph(editor,
                "These are the orientation corrections the verification calculated for the vessel unit. " +
                "A correction is the offset that must be applied so the vessel unit matches the trusted reference.",
                10.5, ColorText, spacingAfter: 10);

            var table = NewTable();
            AddHeaderRow(table, "Axis", "Recommended", "Applied", "Status");

            int index = 0;
            foreach (VerificationAxisKind axis in AllAxes())
            {
                string unit = model.Unit(axis);
                double recommended = model.RecommendedCorrection(axis);
                double applied = model.AppliedCorrection(axis);
                string status = model.HasCorrectionApplied
                    ? (Math.Abs(applied - recommended) < 1e-6 ? "Applied as recommended" : "Applied (adjusted)")
                    : "Pending";

                AddBodyRow(table, index++,
                    model.AxisTitle(axis),
                    Format(recommended, unit),
                    model.HasCorrectionApplied ? Format(applied, unit) : "-",
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
                Paragraph(editor,
                    "Calculated deviation per axis (how far the vessel unit differs from the reference):",
                    10.5, ColorText, spacingAfter: 4);
                InsertImage(editor, model.DeviationChartPng, 680, 240);
            }

            if (model.MeansChartPng != null)
            {
                Paragraph(editor,
                    "Reference versus vessel mean per axis:",
                    10.5, ColorText, spacingBefore: 8, spacingAfter: 4);
                InsertImage(editor, model.MeansChartPng, 680, 285);
            }
        }

        private static void WriteAxisDetails(RadFixedDocumentEditor editor, VerificationReportModel model)
        {
            editor.InsertPageBreak();
            Heading(editor, "8. Supporting Detail per Axis");

            foreach (VerificationAxisKind axis in AllAxes())
            {
                AxisStatistics reference = model.RefStats(axis);
                AxisStatistics test = model.TestStats(axis);
                AxisStatistics dev = model.DevStats(axis);
                string unit = model.Unit(axis);

                Paragraph(editor, model.AxisTitle(axis), 13, ColorHeading, spacingBefore: 8, spacingAfter: 2, bold: true);

                string summary = VerificationAssessment.Summary(axis, reference, test, dev);
                VerificationStatus status = VerificationAssessment.Classify(axis, reference, test, dev);
                Paragraph(editor,
                    VerificationAssessment.StatusLabel(status) + " - " + summary,
                    10.5, ColorText, spacingAfter: 6);

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
                "Correlation measures how closely the vessel unit tracks the reference over time (1.00 = perfect " +
                "agreement). Estimated latency is the time shift that best aligns the two signals; a positive value " +
                "means the vessel unit lags the reference.",
                10.5, ColorText, spacingAfter: 8);

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

            Paragraph(editor,
                "The mean deviation on each axis is compared against the acceptance criterion entered for this " +
                "verification. A deviation within the criterion is a Pass; up to 1.5x the criterion is a Conditional " +
                "Pass; anything larger is a Fail. Axes without an entered criterion are reported as \"Not assessed\".",
                10.5, ColorText, spacingAfter: 8);

            var table = NewTable();
            AddHeaderRow(table, "Axis", "Mean deviation", "Acceptance criterion", "Verdict");

            int index = 0;
            foreach (VerificationAxisKind axis in AllAxes())
            {
                string unit = model.Unit(axis);
                double mean = model.DevStats(axis)?.Mean ?? double.NaN;
                double? threshold = model.AcceptanceThreshold(axis);
                ComplianceResult result = model.Compliance(axis);

                AddBodyRow(table, index++,
                    model.AxisTitle(axis),
                    Stat(double.IsNaN(mean) ? (double?)null : mean, unit),
                    threshold.HasValue ? Stat(threshold, unit) : "-",
                    VerificationAssessment.ComplianceLabel(result));
            }

            editor.InsertTable(table);

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
            HorizontalRule(editor);
            Paragraph(editor,
                "Generated " + model.GeneratedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", Ci) +
                " - Motion Verification System.",
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

        private static void Heading(RadFixedDocumentEditor editor, string text)
        {
            SetText(editor, FontsRepository.HelveticaBold, 15, ColorHeading);
            editor.ParagraphProperties.SpacingBefore = 12;
            editor.ParagraphProperties.SpacingAfter = 4;
            editor.InsertParagraph();
            editor.InsertRun(text);
        }

        private static void Paragraph(RadFixedDocumentEditor editor, string text, double size, RgbColor color,
                                      double spacingBefore = 0, double spacingAfter = 6, bool bold = false)
        {
            SetText(editor, bold ? FontsRepository.HelveticaBold : FontsRepository.Helvetica, size, color);
            editor.ParagraphProperties.SpacingBefore = spacingBefore;
            editor.ParagraphProperties.SpacingAfter = spacingAfter;
            editor.InsertParagraph();
            editor.InsertRun(text ?? string.Empty);
        }

        private static void HorizontalRule(RadFixedDocumentEditor editor)
        {
            var table = new Table { Borders = new TableBorders(new Border(0.5, ColorBorder)) };
            table.DefaultCellProperties.Padding = new Thickness(0);
            TableRow row = table.Rows.AddTableRow();
            TableCell cell = row.Cells.AddTableCell();
            cell.PreferredWidth = 680;
            cell.Borders = new TableCellBorders(
                null, new Border(0.5, ColorBorder), null, null);
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
                var image = new ImageSource(ms);
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
                Borders = new TableBorders(new Border(0.5, ColorBorder)),
                LayoutType = TableLayoutType.FixedWidth,
            };
            table.DefaultCellProperties.Padding = new Thickness(5, 3, 5, 3);
            return table;
        }

        private static void AddHeaderRow(Table table, params string[] cells)
        {
            TableRow row = table.Rows.AddTableRow();
            foreach (string text in cells)
            {
                TableCell cell = row.Cells.AddTableCell();
                cell.Background = ColorTableHeader;
                Block block = cell.Blocks.AddBlock();
                ApplyCellText(block, FontsRepository.HelveticaBold);
                block.InsertText(text);
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
                FontBase font = i == 0 ? FontsRepository.HelveticaBold : FontsRepository.Helvetica;
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
                ApplyCellText(keyBlock, FontsRepository.HelveticaBold);
                keyBlock.InsertText(kv.Key);

                TableCell val = row.Cells.AddTableCell();
                val.PreferredWidth = 510;
                if (alt) val.Background = ColorRowAlt;
                Block valBlock = val.Blocks.AddBlock();
                ApplyCellText(valBlock, FontsRepository.Helvetica);
                valBlock.InsertText(kv.Value ?? string.Empty);
            }
            editor.InsertTable(table);
        }

        // Only the font (bold vs regular) can be set on table-cell text via the
        // public RadPdfProcessing API; size and colour fall back to the document
        // defaults (dark text on light/shaded cells).
        private static void ApplyCellText(Block block, FontBase font)
        {
            block.SpacingAfter = 0;
            block.SpacingBefore = 0;
            block.TextProperties.Font = font;
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
