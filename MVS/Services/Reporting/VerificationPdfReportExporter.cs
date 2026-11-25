using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using MVS.Models;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace MVS.Services.Reporting
{
    /// <summary>
    /// Builds and exports the verification report as a PDF using PDFsharp.
    /// The report includes the SES cover and page branding, measurement
    /// summaries, charts, equipment and test metadata, and supporting definitions.
    /// </summary>
    public static class VerificationPdfReportExporter
    {
        private static readonly CultureInfo Ci = CultureInfo.CurrentCulture;

        private static readonly XColor ColorHeading = XColor.FromArgb(255, 0x33, 0x4A, 0x5C);
        private static readonly XColor ColorAccent = XColor.FromArgb(255, 0x3D, 0xE6, 0xA9);
        private static readonly XColor ColorText = XColor.FromArgb(255, 0x1A, 0x27, 0x32);
        private static readonly XColor ColorMuted = XColor.FromArgb(255, 0x7F, 0x94, 0xA5);
        private static readonly XColor ColorRowAlt = XColor.FromArgb(255, 0xF2, 0xF5, 0xF7);
        private static readonly XColor ColorBorder = XColor.FromArgb(255, 0xD6, 0xDF, 0xE6);

        private static readonly byte[] SesBackgroundPng = LoadResourceBytes(
            "pack://application:,,,/MVS;component/Resources/DocTemplate/ses-background.png");
        private static readonly byte[] SesHeaderPng = LoadResourceBytes(
            "pack://application:,,,/MVS;component/Resources/DocTemplate/ses-header.png");
        private static readonly byte[] SesFooterPng = LoadResourceBytes(
            "pack://application:,,,/MVS;component/Resources/DocTemplate/ses-footer.png");

        private const double MinDurationAcceptableMinutes = 20.0;
        private const double MinDurationRecommendedMinutes = 40.0;

        /// <summary>Exports the report to a file on disk.</summary>
        public static void Export(string path, VerificationReportModel model)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path is required.", nameof(path));
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            using (FileStream stream = File.Create(path))
                Export(stream, model);
        }

        /// <summary>Exports the report to a stream (used by tests and the file overload).</summary>
        public static void Export(Stream stream, VerificationReportModel model)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            using (PdfDocument document = Build(model))
                document.Save(stream);
        }

        /// <summary>
        /// Builds the in-memory PDF document so tests can inspect its page count
        /// without writing a file.
        /// </summary>
        internal static PdfDocument Build(VerificationReportModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var document = new PdfDocument();
            document.Info.Title = "MRU Verification Report - " + Dash(model.ProjectName);
            document.Info.Author = Dash(model.Operator);

            using (var canvas = new ReportCanvas(document))
            {
                WriteCover(canvas, model);
                PdfPage contentsPage = canvas.StartPageWithResult();
                WriteExecutiveDashboard(canvas, model);

                canvas.StartSection("1. Session Overview",
                    "Overview of the verification session, capture timing, operator details, data volume and correction status.");
                WriteOverview(canvas, model);

                canvas.StartSection("2. Applied Corrections",
                    "Recommended and applied corrections for pitch, roll and heave, together with their implementation status.");
                WriteCorrections(canvas, model);

                canvas.StartSection("3. Axis Detail",
                    "Detailed per-axis statistics, comparison charts and correction values for the verification dataset.");
                WriteAxisDetails(canvas, model);

                if (model.HasData)
                {
                    canvas.StartSection("4. Correlation and Latency",
                        "Correlation strength and timing-offset metrics comparing reference and vessel motion signals.");
                    WriteCorrelationAndLatency(canvas, model);
                }

                canvas.StartSection("5. Compliance Assessment",
                    "Assessment of sample count, capture duration and signal quality against recommended criteria.");
                WriteCompliance(canvas, model);

                canvas.StartSection("6. Scope and Objective",
                    "Verification scope, intended outcome and applicable references defining this report's purpose.");
                WriteScope(canvas, model);

                canvas.StartSection("7. Data Processing Methodology",
                    "Summary of synchronization, filtering, statistical treatment and comparison methods.");
                WriteMethodology(canvas, model);

                canvas.StartSection("8. Equipment",
                    "Equipment used for the verification, including the vessel-installed MRU, reference unit and supporting hardware.");
                WriteEquipment(canvas, model);

                canvas.StartSection("9. Test Setup",
                    "Installation arrangement, sensor geometry, synchronization and data acquisition settings.");
                WriteTestSetup(canvas, model);

                canvas.StartSection("10. Test Conditions",
                    "Environmental and operational conditions recorded during the verification.");
                WriteTestConditions(canvas, model);

                canvas.StartSection("11. Metric Definitions",
                    "A quick reference guide for interpreting the metrics in this report.");
                WriteGlossary(canvas, model);

                canvas.StartSection("12. Observations",
                    "Additional notes that may help explain conditions, limitations or noteworthy aspects of the capture.");
                canvas.DrawParagraph(string.IsNullOrWhiteSpace(model.Metadata?.Observations)
                    ? "No additional observations were recorded."
                    : model.Metadata.Observations);

                canvas.StartSection("13. Appendices",
                    "Supporting reference material and explanatory notes that complement the verification results.");
                canvas.DrawParagraph(string.IsNullOrWhiteSpace(model.Metadata?.AppendixNotes)
                    ? "No additional appendix material was provided."
                    : model.Metadata.AppendixNotes);

                canvas.Complete();
                WriteTableOfContents(contentsPage, canvas.SectionPages);
                AddPageNumbers(document);
            }

            return document;
        }

        private static void WriteCover(ReportCanvas canvas, VerificationReportModel model)
        {
            canvas.StartPage();

            if (model.LogoPng != null)
                canvas.DrawImage(model.LogoPng, 180, 42);

            if (model.CoverBannerPng != null)
            {
                canvas.DrawImage(model.CoverBannerPng, ReportCanvas.ContentWidth, 490);
                return;
            }

            canvas.DrawHeading("MOTION REFERENCE UNIT", 22);
            canvas.DrawHeading("VERIFICATION REPORT", 22);
            canvas.DrawParagraph(Dash(model.ProjectName), 13, bold: true, color: ColorMuted);
            canvas.DrawKeyValues(new[]
            {
                Pair("Vessel", model.VesselName),
                Pair("Operator / surveyor", model.Operator),
                Pair("Location", model.Location),
                Pair("Capture start", model.StartTime),
                Pair("Capture end", model.EndTime),
                Pair("Duration", model.Duration),
                Pair("Report generated (UTC)", model.GeneratedUtc.ToString("yyyy-MM-dd HH:mm", Ci)),
            });
        }

        private static void WriteExecutiveDashboard(ReportCanvas canvas, VerificationReportModel model)
        {
            canvas.StartPage();
            canvas.DrawHeading("Executive Dashboard");
            canvas.DrawParagraph(
                "Summary of verification outcome, correction status and key data-quality indicators for a rapid executive review.",
                10, color: ColorMuted);

            canvas.DrawImage(model.ExecutiveDashboardPng, ReportCanvas.ContentWidth, 270);
            if (!model.HasData)
            {
                canvas.DrawParagraph(
                    "No measurement data was captured. Acquire reference and vessel motion data, then regenerate this report.");
                return;
            }

            canvas.DrawSubheading("Key findings");
            canvas.DrawBullet("Verification completed successfully.");
            canvas.DrawBullet(model.HasCorrectionApplied
                ? "Recommended corrections have been applied."
                : "Recommended corrections are ready to apply to the vessel unit.");
            canvas.DrawBullet(string.Format(Ci, "{0:N0} samples collected.", model.SampleCount));
            if (TimeSpan.TryParse(model.Duration, out TimeSpan duration))
                canvas.DrawBullet(string.Format(Ci, "{0:F1} minute capture duration.", duration.TotalMinutes));

            double worstOutlier = model.WorstOutlierPercent;
            if (!double.IsNaN(worstOutlier))
                canvas.DrawBullet(worstOutlier <= VerificationAssessment.OutlierAcceptablePercent
                    ? "Data quality met the recommended outlier target."
                    : string.Format(Ci, "Maximum outlier rate: {0:F1}%.", worstOutlier));
        }

        private static void WriteOverview(ReportCanvas canvas, VerificationReportModel model)
        {
            canvas.DrawImage(model.SessionOverviewPng, ReportCanvas.ContentWidth, 175);
            canvas.DrawKeyValues(new[]
            {
                Pair("Project / reference", model.ProjectName),
                Pair("Vessel", model.VesselName),
                Pair("Operator", model.Operator),
                Pair("Location", model.Location),
                Pair("Sensor setup", model.InputSetup),
                Pair("Capture start", model.StartTime),
                Pair("Capture end", model.EndTime),
                Pair("Duration", model.Duration),
                Pair("Samples averaged", model.SampleCount.ToString("N0", Ci)),
                Pair("Correction", model.HasCorrectionApplied
                    ? "Applied" + (string.IsNullOrWhiteSpace(model.CorrectionAppliedAt) ? "" : " (" + model.CorrectionAppliedAt + ")")
                    : "Not yet applied"),
            });

            if (!string.IsNullOrWhiteSpace(model.Comments))
            {
                canvas.DrawSubheading("Notes");
                canvas.DrawParagraph(model.Comments);
            }
        }

        private static void WriteCorrections(ReportCanvas canvas, VerificationReportModel model)
        {
            canvas.DrawImage(model.CorrectionCardsPng, ReportCanvas.ContentWidth, 260);
            canvas.DrawImage(model.BulletChartsPng, ReportCanvas.ContentWidth, 155);

            var rows = new List<string[]>();
            foreach (VerificationAxisKind axis in AllAxes())
            {
                double recommended = model.RecommendedCorrection(axis);
                double applied = model.AppliedCorrection(axis);
                string status = model.HasCorrectionApplied
                    ? (Math.Abs(applied - recommended) < 1e-6 ? "Applied as recommended" : "Applied (adjusted)")
                    : "Pending";
                rows.Add(new[]
                {
                    model.AxisTitle(axis),
                    Format(recommended, model.Unit(axis)),
                    model.HasCorrectionApplied ? Format(applied, model.Unit(axis)) : "-",
                    status,
                });
            }

            canvas.DrawTable(
                new[] { "Axis", "Recommended", "Applied", "Status" },
                rows,
                new[] { 1.0, 1.2, 1.0, 1.6 });
        }

        private static void WriteAxisDetails(ReportCanvas canvas, VerificationReportModel model)
        {
            canvas.DrawImage(model.DeviationChartPng, ReportCanvas.ContentWidth, 245);
            canvas.DrawImage(model.MeansChartPng, ReportCanvas.ContentWidth, 245);

            foreach (VerificationAxisKind axis in AllAxes())
            {
                canvas.DrawSubheadingAndImage(
                    model.AxisTitle(axis).ToUpperInvariant(),
                    model.AxisSummaryPng(axis),
                    ReportCanvas.ContentWidth,
                    170);

                AxisStatistics reference = model.RefStats(axis);
                AxisStatistics test = model.TestStats(axis);
                AxisStatistics deviation = model.DevStats(axis);
                string unit = model.Unit(axis);
                canvas.DrawTable(
                    new[] { "Metric", "Reference", "Vessel", "Deviation" },
                    new[]
                    {
                        new[] { "Mean", Stat(reference?.Mean, unit), Stat(test?.Mean, unit), Stat(deviation?.Mean, unit) },
                        new[] { "Std. dev (sigma)", Stat(reference?.StdDev, unit), Stat(test?.StdDev, unit), Stat(deviation?.StdDev, unit) },
                        new[] { "Minimum", Stat(reference?.Min, unit), Stat(test?.Min, unit), Stat(deviation?.Min, unit) },
                        new[] { "Maximum", Stat(reference?.Max, unit), Stat(test?.Max, unit), Stat(deviation?.Max, unit) },
                        new[] { "RMS", Stat(reference?.Rms, unit), Stat(test?.Rms, unit), Stat(deviation?.Rms, unit) },
                        new[] { "Outliers", Percent(reference?.OutlierPercent), Percent(test?.OutlierPercent), Percent(deviation?.OutlierPercent) },
                        new[]
                        {
                            "Samples",
                            (reference?.SampleCount ?? 0).ToString("N0", Ci),
                            (test?.SampleCount ?? 0).ToString("N0", Ci),
                            (deviation?.SampleCount ?? 0).ToString("N0", Ci),
                        },
                    },
                    new[] { 1.3, 1.0, 1.0, 1.0 });
            }
        }

        private static void WriteCorrelationAndLatency(ReportCanvas canvas, VerificationReportModel model)
        {
            canvas.DrawParagraph("Correlation: 1.00 indicates perfect agreement. Positive latency means the vessel signal lags the reference.");
            canvas.DrawImage(model.CorrelationBarsPng, ReportCanvas.ContentWidth, 210);

            var rows = new List<string[]>();
            foreach (VerificationAxisKind axis in AllAxes())
            {
                PairedSeriesStatistics pair = model.PairStats(axis);
                rows.Add(new[]
                {
                    model.AxisTitle(axis),
                    Correlation(pair?.Correlation),
                    Latency(pair?.EstimatedLatencySeconds),
                });
            }
            canvas.DrawTable(new[] { "Axis", "Correlation", "Estimated latency" }, rows, new[] { 1.0, 1.0, 1.5 });

            canvas.DrawSubheading("Interpretation");
            canvas.DrawParagraph(
                "Correlation describes how similarly the vessel and reference MRU signals vary over time. Values close to +1.0 indicate strong agreement in signal behaviour; values closer to 0 indicate weaker similarity. Latency represents the estimated timing offset between the two signals. These metrics are diagnostic and are not, on their own, indicators of verification quality.");
        }

        private static void WriteCompliance(ReportCanvas canvas, VerificationReportModel model)
        {
            canvas.DrawImage(model.ComplianceSummaryBannerPng, ReportCanvas.ContentWidth, 125);
            canvas.DrawImage(model.ComplianceScorecardsPng, ReportCanvas.ContentWidth, 130);
            canvas.DrawImage(model.ConfidencePanelPng, ReportCanvas.ContentWidth, 170);

            double minutes = TimeSpan.TryParse(model.Duration, out TimeSpan duration)
                ? duration.TotalMinutes
                : 0;
            double worstOutlier = model.WorstOutlierPercent;
            var rows = new[]
            {
                new[]
                {
                    "Samples",
                    string.Format(Ci, ">= {0:N0}", VerificationAssessment.MinSamplesAcceptable),
                    string.Format(Ci, ">= {0:N0}", VerificationAssessment.MinSamplesGood),
                    model.SampleCount > 0 ? model.SampleCount.ToString("N0", Ci) : "-",
                    model.SampleCount >= VerificationAssessment.MinSamplesGood ? "Good" :
                        model.SampleCount >= VerificationAssessment.MinSamplesAcceptable ? "Acceptable" :
                        model.SampleCount > 0 ? "Insufficient" : "No data",
                },
                new[]
                {
                    "Capture duration",
                    string.Format(Ci, ">= {0:F0} min", MinDurationAcceptableMinutes),
                    string.Format(Ci, ">= {0:F0} min", MinDurationRecommendedMinutes),
                    minutes > 0 ? string.Format(Ci, "{0:F1} min", minutes) : "-",
                    minutes >= MinDurationRecommendedMinutes ? "Good" :
                        minutes >= MinDurationAcceptableMinutes ? "Acceptable" :
                        minutes > 0 ? "Insufficient" : "No data",
                },
                new[]
                {
                    "Max outlier",
                    string.Format(Ci, "<= {0:F0}%", VerificationAssessment.OutlierAttentionPercent),
                    string.Format(Ci, "<= {0:F0}%", VerificationAssessment.OutlierAcceptablePercent),
                    double.IsNaN(worstOutlier) ? "-" : string.Format(Ci, "{0:F1}%", worstOutlier),
                    double.IsNaN(worstOutlier) ? "No data" :
                        worstOutlier <= VerificationAssessment.OutlierAcceptablePercent ? "Good" :
                        worstOutlier <= VerificationAssessment.OutlierAttentionPercent ? "Acceptable" : "Too noisy",
                },
            };
            canvas.DrawTable(
                new[] { "Criterion", "Acceptable (min)", "Good (target)", "Measured", "Status" },
                rows,
                new[] { 1.15, 1.0, 1.0, 0.9, 0.85 });

            DrawOptionalNarrative(canvas, "Assessment", model.Metadata?.AcceptanceCriteriaDiscussion);
            DrawOptionalNarrative(canvas, "Manufacturer specifications", model.Metadata?.ManufacturerSpecifications);
        }

        private static void WriteScope(ReportCanvas canvas, VerificationReportModel model)
        {
            MruReportMetadata metadata = model.Metadata ?? new MruReportMetadata();
            canvas.DrawParagraph(string.IsNullOrWhiteSpace(metadata.TestObjective)
                ? "Verification of the vessel-installed MRU against a calibrated reference unit. Corrections for pitch, roll and heave are determined and applied."
                : metadata.TestObjective);
            DrawOptionalNarrative(canvas, "Applicable standards and references", metadata.ApplicableStandards);
        }

        private static void WriteMethodology(ReportCanvas canvas, VerificationReportModel model)
        {
            canvas.DrawParagraph(
                "Reference and vessel channels are time-aligned sample-by-sample. Per-sample deviation (vessel minus reference) is computed on each axis; the mean deviation is the recommended correction. Agreement is quantified by Pearson correlation and timing offset by cross-correlation.");

            MruReportMetadata metadata = model.Metadata ?? new MruReportMetadata();
            DrawOptionalNarrative(canvas, "Time synchronization", metadata.TimeSynchronizationNotes);
            DrawOptionalNarrative(canvas, "Filtering", metadata.FilteringNotes);
            DrawOptionalNarrative(canvas, "Additional processing notes", metadata.DataProcessingNotes);
        }

        private static void WriteEquipment(ReportCanvas canvas, VerificationReportModel model)
        {
            MruReportMetadata metadata = model.Metadata ?? new MruReportMetadata();
            canvas.DrawSubheading("MRU under test (vessel-installed)");
            canvas.DrawKeyValues(new[]
            {
                Pair("Manufacturer", metadata.DutManufacturer),
                Pair("Model", metadata.DutModel),
                Pair("Serial number", metadata.DutSerialNumber),
                Pair("Firmware version", metadata.DutFirmwareVersion),
            });
            canvas.DrawSubheading("Reference MRU");
            canvas.DrawKeyValues(new[]
            {
                Pair("Manufacturer", metadata.ReferenceManufacturer),
                Pair("Model", metadata.ReferenceModel),
                Pair("Serial number", metadata.ReferenceSerialNumber),
                Pair("Firmware version", metadata.ReferenceFirmwareVersion),
                Pair("Calibration date", metadata.ReferenceCalibrationDate?.ToString("yyyy-MM-dd", Ci)),
                Pair("Calibration certificate", metadata.ReferenceCalibrationCertificateNumber),
            });
            canvas.DrawSubheading("LiDAR");
            canvas.DrawKeyValues(new[]
            {
                Pair("User", metadata.LidarUser),
                Pair("Manufacturer", metadata.LidarManufacturer),
                Pair("Model", metadata.LidarModel),
                Pair("Serial number", metadata.LidarSerialNumber),
                Pair("Firmware version", metadata.LidarFirmwareVersion),
            });
            DrawOptionalNarrative(canvas, "Additional equipment", metadata.AdditionalEquipment);
        }

        private static void WriteTestSetup(ReportCanvas canvas, VerificationReportModel model)
        {
            MruReportMetadata metadata = model.Metadata ?? new MruReportMetadata();
            canvas.DrawKeyValues(new[]
            {
                Pair("Vessel MRU installation location", metadata.DutInstallationLocation),
                Pair("Reference MRU installation location", metadata.ReferenceInstallationLocation),
                Pair("Mounting arrangement", metadata.MountingArrangement),
                Pair("Coordinate system", metadata.CoordinateSystem),
                Pair("Sensor separation", metadata.SensorSeparation),
                Pair("Data acquisition method", metadata.DataAcquisitionMethod),
                Pair("Synchronization method", metadata.SynchronizationMethod),
                Pair("Sample rate", metadata.SampleRateHz.HasValue ? string.Format(Ci, "{0:0.###} Hz", metadata.SampleRateHz.Value) : null),
                Pair("Logging configuration", metadata.LoggingConfiguration),
                Pair("Sensor setup (logged)", model.InputSetup),
            });
        }

        private static void WriteTestConditions(ReportCanvas canvas, VerificationReportModel model)
        {
            MruReportMetadata metadata = model.Metadata ?? new MruReportMetadata();
            canvas.DrawKeyValues(new[]
            {
                Pair("Loading condition", metadata.LoadingCondition),
                Pair("Vessel speed", metadata.VesselSpeed),
                Pair("Operational mode", metadata.OperationalMode),
                Pair("Sea state", metadata.SeaState),
                Pair("Wind conditions", metadata.WindConditions),
                Pair("Wave conditions", metadata.WaveConditions),
                Pair("Current conditions", metadata.CurrentConditions),
            });
            DrawOptionalNarrative(canvas, "Environmental notes", metadata.EnvironmentalNotes);
        }

        private static void WriteGlossary(ReportCanvas canvas, VerificationReportModel model)
        {
            var terms = new[]
            {
                Pair("Reference MRU", "The trusted baseline motion sensor. Its mean reading is used as the reference value for comparisons."),
                Pair("Vessel MRU", "The unit being verified. Its mean reading is compared against the reference MRU."),
                Pair("Deviation", "Vessel minus reference on each axis. This value is used to derive the recommended correction."),
                Pair("Std. dev. (sigma)", "How much the signal varies around its mean. Lower values indicate a more stable signal."),
                Pair("Min / Max", "The most extreme values observed during capture. Large spreads may indicate disturbances."),
                Pair("RMS", "Root-mean-square magnitude of the signal, useful for assessing average motion energy."),
                Pair("Outliers", "Percentage of samples flagged as anomalous by Tukey's 1.5 x IQR rule."),
                Pair("Samples", "Number of data points included in the statistical calculations."),
            };
            canvas.DrawKeyValues(terms);
            canvas.DrawSubheading("Can the deviation be trusted?");
            canvas.DrawParagraph(string.Format(Ci,
                "Trustworthiness depends on input data quality, not the deviation value. Samples: >= {0:N0} usable, >= {1:N0} good. Outliers: <= {2:F0}% good, <= {3:F0}% usable, above {3:F0}% too noisy.",
                VerificationAssessment.MinSamplesAcceptable,
                VerificationAssessment.MinSamplesGood,
                VerificationAssessment.OutlierAcceptablePercent,
                VerificationAssessment.OutlierAttentionPercent));
        }

        private static void DrawOptionalNarrative(ReportCanvas canvas, string title, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            canvas.DrawSubheading(title);
            canvas.DrawParagraph(value);
        }

        private static void WriteTableOfContents(PdfPage page, IDictionary<string, int> sectionPages)
        {
            using (XGraphics graphics = XGraphics.FromPdfPage(page))
            {
                double y = ReportCanvas.TopMargin;
                DrawText(graphics, "Contents", 22, bold: true, ColorHeading,
                    ReportCanvas.LeftMargin, y, ReportCanvas.ContentWidth);
                y += 42;

                foreach (KeyValuePair<string, int> section in sectionPages)
                {
                    DrawText(graphics, section.Key, 11, bold: false, ColorText,
                        ReportCanvas.LeftMargin, y, ReportCanvas.ContentWidth - 40);
                    DrawText(graphics, section.Value.ToString(Ci), 11, bold: true, ColorHeading,
                        page.Width.Point - ReportCanvas.RightMargin - 30, y, 30,
                        XStringFormats.TopRight);
                    y += 25;
                }
            }
        }

        private static void AddPageNumbers(PdfDocument document)
        {
            for (int i = 0; i < document.Pages.Count; i++)
            {
                PdfPage page = document.Pages[i];
                using (XGraphics graphics = XGraphics.FromPdfPage(page))
                {
                    DrawText(graphics, "- " + (i + 1).ToString(Ci) + " -", 8, false, ColorText,
                        0, page.Height.Point - 42, page.Width.Point, XStringFormats.BottomCenter);
                }
            }
        }

        private static void DrawPageChrome(XGraphics graphics, PdfPage page)
        {
            if (SesBackgroundPng != null)
                DrawImageBytes(graphics, SesBackgroundPng, 0, 0, page.Width.Point, page.Height.Point);

            const double headerHeight = 62;
            const double footerHeight = 48;
            if (SesHeaderPng != null)
                DrawImageBytes(graphics, SesHeaderPng, 0, 0, page.Width.Point, headerHeight);
            if (SesFooterPng != null)
                DrawImageBytes(graphics, SesFooterPng, 0, page.Height.Point - footerHeight, page.Width.Point, footerHeight);

            DrawText(graphics, "MRU Verification Report", 14, true, ColorHeading,
                ReportCanvas.LeftMargin, 22, 300);
        }

        private static void DrawImageBytes(XGraphics graphics, byte[] bytes, double x, double y, double width, double height)
        {
            using (var stream = new MemoryStream(bytes, writable: false))
            using (XImage image = XImage.FromStream(stream))
                graphics.DrawImage(image, x, y, width, height);
        }

        private static void DrawText(
            XGraphics graphics,
            string text,
            double size,
            bool bold,
            XColor color,
            double x,
            double y,
            double width,
            XStringFormat format = null)
        {
            var font = new XFont("Arial", size, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular);
            graphics.DrawString(text ?? string.Empty, font, new XSolidBrush(color),
                new XRect(x, y, width, size * 1.6), format ?? XStringFormats.TopLeft);
        }

        private static byte[] LoadResourceBytes(string packUri)
        {
            try
            {
                var info = Application.GetResourceStream(new Uri(packUri));
                if (info == null)
                    return null;
                using (info.Stream)
                using (var memory = new MemoryStream())
                {
                    info.Stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
            catch (IOException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        private static KeyValuePair<string, string> Pair(string key, string value) =>
            new KeyValuePair<string, string>(key, value);

        private static IEnumerable<VerificationAxisKind> AllAxes()
        {
            yield return VerificationAxisKind.Pitch;
            yield return VerificationAxisKind.Roll;
            yield return VerificationAxisKind.Heave;
        }

        private static string Dash(string value) =>
            string.IsNullOrWhiteSpace(value) ? "-" : value;

        private static string Format(double value, string unit) =>
            double.IsNaN(value) ? "-" : string.Format(Ci, "{0:+0.000;-0.000;0.000} {1}", value, unit);

        private static string Stat(double? value, string unit) =>
            !value.HasValue || double.IsNaN(value.Value)
                ? "-"
                : string.Format(Ci, "{0:F3} {1}", value.Value, unit);

        private static string Percent(double? value) =>
            !value.HasValue || double.IsNaN(value.Value)
                ? "-"
                : string.Format(Ci, "{0:F1}%", value.Value);

        private static string Correlation(double? value) =>
            !value.HasValue || double.IsNaN(value.Value)
                ? "-"
                : string.Format(Ci, "{0:F3}", value.Value);

        private static string Latency(double? seconds) =>
            !seconds.HasValue || double.IsNaN(seconds.Value)
                ? "-"
                : string.Format(Ci, "{0:+0.000;-0.000;0.000} s", seconds.Value);

        internal sealed class ReportCanvas : IDisposable
        {
            internal const double LeftMargin = 48;
            internal const double RightMargin = 48;
            internal const double TopMargin = 91;
            private const double BottomMargin = 76;
            internal const double ContentWidth = 595.28 - LeftMargin - RightMargin;

            private readonly PdfDocument _document;
            private XGraphics _graphics;

            internal double Y { get; private set; }
            internal IDictionary<string, int> SectionPages { get; } =
                new Dictionary<string, int>(StringComparer.Ordinal);

            internal ReportCanvas(PdfDocument document)
            {
                _document = document;
            }

            internal void StartPage()
            {
                _graphics?.Dispose();
                PdfPage page = _document.AddPage();
                page.Size = PageSize.A4;
                _graphics = XGraphics.FromPdfPage(page);
                DrawPageChrome(_graphics, page);
                Y = TopMargin;
            }

            internal PdfPage StartPageWithResult()
            {
                StartPage();
                return _document.Pages[_document.Pages.Count - 1];
            }

            internal void StartSection(string title, string description)
            {
                StartPage();
                SectionPages[title] = _document.Pages.Count;
                DrawHeading(title);
                DrawParagraph(description, 10, color: ColorMuted);
            }

            internal void Complete()
            {
                _graphics?.Dispose();
                _graphics = null;
            }

            internal void DrawHeading(string text, double size = 20)
            {
                EnsureSpace(size * 2.4);
                DrawText(_graphics, text, size, true, ColorHeading, LeftMargin, Y, ContentWidth);
                Y += size * 1.8;
                DrawLine(ColorAccent, 3);
                Y += 10;
            }

            internal void DrawSubheading(string text)
            {
                EnsureSpace(28);
                DrawText(_graphics, text, 12, true, ColorHeading, LeftMargin, Y, ContentWidth);
                Y += 21;
            }

            internal void DrawParagraph(string text, double size = 10.5, bool bold = false, XColor? color = null)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    Y += 5;
                    return;
                }

                var font = new XFont("Arial", size, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular);
                foreach (string line in WrapText(text, font, ContentWidth))
                {
                    double lineHeight = size * 1.45;
                    EnsureSpace(lineHeight);
                    _graphics.DrawString(line, font, new XSolidBrush(color ?? ColorText),
                        new XRect(LeftMargin, Y, ContentWidth, lineHeight), XStringFormats.TopLeft);
                    Y += lineHeight;
                }
                Y += 7;
            }

            internal void DrawBullet(string text)
            {
                DrawParagraph("- " + text, 10.5);
            }

            internal void DrawKeyValues(IEnumerable<KeyValuePair<string, string>> rows)
            {
                foreach (KeyValuePair<string, string> row in rows)
                {
                    // Keep the label and the first line of its value together.
                    EnsureSpace(14 + 10 * 1.45);
                    DrawText(_graphics, row.Key, 9.5, true, ColorHeading, LeftMargin, Y, ContentWidth);
                    Y += 14;
                    DrawParagraph(Dash(row.Value), 10);
                }
            }

            internal void DrawTable(string[] headers, IEnumerable<string[]> rows, double[] weights)
            {
                double totalWeight = 0;
                foreach (double weight in weights)
                    totalWeight += weight;
                double[] widths = new double[weights.Length];
                for (int i = 0; i < weights.Length; i++)
                    widths[i] = ContentWidth * weights[i] / totalWeight;

                DrawTableRow(headers, widths, header: true, rowIndex: 0);
                int rowIndex = 0;
                foreach (string[] row in rows)
                    DrawTableRow(row, widths, header: false, rowIndex: rowIndex++);
                Y += 9;
            }

            internal void DrawImage(byte[] bytes, double maxWidth, double maxHeight)
            {
                if (bytes == null || bytes.Length == 0)
                    return;

                using (var stream = new MemoryStream(bytes, writable: false))
                using (XImage image = XImage.FromStream(stream))
                {
                    double ratio = (double)image.PixelHeight / image.PixelWidth;
                    double width = Math.Min(maxWidth, ContentWidth);
                    double height = width * ratio;
                    if (height > maxHeight)
                    {
                        height = maxHeight;
                        width = height / ratio;
                    }

                    EnsureSpace(height + 12);
                    _graphics.DrawImage(image, LeftMargin + (ContentWidth - width) / 2, Y, width, height);
                    Y += height + 12;
                }
            }

            internal void DrawSubheadingAndImage(string text, byte[] bytes, double maxWidth, double maxHeight)
            {
                EnsureSpace(28 + (bytes != null && bytes.Length > 0 ? maxHeight + 12 : 0));
                DrawSubheading(text);
                DrawImage(bytes, maxWidth, maxHeight);
            }

            internal void EnsureSpace(double height)
            {
                if (Y + height <= PageHeight - BottomMargin)
                    return;
                StartPage();
            }

            private void DrawTableRow(string[] cells, double[] widths, bool header, int rowIndex)
            {
                var font = new XFont("Arial", header ? 8.5 : 8.5,
                    header ? XFontStyleEx.Bold : XFontStyleEx.Regular);
                var cellLines = new List<string[]>();
                double rowHeight = header ? 25 : 22;
                for (int i = 0; i < widths.Length; i++)
                {
                    string[] lines = WrapText(i < cells.Length ? cells[i] : string.Empty, font, widths[i] - 12).ToArray();
                    cellLines.Add(lines);
                    rowHeight = Math.Max(rowHeight, lines.Length * 12 + 10);
                }

                EnsureSpace(rowHeight);
                double x = LeftMargin;
                for (int i = 0; i < widths.Length; i++)
                {
                    XRect rect = new XRect(x, Y, widths[i], rowHeight);
                    XColor fill = header ? ColorHeading : (rowIndex % 2 == 0 ? ColorRowAlt : XColors.White);
                    _graphics.DrawRectangle(new XSolidBrush(fill), rect);
                    _graphics.DrawRectangle(new XPen(ColorBorder, 0.5), rect);
                    double textY = Y + 5;
                    XColor textColor = header ? XColors.White : ColorText;
                    foreach (string line in cellLines[i])
                    {
                        _graphics.DrawString(line, font, new XSolidBrush(textColor),
                            new XRect(x + 6, textY, widths[i] - 12, 12), XStringFormats.TopLeft);
                        textY += 12;
                    }
                    x += widths[i];
                }
                Y += rowHeight;
            }

            private void DrawLine(XColor color, double thickness)
            {
                _graphics.DrawLine(new XPen(color, thickness),
                    LeftMargin, Y, LeftMargin + ContentWidth, Y);
            }

            private IEnumerable<string> WrapText(string text, XFont font, double width)
            {
                foreach (string paragraph in (text ?? string.Empty).Replace("\r", string.Empty).Split('\n'))
                {
                    string line = string.Empty;
                    foreach (string word in paragraph.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (_graphics.MeasureString(word, font).Width > width)
                        {
                            if (line.Length > 0)
                            {
                                yield return line;
                                line = string.Empty;
                            }

                            var segment = new StringBuilder();
                            TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(word);
                            while (elements.MoveNext())
                            {
                                string element = elements.GetTextElement();
                                string segmentCandidate = segment.ToString() + element;
                                if (segment.Length > 0 && _graphics.MeasureString(segmentCandidate, font).Width > width)
                                {
                                    yield return segment.ToString();
                                    segment.Clear();
                                }
                                segment.Append(element);
                            }

                            line = segment.ToString();
                            continue;
                        }

                        string candidate = line.Length == 0 ? word : line + " " + word;
                        if (line.Length > 0 && _graphics.MeasureString(candidate, font).Width > width)
                        {
                            yield return line;
                            line = word;
                        }
                        else
                        {
                            line = candidate;
                        }
                    }
                    yield return line;
                }
            }

            private double PageHeight => _graphics.PageSize.Height;

            public void Dispose()
            {
                _graphics?.Dispose();
                _graphics = null;
            }
        }
    }
}
