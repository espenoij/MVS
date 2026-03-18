using System;
using System.Collections.Generic;

namespace MVS
{
    /// <summary>
    /// Result of deck-edge detection: the 3-D edge line, its angle relative to
    /// the LiDAR forward axis, the inferred vessel-forward direction, and the
    /// set of edge points used for the fit.
    /// </summary>
    public class LivoxLidarDeckEdgeResult
    {
        public bool IsValid { get; set; }

        /// <summary>3-D unit direction vector along the detected deck edge.</summary>
        public double DirectionX { get; set; }
        public double DirectionY { get; set; }
        public double DirectionZ { get; set; }

        /// <summary>
        /// Angle (degrees) of the edge direction relative to the LiDAR forward
        /// axis (+X), measured in the deck plane.
        /// </summary>
        public double EdgeAngleDeg { get; set; }

        /// <summary>Vessel-forward unit vector inferred from the edge (perpendicular to edge, toward bow).</summary>
        public double VesselForwardX { get; set; }
        public double VesselForwardY { get; set; }
        public double VesselForwardZ { get; set; }

        /// <summary>Vessel-forward heading in the sensor XY plane (degrees from +X).</summary>
        public double VesselForwardAngleDeg { get; set; }

        /// <summary>3-D midpoint of the detected edge segment.</summary>
        public double MidpointX { get; set; }
        public double MidpointY { get; set; }
        public double MidpointZ { get; set; }

        /// <summary>Half-length of the detected edge segment (mm).</summary>
        public double HalfLength { get; set; }

        /// <summary>Edge points in 3-D used for the line fit.</summary>
        public List<(float x, float y, float z)> EdgePoints { get; set; }
            = new List<(float, float, float)>();

        // Diagnostic counters
        public int    ForwardPointCount { get; set; }
        public int    DeckInlierCount   { get; set; }
        public int    HullVertexCount   { get; set; }
        public int    EdgePointCount    { get; set; }
        public double FitRmseMm         { get; set; }
    }

