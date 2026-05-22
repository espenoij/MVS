using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;

namespace MVS
{
    public class LivoxLidarPlaneFitResult
    {
        public bool   IsValid      { get; set; }
        /// <summary>Pitch of helideck plane (degrees). Positive = bow up when LiDAR +X points forward.</summary>
        public double PitchDeg     { get; set; }
        /// <summary>Roll of helideck plane (degrees). Positive = starboard up when LiDAR +Y points right.</summary>
        public double RollDeg      { get; set; }
        /// <summary>RMSE of the fit in millimetres (= sqrt of smallest eigenvalue).</summary>
        public double FitRmse      { get; set; }
        /// <summary>
        /// RMSE in millimetres against the detected surface shape (flat / ridge / dome).
        /// Equals <see cref="FitRmse"/> for a Flat deck; for Ridge/CenterHigh decks
        /// it removes the variance explained by the slant, so it reflects sensor
        /// noise and residual clutter rather than deck geometry.
        /// </summary>
        public double SurfaceRmse  { get; set; }
        /// <summary>Mean perpendicular distance from the sensor origin to the fitted plane (mm).</summary>
        public double ClearanceMm  { get; set; }
        public int    PointCount   { get; set; }
        /// <summary>Fitted plane centroid (mm).</summary>
        public double CentroidX    { get; set; }
        public double CentroidY    { get; set; }
        public double CentroidZ    { get; set; }
        /// <summary>Plane normal (unit vector, pointing toward sensor).</summary>
        public double NormalX      { get; set; }
        public double NormalY      { get; set; }
        public double NormalZ      { get; set; }
        /// <summary>Primary in-plane axis (bow–stern direction, unit vector).</summary>
        public double VesselForwardX { get; set; }
        public double VesselForwardY { get; set; }
        public double VesselForwardZ { get; set; }
        /// <summary>Half-extent of the point cloud along the primary axis (mm).</summary>
        public double ExtentPrimary   { get; set; }
        /// <summary>Half-extent of the point cloud along the secondary axis (mm).</summary>
        public double ExtentSecondary { get; set; }
        /// <summary>Signed extent along vessel forward from centroid — stern side (mm, ≤ 0).</summary>
        public double FwdExtentMin    { get; set; }
        /// <summary>Signed extent along vessel forward from centroid — bow side (mm, ≥ 0).</summary>
        public double FwdExtentMax    { get; set; }
        /// <summary>Signed extent along the lateral axis from centroid — port side (mm, ≤ 0).</summary>
        public double LatExtentMin    { get; set; }
        /// <summary>Signed extent along the lateral axis from centroid — starboard side (mm, ≥ 0).</summary>
        public double LatExtentMax    { get; set; }
        /// <summary>Detected deck slant type (Flat / Ridge / CenterHigh) based on residual analysis.</summary>
        public DeckSlantType DetectedSlantType { get; set; }
        /// <summary>Magnitude of the detected slant in degrees (0 for a perfectly flat deck).</summary>
        public double DetectedSlantDeg { get; set; }
    }

    /// <summary>
    /// Fits a plane to a 3-D point cloud using PCA on the 3×3 covariance matrix.
    /// The eigendecomposition is delegated to the Math.NET Numerics library
    /// (well-tested, production-grade implementation).
    /// </summary>
    public static class LivoxLidarPlaneFitter
    {
        private const double Rad2Deg = 180.0 / Math.PI;
        private const int    MinPoints = 20;

