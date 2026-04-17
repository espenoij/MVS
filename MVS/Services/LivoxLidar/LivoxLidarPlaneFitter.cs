using System;
using System.Collections.Generic;

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
    }

    /// <summary>
    /// Fits a plane to a 3-D point cloud using PCA on the 3×3 covariance matrix.
    /// The eigendecomposition is solved analytically via the cyclic Jacobi method —
    /// no external libraries required.
    /// </summary>
    public static class LivoxLidarPlaneFitter
    {
        private const double Rad2Deg = 180.0 / Math.PI;
        private const int    MaxSweeps = 50;
        private const double Tolerance = 1e-14;
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

                double[] ev; double[,] V;
                Jacobi3x3(M, out ev, out V);
                SortColumns(ref ev, ref V);

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

            // ── Step 3: Jacobi eigendecomposition ────────────────────────────
            double[] eigenvalues;
            double[,] V;
            Jacobi3x3(M, out eigenvalues, out V);

            // Sort ascending so column 0 = smallest eigenvalue (normal)
            SortColumns(ref eigenvalues, ref V);

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
            result.PitchDeg   = Math.Atan2(nx, nz) * Rad2Deg;
            result.RollDeg    = Math.Atan2(ny, nz) * Rad2Deg;

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
            foreach (var p in points)
            {
                double dx = p.x - cx, dy = p.y - cy, dz = p.z - cz;
                double pf = dx*ax + dy*ay + dz*az;
                double pl = dx*latX + dy*latY + dz*latZ;
                if (pf > fwdMax) fwdMax = pf; if (pf < fwdMin) fwdMin = pf;
                if (pl > latMax) latMax = pl; if (pl < latMin) latMin = pl;
            }
            result.ExtentPrimary   = Math.Max(fwdMax, -fwdMin);
            result.ExtentSecondary = Math.Max(latMax, -latMin);
            result.FwdExtentMin = fwdMin; result.FwdExtentMax = fwdMax;
            result.LatExtentMin = latMin; result.LatExtentMax = latMax;

            result.CentroidX = cx; result.CentroidY = cy; result.CentroidZ = cz;
            result.NormalX = nx;           result.NormalY = ny;           result.NormalZ = nz;
            result.VesselForwardX = ax;   result.VesselForwardY = ay;   result.VesselForwardZ = az;
            result.PointCount = n;
            result.IsValid    = true;

            return result;
        }

        // ── Jacobi cyclic algorithm for real symmetric 3×3 ──────────────────
        // V columns are the eigenvectors; eigenvalues correspond to the diagonal of A after convergence.
        private static void Jacobi3x3(double[,] A, out double[] eigenvalues, out double[,] V)
        {
            double[,] a = (double[,])A.Clone();
            V = new double[3, 3] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };

            for (int sweep = 0; sweep < MaxSweeps; sweep++)
            {
                // Off-diagonal Frobenius norm — stop when negligible
                double off = a[0,1]*a[0,1] + a[0,2]*a[0,2] + a[1,2]*a[1,2];
                if (off < Tolerance) break;

                // Cycle over all three off-diagonal pairs
                for (int p = 0; p < 2; p++)
                for (int q = p + 1; q < 3; q++)
                {
                    double apq = a[p, q];
                    if (Math.Abs(apq) < 1e-30) continue;

                    // Givens rotation angle to zero a[p,q]
                    double theta = 0.5 * Math.Atan2(2.0 * apq, a[q, q] - a[p, p]);
                    double c = Math.Cos(theta);
                    double s = Math.Sin(theta);

                    // Save values before overwrite
                    double app = a[p, p], aqq = a[q, q];
                    int r = 3 - p - q;             // third index: 0+1+2=3
                    double arp = a[r, p], arq = a[r, q];

                    // Apply similarity transform G^T * a * G
                    a[p, p] = c*c*app - 2*s*c*apq + s*s*aqq;
                    a[q, q] = s*s*app + 2*s*c*apq + c*c*aqq;
                    a[p, q] = a[q, p] = 0.0;
                    a[r, p] = a[p, r] = c*arp - s*arq;
                    a[r, q] = a[q, r] = s*arp + c*arq;

                    // Accumulate rotation: V ← V * G
                    for (int i = 0; i < 3; i++)
                    {
                        double vip = V[i, p], viq = V[i, q];
                        V[i, p] = c*vip - s*viq;
                        V[i, q] = s*vip + c*viq;
                    }
                }
            }

            eigenvalues = new double[] { a[0, 0], a[1, 1], a[2, 2] };
        }

        // Sort eigenvalues ascending, keeping eigenvector columns aligned
        private static void SortColumns(ref double[] vals, ref double[,] vecs)
        {
            for (int i = 0; i < 2; i++)
            for (int j = i + 1; j < 3; j++)
            {
                if (vals[j] < vals[i])
                {
                    double tmp = vals[i]; vals[i] = vals[j]; vals[j] = tmp;
                    for (int k = 0; k < 3; k++)
                    {
                        tmp = vecs[k, i]; vecs[k, i] = vecs[k, j]; vecs[k, j] = tmp;
                    }
                }
            }
        }
    }
}
