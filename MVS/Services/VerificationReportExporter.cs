using System;
using System.Globalization;
using System.IO;
using System.Text;
using MVS.Models;

namespace MVS.Services
{
    /// <summary>
    /// Exports the verification result for a project as a structured CSV report.
    /// CSV is used because no document-rendering Telerik assembly is referenced
    /// by the project; the format is intentionally simple enough to be opened
    /// in Excel or attached to a verification record.
    /// </summary>
    public class VerificationReportExporter
    {
        public void ExportCsv(string path, Project project, ProjectVM vm)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (vm == null) throw new ArgumentNullException(nameof(vm));

            var sb = new StringBuilder();
            var ci = CultureInfo.InvariantCulture;

            // ----- Header block -----
            sb.AppendLine("Section,Key,Value");
            sb.AppendLine(Row("Project", "Id", project.Id.ToString(ci)));
            sb.AppendLine(Row("Project", "Name", Escape(project.Name)));
            sb.AppendLine(Row("Project", "Comments", Escape(project.Comments)));
            sb.AppendLine(Row("Project", "InputMRUs", project.InputMRUs.ToString()));
            sb.AppendLine(Row("Project", "StartTime", project.StartTimeString2));
            sb.AppendLine(Row("Project", "EndTime", project.EndTimeString2));
            sb.AppendLine(Row("Project", "Duration", project.DurationString));
            sb.AppendLine(Row("Project", "ReportGeneratedUtc",
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", ci)));
            sb.AppendLine();

            // ----- Per-axis statistics -----
            sb.AppendLine("Axis,Series,SampleCount,Mean,StdDev,Min,Max,RMS,OutlierPercent");
            WriteAxis(sb, ci, "Pitch", "Reference", vm.RefPitchStats);
            WriteAxis(sb, ci, "Pitch", "Test", vm.TestPitchStats);
            WriteAxis(sb, ci, "Pitch", "Deviation", vm.DevPitchStats);
            WriteAxis(sb, ci, "Roll", "Reference", vm.RefRollStats);
            WriteAxis(sb, ci, "Roll", "Test", vm.TestRollStats);
            WriteAxis(sb, ci, "Roll", "Deviation", vm.DevRollStats);
            WriteAxis(sb, ci, "Heave", "Reference", vm.RefHeaveStats);
            WriteAxis(sb, ci, "Heave", "Test", vm.TestHeaveStats);
            WriteAxis(sb, ci, "Heave", "Deviation", vm.DevHeaveStats);
            sb.AppendLine();

            // ----- Recommended and applied corrections -----
            sb.AppendLine("Axis,RecommendedCorrection,AppliedCorrection,Unit");
            sb.AppendLine(string.Format(ci, "Pitch,{0:F6},{1:F6},deg",
                vm.RecommendedCorrectionPitch, project.AppliedCorrectionPitch));
            sb.AppendLine(string.Format(ci, "Roll,{0:F6},{1:F6},deg",
                vm.RecommendedCorrectionRoll, project.AppliedCorrectionRoll));
            sb.AppendLine(string.Format(ci, "Heave,{0:F6},{1:F6},m",
                vm.RecommendedCorrectionHeave, project.AppliedCorrectionHeave));
            sb.AppendLine();

            sb.AppendLine(Row("Correction", "Applied", project.HasCorrectionApplied ? "Yes" : "No"));
            sb.AppendLine(Row("Correction", "AppliedAt", project.CorrectionAppliedAtString));

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteAxis(StringBuilder sb, CultureInfo ci, string axis, string series, AxisStatistics s)
        {
            if (s == null) s = AxisStatistics.Empty;
            sb.AppendLine(string.Format(ci,
                "{0},{1},{2},{3:F6},{4:F6},{5:F6},{6:F6},{7:F6},{8:F2}",
                axis, series, s.SampleCount, s.Mean, s.StdDev, s.Min, s.Max, s.Rms, s.OutlierPercent));
        }

        private static string Row(string section, string key, string value)
        {
            return string.Format("{0},{1},{2}", section, key, value);
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