        private const int    FilterIterations = 3;
        private const double FilterMadScale   = 6.0; // keep points within median ± 6·MAD
        private const double FilterMinThreshold = 100.0; // mm — floor so thin-noise clouds aren't over-trimmed

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Iteratively removes non-planar outliers (e.g. obstacle walls/tops)
        /// by fitting a quick PCA plane, computing the median absolute deviation
        /// of signed distances, and discarding points beyond median ± k·MAD.
        /// The median-based threshold is robust to the ~10–15 % cube contamination
        /// that would otherwise skew a mean-based or fixed threshold.
        /// </summary>
        public static List<(float x, float y, float z)> FilterPlaneInliers(
            List<(float x, float y, float z)> points)
        {
            var current = points;

            for (int iter = 0; iter < FilterIterations; iter++)
            {
                int n = current.Count;
                if (n < MinPoints) return current;

                // Centroid
                double sx = 0, sy = 0, sz = 0;
                foreach (var p in current) { sx += p.x; sy += p.y; sz += p.z; }
                double cx = sx / n, cy = sy / n, cz = sz / n;

                // 3×3 covariance
                double sxx = 0, syy = 0, szz = 0, sxy = 0, sxz = 0, syz = 0;
                foreach (var p in current)
                {
                    double dx = p.x - cx, dy = p.y - cy, dz = p.z - cz;
                    sxx += dx * dx; syy += dy * dy; szz += dz * dz;
                    sxy += dx * dy; sxz += dx * dz; syz += dy * dz;
                }
                double[,] M =
                {
                    { sxx / n, sxy / n, sxz / n },
                    { sxy / n, syy / n, syz / n },
                    { sxz / n, syz / n, szz / n }
                };

                EigenDecomposeAscending(M, out double[] ev, out double[,] V);

                // Normal = smallest eigenvector
                double nx = V[0, 0], ny = V[1, 0], nz = V[2, 0];
                double len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (len < 1e-12) return current;
                nx /= len; ny /= len; nz /= len;

                // Signed distance from each point to the fitted plane
                double d = -(nx * cx + ny * cy + nz * cz);
                var dists = new double[n];
                for (int i = 0; i < n; i++)
                {
                    var p = current[i];
                    dists[i] = nx * p.x + ny * p.y + nz * p.z + d;
                }

                // Robust threshold: median ± k·MAD
                var sorted = (double[])dists.Clone();
                Array.Sort(sorted);
                double median = sorted[n / 2];

                var absDev = new double[n];
                for (int i = 0; i < n; i++)
                    absDev[i] = Math.Abs(dists[i] - median);
                Array.Sort(absDev);
                double mad = absDev[n / 2];

                double threshold = Math.Max(FilterMinThreshold, FilterMadScale * mad);

                // Keep only inliers
                var inliers = new List<(float, float, float)>(n);
                for (int i = 0; i < n; i++)
                {
                    if (Math.Abs(dists[i] - median) <= threshold)
                        inliers.Add(current[i]);
                }

                if (inliers.Count < MinPoints) return current;
                current = inliers;
            }

            return current;
        }

