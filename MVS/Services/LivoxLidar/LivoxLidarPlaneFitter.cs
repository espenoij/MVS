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
        /// <summary>Heading of the bow–stern axis in the LiDAR XY plane (degrees).</summary>
        public double HeadingDeg   { get; set; }
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
        public double AxisX        { get; set; }
        public double AxisY        { get; set; }
        public double AxisZ        { get; set; }
        /// <summary>Half-extent of the point cloud along the primary axis (mm).</summary>
        public double ExtentPrimary   { get; set; }
        /// <summary>Half-extent of the point cloud along the secondary axis (mm).</summary>
        public double ExtentSecondary { get; set; }
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

        // ── Public API ───────────────────────────────────────────────────────

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

            // ── Step 4: Normal and primary axis ──────────────────────────────
            double nx = V[0, 0], ny = V[1, 0], nz = V[2, 0];
            double ax = V[0, 2], ay = V[1, 2], az = V[2, 2];

            // Ensure normal points toward the sensor (positive Z when sensor is above helideck)
            if (nz < 0) { nx = -nx; ny = -ny; nz = -nz; }

            // Ensure primary axis is in the positive-X half-space (consistent heading)
            if (ax < 0) { ax = -ax; ay = -ay; az = -az; }

            // ── Step 5: Extract angles ────────────────────────────────────────
            result.PitchDeg   = Math.Atan2(nx, nz) * Rad2Deg;
            result.RollDeg    = Math.Atan2(ny, nz) * Rad2Deg;
            result.HeadingDeg = Math.Atan2(ay, ax) * Rad2Deg;

            // ── Step 6: Clearance = n · centroid (signed distance from origin to plane)
            result.ClearanceMm = nx * cx + ny * cy + nz * cz;

            // ── Step 7: RMSE = sqrt(smallest eigenvalue) ─────────────────────
            result.FitRmse = Math.Sqrt(Math.Max(0.0, eigenvalues[0]));

            // ── Step 8: Extents (for plane visualisation rectangle) ───────────
            double secX = V[0, 1], secY = V[1, 1], secZ = V[2, 1];
            double maxPri = 0, maxSec = 0;
            foreach (var p in points)
            {
                double dx = p.x - cx, dy = p.y - cy, dz = p.z - cz;
                double projPri = Math.Abs(dx * ax + dy * ay + dz * az);
                double projSec = Math.Abs(dx * secX + dy * secY + dz * secZ);
                if (projPri > maxPri) maxPri = projPri;
                if (projSec > maxSec) maxSec = projSec;
            }
            result.ExtentPrimary   = maxPri;
            result.ExtentSecondary = maxSec;

            result.CentroidX = cx; result.CentroidY = cy; result.CentroidZ = cz;
            result.NormalX = nx;   result.NormalY = ny;   result.NormalZ = nz;
            result.AxisX   = ax;   result.AxisY   = ay;   result.AxisZ   = az;
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
