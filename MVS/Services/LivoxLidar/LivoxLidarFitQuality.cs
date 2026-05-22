namespace MVS
{
    /// <summary>
    /// Qualitative rating of a LiDAR plane-fit RMSE value, used purely to
    /// guide the operator. The thresholds below are derived from the Livox
    /// Mid-360 single-shot range noise (~1–3 cm) plus typical helideck
    /// surface roughness; they have no effect on the applied correction.
    /// </summary>
    public enum FitRmseQuality
    {
        Unknown,
        Good,
        Acceptable,
        Marginal,
        Bad
    }

    public static class LivoxLidarFitQuality
    {
        // ── Thresholds (millimetres) ────────────────────────────────────────
        // < Good:        clean flat deck, sensor noise floor only
        // < Acceptable:  normal scan of a real coated helideck surface
        // < Marginal:    likely some non-flat geometry (slant) or residual clutter
        // ≥ Marginal:    bad — strongly non-planar, untrusted geometry
        public const double GoodMaxMm       = 15.0;
        public const double AcceptableMaxMm = 30.0;
        public const double MarginalMaxMm   = 60.0;

        /// <summary>
        /// One-line human description of the thresholds, suitable for a tooltip.
        /// </summary>
        public const string ThresholdDescription =
            "Fit RMSE quality bands (residual against the detected surface shape —\n" +
            "slant is removed before measuring, so a slanted deck is not penalised):\n" +
            "  Good        < 15 mm  — clean deck, sensor noise only\n" +
            "  Acceptable  15–30 mm — normal coated helideck surface\n" +
            "  Marginal    30–60 mm — residual clutter or unmodelled bumps\n" +
            "  Bad         ≥ 60 mm  — strongly non-planar, do not trust";

        public static FitRmseQuality Classify(double rmseMm)
        {
            if (double.IsNaN(rmseMm) || rmseMm < 0)         return FitRmseQuality.Unknown;
            if (rmseMm <  GoodMaxMm)        return FitRmseQuality.Good;
            if (rmseMm <  AcceptableMaxMm)  return FitRmseQuality.Acceptable;
            if (rmseMm <  MarginalMaxMm)    return FitRmseQuality.Marginal;
            return FitRmseQuality.Bad;
        }

        public static string Label(FitRmseQuality q)
        {
            switch (q)
            {
                case FitRmseQuality.Good:       return "Good";
                case FitRmseQuality.Acceptable: return "Acceptable";
                case FitRmseQuality.Marginal:   return "Marginal";
                case FitRmseQuality.Bad:        return "Bad";
                default:                        return "—";
            }
        }
    }
}
