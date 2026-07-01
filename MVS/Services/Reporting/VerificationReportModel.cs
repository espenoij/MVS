using System;
using MVS.Models;

namespace MVS.Services.Reporting
{
    /// <summary>
    /// Plain, UI-independent snapshot of everything a verification report needs:
    /// project/session metadata, the per-axis statistics that were calculated,
    /// the recommended and applied corrections, and (optionally) pre-rendered
    /// chart graphics.
    ///
    /// Keeping this a simple POCO lets the PDF builder be unit-tested without a
    /// running WPF application, a database, or a live <see cref="ProjectVM"/>.
    /// </summary>
    public class VerificationReportModel
    {
        // ----- Project / session metadata (already formatted for display) -----
        public string ProjectName { get; set; }
        public string Comments { get; set; }
        public string InputSetup { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Duration { get; set; }
        public bool HasCorrectionApplied { get; set; }
        public string CorrectionAppliedAt { get; set; }
        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

        // ----- Per-axis statistics (reference, vessel/test and deviation) -----
        public AxisStatistics RefPitch { get; set; } = AxisStatistics.Empty;
        public AxisStatistics TestPitch { get; set; } = AxisStatistics.Empty;
        public AxisStatistics DevPitch { get; set; } = AxisStatistics.Empty;

        public AxisStatistics RefRoll { get; set; } = AxisStatistics.Empty;
        public AxisStatistics TestRoll { get; set; } = AxisStatistics.Empty;
        public AxisStatistics DevRoll { get; set; } = AxisStatistics.Empty;

        public AxisStatistics RefHeave { get; set; } = AxisStatistics.Empty;
        public AxisStatistics TestHeave { get; set; } = AxisStatistics.Empty;
        public AxisStatistics DevHeave { get; set; } = AxisStatistics.Empty;

        // ----- Final results: recommended vs applied corrections -----
        public double RecommendedCorrectionPitch { get; set; }
        public double RecommendedCorrectionRoll { get; set; }
        public double RecommendedCorrectionHeave { get; set; }

        public double AppliedCorrectionPitch { get; set; }
        public double AppliedCorrectionRoll { get; set; }
        public double AppliedCorrectionHeave { get; set; }

        // ----- Optional graphics (PNG bytes). Rendered on the UI thread. -----
        public byte[] DeviationChartPng { get; set; }
        public byte[] MeansChartPng { get; set; }
        public byte[] LogoPng { get; set; }

        /// <summary>Number of averaged samples backing the deviation result.</summary>
        public int SampleCount
        {
            get { return DevPitch?.SampleCount ?? 0; }
        }

        /// <summary>True when there is captured data to report on.</summary>
        public bool HasData
        {
            get { return SampleCount > 0; }
        }

        /// <summary>
        /// Builds a report model from the live project and its view model. Run
        /// this after <see cref="ProjectVM.ComputeExtendedStatistics"/> so the
        /// statistics are current.
        /// </summary>
        public static VerificationReportModel FromProject(Project project, ProjectVM vm)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (vm == null) throw new ArgumentNullException(nameof(vm));

            return new VerificationReportModel
            {
                ProjectName = string.IsNullOrWhiteSpace(project.Name) ? "(unnamed project)" : project.Name,
                Comments = project.Comments,
                InputSetup = project.InputMRUs.GetDescription(),
                StartTime = project.StartTimeString2,
                EndTime = project.EndTimeString2,
                Duration = project.DurationString,
                HasCorrectionApplied = project.HasCorrectionApplied,
                CorrectionAppliedAt = project.CorrectionAppliedAtString,
                GeneratedUtc = DateTime.UtcNow,

                RefPitch = vm.RefPitchStats ?? AxisStatistics.Empty,
                TestPitch = vm.TestPitchStats ?? AxisStatistics.Empty,
                DevPitch = vm.DevPitchStats ?? AxisStatistics.Empty,

                RefRoll = vm.RefRollStats ?? AxisStatistics.Empty,
                TestRoll = vm.TestRollStats ?? AxisStatistics.Empty,
                DevRoll = vm.DevRollStats ?? AxisStatistics.Empty,

                RefHeave = vm.RefHeaveStats ?? AxisStatistics.Empty,
                TestHeave = vm.TestHeaveStats ?? AxisStatistics.Empty,
                DevHeave = vm.DevHeaveStats ?? AxisStatistics.Empty,

                RecommendedCorrectionPitch = vm.RecommendedCorrectionPitch,
                RecommendedCorrectionRoll = vm.RecommendedCorrectionRoll,
                RecommendedCorrectionHeave = vm.RecommendedCorrectionHeave,

                AppliedCorrectionPitch = project.AppliedCorrectionPitch,
                AppliedCorrectionRoll = project.AppliedCorrectionRoll,
                AppliedCorrectionHeave = project.AppliedCorrectionHeave,
            };
        }

        // ============================================================
        // Per-axis convenience accessors used by the renderer/exporter
        // ============================================================

        public string AxisTitle(VerificationAxisKind axis)
        {
            switch (axis)
            {
                case VerificationAxisKind.Pitch: return "Pitch";
                case VerificationAxisKind.Roll: return "Roll";
                default: return "Heave";
            }
        }

        public string Unit(VerificationAxisKind axis)
        {
            return VerificationAssessment.Unit(axis);
        }

        /// <summary>
        /// Reference scale used to draw the deviation magnitude bars. Mirrors the
        /// MagnitudeScale used by the review cards (1 degree, 0.5 metre).
        /// </summary>
        public double MagnitudeScale(VerificationAxisKind axis)
        {
            return axis == VerificationAxisKind.Heave ? 0.5 : 1.0;
        }

        public AxisStatistics RefStats(VerificationAxisKind axis)
        {
            switch (axis)
            {
                case VerificationAxisKind.Pitch: return RefPitch;
                case VerificationAxisKind.Roll: return RefRoll;
                default: return RefHeave;
            }
        }

        public AxisStatistics TestStats(VerificationAxisKind axis)
        {
            switch (axis)
            {
                case VerificationAxisKind.Pitch: return TestPitch;
                case VerificationAxisKind.Roll: return TestRoll;
                default: return TestHeave;
            }
        }

        public AxisStatistics DevStats(VerificationAxisKind axis)
        {
            switch (axis)
            {
                case VerificationAxisKind.Pitch: return DevPitch;
                case VerificationAxisKind.Roll: return DevRoll;
                default: return DevHeave;
            }
        }

        public double RecommendedCorrection(VerificationAxisKind axis)
        {
            switch (axis)
            {
                case VerificationAxisKind.Pitch: return RecommendedCorrectionPitch;
                case VerificationAxisKind.Roll: return RecommendedCorrectionRoll;
                default: return RecommendedCorrectionHeave;
            }
        }

        public double AppliedCorrection(VerificationAxisKind axis)
        {
            switch (axis)
            {
                case VerificationAxisKind.Pitch: return AppliedCorrectionPitch;
                case VerificationAxisKind.Roll: return AppliedCorrectionRoll;
                default: return AppliedCorrectionHeave;
            }
        }
    }
}