        public static LivoxLidarPlaneFitResult Fit(List<(float x, float y, float z)> points)
        {
            var result = new LivoxLidarPlaneFitResult();
            int n = points.Count;

            if (n < MinPoints)
            {
                result.IsValid = false;
                return result;
            }

            // ── Step 1: centroid ─────────────────────────────────────────────
            double sx = 0, sy = 0, sz = 0;
            foreach (var p in points) { sx += p.x; sy += p.y; sz += p.z; }
            double cx = sx / n, cy = sy / n, cz = sz / n;

            // ── Step 2: 3×3 covariance matrix (6 unique entries) ─────────────
            double sxx = 0, syy = 0, szz = 0, sxy = 0, sxz = 0, syz = 0;
            foreach (var p in points)
            {
                double dx = p.x - cx, dy = p.y - cy, dz = p.z - cz;
                sxx += dx * dx;  syy += dy * dy;  szz += dz * dz;
                sxy += dx * dy;  sxz += dx * dz;  syz += dy * dz;
            }

            double[,] M =
            {
                { sxx / n, sxy / n, sxz / n },
                { sxy / n, syy / n, syz / n },
                { sxz / n, syz / n, szz / n }
            };

            // ── Step 3: Eigendecomposition (Math.NET Numerics) ───────────────
            // Sorted ascending so column 0 = smallest eigenvalue (plane normal).
            EigenDecomposeAscending(M, out double[] eigenvalues, out double[,] V);

            // ── Step 4: Plane normal ──────────────────────────────────────────
            double nx = V[0, 0], ny = V[1, 0], nz = V[2, 0];

            // Ensure normal points toward the sensor (positive Z when sensor is above helideck)
            if (nz < 0) { nx = -nx; ny = -ny; nz = -nz; }

            // ── Step 5: In-plane frame aligned with LiDAR +X ─────────────────
            // U = projection of sensor +X onto the fitted plane, normalised
            double uDotN = nx; // (1,0,0)·N = nx
            double ux = 1.0 - uDotN*nx, uy = -uDotN*ny, uz = -uDotN*nz;
            double uLen = Math.Sqrt(ux*ux + uy*uy + uz*uz);
            if (uLen < 1e-6) { ux = 0; uy = 1; uz = 0; } // fallback: +X ∥ N
            else { ux /= uLen; uy /= uLen; uz /= uLen; }
            // W = N × U (second in-plane basis vector, 90° left of U)
            double wx = ny*uz - nz*uy, wy = nz*ux - nx*uz, wz = nx*uy - ny*ux;

            // Project all centroid-relative points onto (U, W)
            var pu = new double[n];
            var pw = new double[n];
            for (int i = 0; i < n; i++)
            {
                double dx = points[i].x - cx, dy = points[i].y - cy, dz = points[i].z - cz;
                pu[i] = dx*ux + dy*uy + dz*uz;
                pw[i] = dx*wx + dy*wy + dz*wz;
            }

            // ── Step 6: Vessel forward — bow-edge line fit ───────────────────
            // The edge directly in front of the sensor is always perpendicular to
            // vessel forward.  Collect all points in the top 15 % of the U range
            // (the bow-edge region), fit a line via 2-D PCA, and take the smallest
            // eigenvector (= bow-edge normal) as vessel forward.  Using every point
            // in the frontier band — rather than one max-U sample per lateral bin —
            // gives a well-conditioned covariance and is robust to surface outliers.
            double uMin = double.MaxValue, uMax = double.MinValue;
            for (int i = 0; i < n; i++)
            {
                if (pu[i] < uMin) uMin = pu[i];
                if (pu[i] > uMax) uMax = pu[i];
            }
            double uRange = uMax - uMin;
            if (uRange < 1.0) uRange = 1.0;
            double uCutoff = uMax - 0.15 * uRange;

            double bSumU = 0.0, bSumW = 0.0;
            int    bN    = 0;
            for (int i = 0; i < n; i++)
            {
                if (pu[i] >= uCutoff) { bSumU += pu[i]; bSumW += pw[i]; bN++; }
            }

            double evU, evW;
            if (bN >= 3)
            {
                double muBU = bSumU / bN, muBW = bSumW / bN;
                double bSuu = 0.0, bSww = 0.0, bSuw = 0.0;
                for (int i = 0; i < n; i++)
                {
                    if (pu[i] < uCutoff) continue;
                    double du = pu[i] - muBU, dw = pw[i] - muBW;
                    bSuu += du * du;  bSww += dw * dw;  bSuw += du * dw;
                }

                // Smallest eigenvector = bow-edge normal = vessel forward
                // λ_small = ( (a+c) − sqrt((a−c)² + 4b²) ) / 2,  eigenvector = (b, λ_small − a)
                double bDisc     = Math.Sqrt(Math.Max(0.0, (bSuu - bSww) * (bSuu - bSww) + 4.0 * bSuw * bSuw));
                double bLamSmall = (bSuu + bSww - bDisc) * 0.5;
                evU = bSuw;  evW = bLamSmall - bSuu;
                double evLen = Math.Sqrt(evU * evU + evW * evW);
                if (evLen < 1e-12) { evU = 1.0; evW = 0.0; }
                else { evU /= evLen; evW /= evLen; }
                if (evU < 0) { evU = -evU; evW = -evW; } // keep in +U (sensor-forward) half-space
            }
            else
            {
                evU = 1.0; evW = 0.0; // fallback: vessel forward ≈ sensor +X projected
            }

            // Vessel forward in 3-D
            double ax = evU * ux + evW * wx;
            double ay = evU * uy + evW * wy;
            double az = evU * uz + evW * wz;
            if (ax < 0) { ax = -ax; ay = -ay; az = -az; } // guarantee +X half-space

            // ── Step 7: Extract angles ────────────────────────────────────────
            // Project normal onto vessel axes rather than fixed LiDAR axes so that
            // pitch/roll are correct even when the LiDAR is yawed relative to the deck.
            // Use the horizontal projection of vessel forward so the result is
            // independent of the in-plane tilt of the forward vector itself.
            double fhLen = Math.Sqrt(ax * ax + ay * ay);
            if (fhLen < 1e-6) fhLen = 1.0; // fallback: forward nearly vertical
            double fhx = ax / fhLen, fhy = ay / fhLen; // unit horizontal forward
            double lhx = -fhy,       lhy = fhx;        // unit horizontal lateral (90° CCW of forward)
            result.PitchDeg = Math.Atan2(nx * fhx + ny * fhy, nz) * Rad2Deg;
            result.RollDeg  = Math.Atan2(nx * lhx + ny * lhy, nz) * Rad2Deg;

            // ── Step 8: Clearance = perpendicular distance from sensor origin to plane.
            // dot(n, centroid) is negative when the normal points toward the sensor,
            // so negate it to obtain a positive distance value.
            result.ClearanceMm = -(nx * cx + ny * cy + nz * cz);

            // ── Step 9: RMSE = sqrt(smallest eigenvalue) ──────────────────────
            result.FitRmse = Math.Sqrt(Math.Max(0.0, eigenvalues[0]));

            // ── Step 10: Extents — project all points onto vessel forward / lateral ─
            // Lateral = N × VesselForward (in-plane port-starboard direction)
            double latX = ny*az - nz*ay, latY = nz*ax - nx*az, latZ = nx*ay - ny*ax;
            double fwdMax = 0, fwdMin = 0, latMax = 0, latMin = 0;
            // Residuals (signed perpendicular distance to fitted plane) and the in-plane
            // |lateral| / radial coordinates needed for slant-type classification.
            var residuals = new double[n];
            var absLat    = new double[n];
            var radial    = new double[n];
            for (int i = 0; i < n; i++)
            {
                var p = points[i];
                double dx = p.x - cx, dy = p.y - cy, dz = p.z - cz;
                double pf = dx*ax + dy*ay + dz*az;
                double pl = dx*latX + dy*latY + dz*latZ;
                if (pf > fwdMax) fwdMax = pf; if (pf < fwdMin) fwdMin = pf;
                if (pl > latMax) latMax = pl; if (pl < latMin) latMin = pl;
                residuals[i] = nx * dx + ny * dy + nz * dz; // signed dist to plane
                absLat[i]    = Math.Abs(pl);
                radial[i]    = Math.Sqrt(pf * pf + pl * pl);
            }
            result.ExtentPrimary   = Math.Max(fwdMax, -fwdMin);
            result.ExtentSecondary = Math.Max(latMax, -latMin);
            result.FwdExtentMin = fwdMin; result.FwdExtentMax = fwdMax;
            result.LatExtentMin = latMin; result.LatExtentMax = latMax;

            // ── Step 11: Slant-type classification ──────────────────────────
            // The plane fit captures any single-direction (Flat) tilt in pitch/roll,
            // so residuals are ~noise for a Flat deck. A Ridge produces residuals that
            // correlate linearly with |lateral|; a CenterHigh dome produces residuals
            // that correlate linearly with the in-plane radius. Fit both candidate
            // models by least-squares regression of residual vs. predictor, compute
            // the resulting sum-of-squared errors (SSE), and pick whichever model
            // explains the most variance — provided the improvement vs. Flat is
            // significant and the recovered slope exceeds a small threshold.
            ClassifySlant(residuals, absLat, radial, out var slantType, out double slantDeg, out double surfaceSse);
            result.DetectedSlantType = slantType;
            result.DetectedSlantDeg  = slantDeg;

            // Slant-aware RMSE — residual against the detected surface model.
            // For a Flat deck this equals FitRmse; for Ridge / CenterHigh it
            // removes the deck geometry's contribution so the operator sees a
            // pure noise/clutter quality figure.
            result.SurfaceRmse = Math.Sqrt(Math.Max(0.0, surfaceSse / n));

            result.CentroidX = cx; result.CentroidY = cy; result.CentroidZ = cz;
            result.NormalX = nx;           result.NormalY = ny;           result.NormalZ = nz;
            result.VesselForwardX = ax;   result.VesselForwardY = ay;   result.VesselForwardZ = az;
            result.PointCount = n;
            result.IsValid    = true;

            return result;
        }