    /// <summary>
    /// Detects the deck edge directly in front of the LiDAR sensor.
    /// Pipeline: forward crop → RANSAC plane → 2-D projection → convex hull →
    /// closest boundary segment → PCA line fit.
    /// No external library dependencies.
    /// </summary>
    public static class LivoxLidarDeckEdgeFinder
    {
        private const double Rad2Deg           = 180.0 / Math.PI;
        private const int    RansacIterations  = 300;
        private const double RansacThresholdMm = 50.0;
        private const double MinNormalZ        = 0.7;
        private const int    MinPoints         = 50;
        private const double EdgeBandMm        = 400.0;
        private const int    MaxJacobiSweeps   = 50;
        private const double JacobiTolerance   = 1e-14;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Detect the front deck edge from a raw LiDAR point cloud.
        /// Returns an invalid result if detection fails at any stage.
        /// </summary>
        public static LivoxLidarDeckEdgeResult FindEdge(
            List<(float x, float y, float z)> allPoints)
        {
            var result = new LivoxLidarDeckEdgeResult();
            if (allPoints == null || allPoints.Count < MinPoints)
                return result;

            // ── Step 1: Crop to forward region (x > 0) ──────────────────────
            var fwd = new List<(float x, float y, float z)>(allPoints.Count / 2);
            foreach (var p in allPoints)
                if (p.x > 0) fwd.Add(p);
            result.ForwardPointCount = fwd.Count;
            if (fwd.Count < MinPoints) return result;

            // ── Step 2: RANSAC horizontal plane fit — keep inliers ───────────
            var    rng     = new Random(42);
            double bNx     = 0, bNy = 0, bNz = 1, bD = 0;
            int    bestCnt = 0;

            for (int iter = 0; iter < RansacIterations; iter++)
            {
                int i0 = rng.Next(fwd.Count);
                int i1 = rng.Next(fwd.Count);
                int i2 = rng.Next(fwd.Count);
                if (i0 == i1 || i0 == i2 || i1 == i2) continue;

                double v1x = fwd[i1].x - fwd[i0].x, v1y = fwd[i1].y - fwd[i0].y, v1z = fwd[i1].z - fwd[i0].z;
                double v2x = fwd[i2].x - fwd[i0].x, v2y = fwd[i2].y - fwd[i0].y, v2z = fwd[i2].z - fwd[i0].z;

                double nx = v1y * v2z - v1z * v2y;
                double ny = v1z * v2x - v1x * v2z;
                double nz = v1x * v2y - v1y * v2x;
                double nL = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (nL < 1e-10) continue;
                nx /= nL; ny /= nL; nz /= nL;
                if (nz < 0) { nx = -nx; ny = -ny; nz = -nz; }
                if (Math.Abs(nz) < MinNormalZ) continue;

                double d = -(nx * fwd[i0].x + ny * fwd[i0].y + nz * fwd[i0].z);
                int cnt = 0;
                for (int i = 0; i < fwd.Count; i++)
                    if (Math.Abs(nx * fwd[i].x + ny * fwd[i].y + nz * fwd[i].z + d) < RansacThresholdMm)
                        cnt++;

                if (cnt > bestCnt) { bestCnt = cnt; bNx = nx; bNy = ny; bNz = nz; bD = d; }
            }

            if (bestCnt < MinPoints) return result;

            var inliers = new List<(float x, float y, float z)>(bestCnt);
            foreach (var p in fwd)
                if (Math.Abs(bNx * p.x + bNy * p.y + bNz * p.z + bD) < RansacThresholdMm)
                    inliers.Add(p);
            result.DeckInlierCount = inliers.Count;

            // Refit plane with PCA on all inliers for a refined normal
            int    n  = inliers.Count;
            double cx = 0, cy = 0, cz = 0;
            foreach (var p in inliers) { cx += p.x; cy += p.y; cz += p.z; }
            cx /= n; cy /= n; cz /= n;

            double sxx = 0, syy = 0, szz = 0, sxy = 0, sxz = 0, syz = 0;
            foreach (var p in inliers)
            {
                double dx = p.x - cx, dy = p.y - cy, dz = p.z - cz;
                sxx += dx * dx; syy += dy * dy; szz += dz * dz;
                sxy += dx * dy; sxz += dx * dz; syz += dy * dz;
            }
            double[,] cov =
            {
                { sxx / n, sxy / n, sxz / n },
                { sxy / n, syy / n, syz / n },
                { sxz / n, syz / n, szz / n }
            };
            Jacobi3x3(cov, out double[] evals, out double[,] V);
            SortColumns(ref evals, ref V);

            double pnx = V[0, 0], pny = V[1, 0], pnz = V[2, 0];
            if (pnz < 0) { pnx = -pnx; pny = -pny; pnz = -pnz; }
            result.FitRmseMm = Math.Sqrt(Math.Max(0.0, evals[0]));

            // ── Step 3: Project inliers onto 2-D deck plane ──────────────────
            // U = projection of sensor +X onto the fitted plane, normalised
            double uDotN = pnx;
            double ux = 1.0 - uDotN * pnx, uy = -uDotN * pny, uz = -uDotN * pnz;
            double uL = Math.Sqrt(ux * ux + uy * uy + uz * uz);
            if (uL < 1e-6) { ux = 0; uy = 1; uz = 0; }
            else            { ux /= uL; uy /= uL; uz /= uL; }
            // W = N × U (second in-plane basis vector)
            double wx = pny * uz - pnz * uy, wy = pnz * ux - pnx * uz, wz = pnx * uy - pny * ux;

            var proj = new (double u, double w)[n];
            for (int i = 0; i < n; i++)
            {
                double dx = inliers[i].x - cx, dy = inliers[i].y - cy, dz = inliers[i].z - cz;
                proj[i] = (dx * ux + dy * uy + dz * uz, dx * wx + dy * wy + dz * wz);
            }

            // LiDAR origin (0,0,0) projected onto the plane's 2-D frame
            double lidarU = -cx * ux - cy * uy - cz * uz;
            double lidarW = -cx * wx - cy * wy - cz * wz;

            // ── Step 4: Convex hull of the deck boundary ─────────────────────
            var hull = ConvexHull(proj);
            result.HullVertexCount = hull.Count;
            if (hull.Count < 3) return result;

            // ── Step 5: Closest hull segment to LiDAR projection ─────────────
            int    bestSeg  = 0;
            double bestDist = double.MaxValue;
            for (int i = 0; i < hull.Count; i++)
            {
                int j = (i + 1) % hull.Count;
                double d = PointToSegmentDist(lidarU, lidarW,
                               proj[hull[i]].u, proj[hull[i]].w,
                               proj[hull[j]].u, proj[hull[j]].w);
                if (d < bestDist) { bestDist = d; bestSeg = i; }
            }

            int hA = hull[bestSeg], hB = hull[(bestSeg + 1) % hull.Count];
            double eu = proj[hB].u - proj[hA].u, ew = proj[hB].w - proj[hA].w;
            double eLen = Math.Sqrt(eu * eu + ew * ew);
            if (eLen < 1e-6) return result;

            // Edge-line normal pointing inward (toward deck centroid at origin)
            double enu = -ew / eLen, enw = eu / eLen;
            if (enu * (0 - proj[hA].u) + enw * (0 - proj[hA].w) < 0) { enu = -enu; enw = -enw; }

            // Collect all inlier points within the perpendicular band of the edge line
            var edgeIdx = new List<int>();
            for (int i = 0; i < n; i++)
            {
                double perpDist = Math.Abs(enu * (proj[i].u - proj[hA].u) + enw * (proj[i].w - proj[hA].w));
                if (perpDist <= EdgeBandMm)
                    edgeIdx.Add(i);
            }
            result.EdgePointCount = edgeIdx.Count;
            if (edgeIdx.Count < 3) return result;

            // ── Step 6: PCA line fit on edge points (2-D) ────────────────────
            double muU = 0, muW = 0;
            foreach (int idx in edgeIdx) { muU += proj[idx].u; muW += proj[idx].w; }
            muU /= edgeIdx.Count; muW /= edgeIdx.Count;

            double suu = 0, sww = 0, suw = 0;
            foreach (int idx in edgeIdx)
            {
                double du = proj[idx].u - muU, dw = proj[idx].w - muW;
                suu += du * du; sww += dw * dw; suw += du * dw;
            }

            // Largest eigenvector of the 2×2 covariance = edge direction
            double disc = Math.Sqrt(Math.Max(0.0, (suu - sww) * (suu - sww) + 4.0 * suw * suw));
            double lamL = (suu + sww + disc) * 0.5;
            double dirU, dirW;
            if (Math.Abs(suw) > 1e-12)
            {
                dirU = suw;  dirW = lamL - suu;
            }
            else
            {
                dirU = suu >= sww ? 1.0 : 0.0;
                dirW = suu >= sww ? 0.0 : 1.0;
            }
            double dL = Math.Sqrt(dirU * dirU + dirW * dirW);
            if (dL < 1e-12) { dirU = 0; dirW = 1; } else { dirU /= dL; dirW /= dL; }

            // Convert edge direction to 3-D
            double d3x = dirU * ux + dirW * wx, d3y = dirU * uy + dirW * wy, d3z = dirU * uz + dirW * wz;
            double d3L = Math.Sqrt(d3x * d3x + d3y * d3y + d3z * d3z);
            if (d3L > 1e-10) { d3x /= d3L; d3y /= d3L; d3z /= d3L; }

            // Edge midpoint in 3-D (centroid of edge-band points)
            double mx = cx + muU * ux + muW * wx;
            double my = cy + muU * uy + muW * wy;
            double mz = cz + muU * uz + muW * wz;

            // Half-length from PCA edge centroid
            double eMin = 0, eMax = 0;
            foreach (int idx in edgeIdx)
            {
                double t = (proj[idx].u - muU) * dirU + (proj[idx].w - muW) * dirW;
                if (t < eMin) eMin = t; if (t > eMax) eMax = t;
            }

            // Vessel forward = perpendicular to edge in the plane, pointing from LiDAR toward edge
            double fU = -dirW, fW = dirU;
            if (fU * (muU - lidarU) + fW * (muW - lidarW) < 0) { fU = -fU; fW = -fW; }

            double f3x = fU * ux + fW * wx, f3y = fU * uy + fW * wy, f3z = fU * uz + fW * wz;
            double fL = Math.Sqrt(f3x * f3x + f3y * f3y + f3z * f3z);
            if (fL > 1e-10) { f3x /= fL; f3y /= fL; f3z /= fL; }

            // Angles
            double edgeAngle = Math.Atan2(dirW, dirU) * Rad2Deg;
            double fwdAngle  = Math.Atan2(f3y, f3x)  * Rad2Deg;

            // Collect 3-D edge points
            var edgePts = new List<(float x, float y, float z)>(edgeIdx.Count);
            foreach (int idx in edgeIdx) edgePts.Add(inliers[idx]);

            // ── Step 7: Populate result ──────────────────────────────────────
            result.IsValid               = true;
            result.DirectionX            = d3x;
            result.DirectionY            = d3y;
            result.DirectionZ            = d3z;
            result.EdgeAngleDeg          = edgeAngle;
            result.VesselForwardX        = f3x;
            result.VesselForwardY        = f3y;
            result.VesselForwardZ        = f3z;
            result.VesselForwardAngleDeg = fwdAngle;
            result.MidpointX             = mx;
            result.MidpointY             = my;
            result.MidpointZ             = mz;
            result.HalfLength            = Math.Max(eMax, -eMin);
            result.EdgePoints            = edgePts;

            return result;
        }

