using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS.Models;
using MVS.Services;
using MVS.Services.Reporting;
using MVSTests.TestInfrastructure;

namespace MVSTests.Services
{
    /// <summary>
    /// Tests for the verification reporting layer: the report model accessors,
    /// the deterministic chart bitmaps, and the RadPdfProcessing PDF export.
    /// PDF/chart building touches Telerik/GDI+ objects, so those tests run on an
    /// STA thread via <see cref="StaTestHelper"/>.
    /// </summary>
    [TestClass]
    public class VerificationPdfReportExporterTests
    {
        private static VerificationReportModel SampleModel(bool withData = true)
        {
            var model = new VerificationReportModel
            {
                ProjectName = "Test Vessel",
                Comments = "Sea trial off Bergen.",
                InputSetup = "Reference + Vessel MRU",
                StartTime = "2025-01-01 08:00:00",
                EndTime = "2025-01-01 09:00:00",
                Duration = "01:00:00",
                HasCorrectionApplied = true,
                CorrectionAppliedAt = "2025-01-01 09:05:00",
                RecommendedCorrectionPitch = 0.42,
                RecommendedCorrectionRoll = -0.15,
                RecommendedCorrectionHeave = 0.03,
                AppliedCorrectionPitch = 0.42,
                AppliedCorrectionRoll = -0.15,
                AppliedCorrectionHeave = 0.03,
            };

            if (withData)
            {
                model.RefPitch = Stat(400, 1.0, 0.2);
                model.TestPitch = Stat(400, 1.4, 0.25);
                model.DevPitch = Stat(400, 0.42, 0.1);
                model.RefRoll = Stat(400, 0.5, 0.2);
                model.TestRoll = Stat(400, 0.35, 0.22);
                model.DevRoll = Stat(400, -0.15, 0.09);
                model.RefHeave = Stat(400, 0.10, 0.05);
                model.TestHeave = Stat(400, 0.13, 0.06);
                model.DevHeave = Stat(400, 0.03, 0.02);
            }

            return model;
        }

        private static AxisStatistics Stat(int count, double mean, double std)
        {
            return new AxisStatistics
            {
                SampleCount = count,
                Mean = mean,
                StdDev = std,
                Min = mean - std,
                Max = mean + std,
                Rms = Math.Abs(mean),
                OutlierPercent = 1.0,
            };
        }

        // ── Model ──

        [TestMethod]
        public void Model_PerAxisAccessors_MapToCorrectAxis()
        {
            var model = SampleModel();

            Assert.AreEqual(model.DevPitch, model.DevStats(VerificationAxisKind.Pitch));
            Assert.AreEqual(model.DevRoll, model.DevStats(VerificationAxisKind.Roll));
            Assert.AreEqual(model.DevHeave, model.DevStats(VerificationAxisKind.Heave));

            Assert.AreEqual(model.RefPitch, model.RefStats(VerificationAxisKind.Pitch));
            Assert.AreEqual(model.TestHeave, model.TestStats(VerificationAxisKind.Heave));

            Assert.AreEqual(0.42, model.RecommendedCorrection(VerificationAxisKind.Pitch), 1e-9);
            Assert.AreEqual(-0.15, model.AppliedCorrection(VerificationAxisKind.Roll), 1e-9);
        }

        [TestMethod]
        public void Model_Units_AreDegreesForAnglesMetresForHeave()
        {
            var model = SampleModel();

            Assert.AreEqual("\u00B0", model.Unit(VerificationAxisKind.Pitch));
            Assert.AreEqual("\u00B0", model.Unit(VerificationAxisKind.Roll));
            Assert.AreEqual("m", model.Unit(VerificationAxisKind.Heave));
        }

        [TestMethod]
        public void Model_HasData_ReflectsSampleCount()
        {
            Assert.IsTrue(SampleModel(withData: true).HasData);
            Assert.IsFalse(SampleModel(withData: false).HasData);
        }

        // ── Charts ──

        [TestMethod]
        public void ChartRenderer_ProducesValidPngBytes()
        {
            StaTestHelper.Run(() =>
            {
                var model = SampleModel();
                byte[] deviation = ReportChartRenderer.RenderDeviationChart(model);
                byte[] means = ReportChartRenderer.RenderMeansChart(model);

                AssertIsPng(deviation);
                AssertIsPng(means);
            });
        }

        // ── PDF ──

        [TestMethod]
        public void Build_WithData_ProducesAtLeastOnePage()
        {
            StaTestHelper.Run(() =>
            {
                var model = SampleModel();
                model.DeviationChartPng = ReportChartRenderer.RenderDeviationChart(model);
                model.MeansChartPng = ReportChartRenderer.RenderMeansChart(model);

                var document = VerificationPdfReportExporter.Build(model);

                Assert.IsNotNull(document);
                Assert.IsTrue(document.Pages.Count >= 1,
                    "Expected the report to contain at least one page.");
            });
        }

        [TestMethod]
        public void Export_WritesPdfSignatureToStream()
        {
            StaTestHelper.Run(() =>
            {
                var model = SampleModel();

                using (var ms = new MemoryStream())
                {
                    VerificationPdfReportExporter.Export(ms, model);
                    byte[] bytes = ms.ToArray();

                    Assert.IsTrue(bytes.Length > 0, "PDF stream should not be empty.");
                    // Every PDF file starts with the "%PDF" magic number.
                    Assert.AreEqual((byte)'%', bytes[0]);
                    Assert.AreEqual((byte)'P', bytes[1]);
                    Assert.AreEqual((byte)'D', bytes[2]);
                    Assert.AreEqual((byte)'F', bytes[3]);
                }
            });
        }

        [TestMethod]
        public void Export_WithoutData_StillProducesValidPdf()
        {
            StaTestHelper.Run(() =>
            {
                var model = SampleModel(withData: false);

                using (var ms = new MemoryStream())
                {
                    VerificationPdfReportExporter.Export(ms, model);
                    Assert.IsTrue(ms.Length > 0, "Empty-data report should still export a PDF.");
                }
            });
        }

        private static void AssertIsPng(byte[] bytes)
        {
            Assert.IsNotNull(bytes);
            Assert.IsTrue(bytes.Length > 8, "PNG output should not be empty.");
            // PNG signature: 89 50 4E 47 0D 0A 1A 0A
            Assert.AreEqual(0x89, bytes[0]);
            Assert.AreEqual(0x50, bytes[1]);
            Assert.AreEqual(0x4E, bytes[2]);
            Assert.AreEqual(0x47, bytes[3]);
        }
    }
}