        // ── Slant-type classification ───────────────────────────────────────
        // After the plane fit, residuals along the plane normal encode any remaining
        // deviation from flatness. A Ridge (peak along vessel forward) makes residuals
        // decrease linearly with |lateral|; a CenterHigh dome makes them decrease
        // linearly with the in-plane radius. We fit residual = a + b·predictor by
        // least squares for both predictors and accept the better-fitting non-Flat
        // model only when the improvement in SSE vs. Flat is significant.
        private const double SlantMinAngleDeg = 0.5;     // ignore sub-half-degree slants
        private const double SlantSseImprovementRatio = 0.5; // model must cut SSE by ≥ 50 %

        private static void ClassifySlant(
            double[] residuals, double[] absLat, double[] radial,
            out DeckSlantType type, out double slantDeg, out double surfaceSse)
        {
            type = DeckSlantType.Flat;
            slantDeg = 0.0;
            surfaceSse = 0.0;

            int n = residuals.Length;
            if (n < MinPoints) return;

            // Mean residual (should be ~0 by construction, but compute defensively)
            double meanR = 0;
            for (int i = 0; i < n; i++) meanR += residuals[i];
            meanR /= n;

            // SSE for the Flat hypothesis (residual ≈ constant)
            double sseFlat = 0;
            for (int i = 0; i < n; i++)
            {
                double d = residuals[i] - meanR;
                sseFlat += d * d;
            }
            surfaceSse = sseFlat;
            if (sseFlat < 1e-9) return;

            // Linear fit residual = a + b·x for a given predictor x
            FitLine(residuals, absLat, out double bRidge,  out double sseRidge);
            FitLine(residuals, radial, out double bDome,   out double sseDome);

            // Ridge and CenterHigh both push the centre up relative to the edges,
            // so the slope must be negative (residual falls as |lat| / radius grows).
            bool ridgeOk = bRidge < 0 && sseRidge < sseFlat * SlantSseImprovementRatio;
            bool domeOk  = bDome  < 0 && sseDome  < sseFlat * SlantSseImprovementRatio;

            if (!ridgeOk && !domeOk) return;

            // Pick the model with the lower SSE
            if (domeOk && (!ridgeOk || sseDome <= sseRidge))
            {
                type = DeckSlantType.CenterHigh;
                slantDeg = Math.Atan(-bDome) * Rad2Deg;
                surfaceSse = sseDome;
            }
            else
            {
                type = DeckSlantType.Ridge;
                slantDeg = Math.Atan(-bRidge) * Rad2Deg;
                surfaceSse = sseRidge;
            }

            if (slantDeg < SlantMinAngleDeg)
            {
                type = DeckSlantType.Flat;
                slantDeg = 0.0;
                surfaceSse = sseFlat;
            }
        }