        // ── Convex hull — Andrew's monotone chain ────────────────────────────
        // Returns indices into the input array forming the hull in CCW order.

        private static List<int> ConvexHull((double u, double w)[] pts)
        {
            int n = pts.Length;
            if (n < 3) return new List<int>();

            var sorted = new int[n];
            for (int i = 0; i < n; i++) sorted[i] = i;
            Array.Sort(sorted, (a, b) =>
            {
                int c = pts[a].u.CompareTo(pts[b].u);
                return c != 0 ? c : pts[a].w.CompareTo(pts[b].w);
            });

            var hull = new int[2 * n];
            int k = 0;

            // Lower hull
            for (int i = 0; i < n; i++)
            {
                while (k >= 2 && Cross2D(pts[hull[k - 2]], pts[hull[k - 1]], pts[sorted[i]]) <= 0)
                    k--;
                hull[k++] = sorted[i];
            }

            // Upper hull
            int lower = k + 1;
            for (int i = n - 2; i >= 0; i--)
            {
                while (k >= lower && Cross2D(pts[hull[k - 2]], pts[hull[k - 1]], pts[sorted[i]]) <= 0)
                    k--;
                hull[k++] = sorted[i];
            }

            // k-1 because the last vertex duplicates the first
            var res = new List<int>(k - 1);
            for (int i = 0; i < k - 1; i++) res.Add(hull[i]);
            return res;
        }