        // Ordinary least-squares fit of y = a + b·x; returns the slope b and the
        // residual sum of squares (SSE) of the fit. Falls back to a constant model
        // (SSE = total variance, slope = 0) when x has zero spread.
        private static void FitLine(double[] y, double[] x, out double b, out double sse)
        {
            int n = y.Length;
            double sx = 0, sy = 0;
            for (int i = 0; i < n; i++) { sx += x[i]; sy += y[i]; }
            double mx = sx / n, my = sy / n;

            double sxx = 0, sxy = 0;
            for (int i = 0; i < n; i++)
            {
                double dx = x[i] - mx;
                sxx += dx * dx;
                sxy += dx * (y[i] - my);
            }

            if (sxx < 1e-12)
            {
                b = 0;
                sse = 0;
                for (int i = 0; i < n; i++) { double d = y[i] - my; sse += d * d; }
                return;
            }

            b = sxy / sxx;
            double a = my - b * mx;
            sse = 0;
            for (int i = 0; i < n; i++)
            {
                double r = y[i] - (a + b * x[i]);
                sse += r * r;
            }
        }

        // ── Symmetric eigendecomposition via Math.NET Numerics ──────────────
        // Wraps the well-tested Math.NET Evd routine and returns eigenvalues
        // sorted ascending with eigenvector columns aligned, matching the
        // calling convention previously satisfied by the in-house Jacobi solver.
        private static void EigenDecomposeAscending(double[,] A, out double[] eigenvalues, out double[,] V)
        {
            var m = DenseMatrix.OfArray(A);
            var evd = m.Evd(Symmetricity.Symmetric);

            // Math.NET returns eigenvalues already sorted ascending for symmetric input,
            // and EigenVectors columns aligned to those eigenvalues.
            var evVec = evd.EigenValues;
            var V_m   = evd.EigenVectors;

            eigenvalues = new double[3];
            V           = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                eigenvalues[i] = evVec[i].Real;
                for (int r = 0; r < 3; r++)
                    V[r, i] = V_m[r, i];
            }
        }
    }
}