        private static double Cross2D((double u, double w) O,
                                      (double u, double w) A,
                                      (double u, double w) B)
        {
            return (A.u - O.u) * (B.w - O.w) - (A.w - O.w) * (B.u - O.u);
        }

        // ── Point-to-segment distance (2-D) ─────────────────────────────────

        private static double PointToSegmentDist(double px, double py,
            double ax, double ay, double bx, double by)
        {
            double dx = bx - ax, dy = by - ay;
            double lenSq = dx * dx + dy * dy;
            if (lenSq < 1e-20)
                return Math.Sqrt((px - ax) * (px - ax) + (py - ay) * (py - ay));

            double t = Math.Max(0.0, Math.Min(1.0, ((px - ax) * dx + (py - ay) * dy) / lenSq));
            double cx = ax + t * dx, cy = ay + t * dy;
            return Math.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
        }

        // ── Jacobi cyclic eigendecomposition for real symmetric 3×3 ─────────
        // (Duplicated from LivoxLidarPlaneFitter to keep this class self-contained.)

        private static void Jacobi3x3(double[,] A, out double[] eigenvalues, out double[,] V)
        {
            double[,] a = (double[,])A.Clone();
            V = new double[3, 3] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };

            for (int sweep = 0; sweep < MaxJacobiSweeps; sweep++)
            {
                double off = a[0, 1] * a[0, 1] + a[0, 2] * a[0, 2] + a[1, 2] * a[1, 2];
                if (off < JacobiTolerance) break;

                for (int p = 0; p < 2; p++)
                    for (int q = p + 1; q < 3; q++)
                    {
                        double apq = a[p, q];
                        if (Math.Abs(apq) < 1e-30) continue;

                        double theta = 0.5 * Math.Atan2(2.0 * apq, a[q, q] - a[p, p]);
                        double c = Math.Cos(theta), s = Math.Sin(theta);
                        double app = a[p, p], aqq = a[q, q];
                        int r = 3 - p - q;
                        double arp = a[r, p], arq = a[r, q];

                        a[p, p] = c * c * app - 2 * s * c * apq + s * s * aqq;
                        a[q, q] = s * s * app + 2 * s * c * apq + c * c * aqq;
                        a[p, q] = a[q, p] = 0.0;
                        a[r, p] = a[p, r] = c * arp - s * arq;
                        a[r, q] = a[q, r] = s * arp + c * arq;

                        for (int i = 0; i < 3; i++)
                        {
                            double vip = V[i, p], viq = V[i, q];
                            V[i, p] = c * vip - s * viq;
                            V[i, q] = s * vip + c * viq;
                        }
                    }
            }

            eigenvalues = new double[] { a[0, 0], a[1, 1], a[2, 2] };
        }

        private static void SortColumns(ref double[] vals, ref double[,] vecs)
        {
            for (int i = 0; i < 2; i++)
                for (int j = i + 1; j < 3; j++)
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
