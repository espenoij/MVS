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

        /// <summary>Estimated 3-D positions of the 6 hexagon deck vertices.</summary>
        public List<(double X, double Y, double Z)> HexVertices3D { get; set; }
            = new List<(double, double, double)>();
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

            // ── Step 4: Detect all 6 hex vertex candidates ───────────────────
            // Each vertex of a flat-side regular hexagon is the unique maximiser
            // of a linear objective oriented in that vertex's outward direction
            // θk = 30° + k·60°.  A single pass over all projected inliers
            // evaluates all 6 objectives simultaneously.
            double sq3h = Math.Sqrt(3.0) * 0.5;          // ≈ 0.866
            //                              k= 0     1      2      3      4      5
            double[] cosK = {  sq3h,  0.0, -sq3h, -sq3h,  0.0,  sq3h }; // cos θk
            double[] sinK = {   0.5,  1.0,   0.5,  -0.5, -1.0,  -0.5 }; // sin θk
            int[]    vIdx = new int[6];
            double[] vBest = { double.MinValue, double.MinValue, double.MinValue,
                               double.MinValue, double.MinValue, double.MinValue };
            for (int i = 0; i < n; i++)
            {
                double ui = proj[i].u, wi = proj[i].w;
                for (int k = 0; k < 6; k++)
                {
                    double s = ui * cosK[k] + wi * sinK[k];
                    if (s > vBest[k]) { vBest[k] = s; vIdx[k] = i; }
                }
            }

            // ── Step 5: Identify the bow edge from the 6 detected vertices ───
            // The bow side is the adjacent vertex pair (k, k+1 mod 6) whose
            // combined forward component is greatest — i.e. the side most
            // directly facing the sensor.
            int    bowK     = 0;
            double bowScore = double.MinValue;
            for (int k = 0; k < 6; k++)
            {
                double score = proj[vIdx[k]].u + proj[vIdx[(k + 1) % 6]].u;
                if (score > bowScore) { bowScore = score; bowK = k; }
            }
            // Assign port (higher W) / stbd (lower W) to the two bow-edge vertices
            int idxA = vIdx[bowK], idxB = vIdx[(bowK + 1) % 6];
            if (proj[idxA].w < proj[idxB].w) { int tmp = idxA; idxA = idxB; idxB = tmp; }
            double pu = proj[idxA].u, pw = proj[idxA].w;  // bow-port corner (2-D)
            double su = proj[idxB].u, sw = proj[idxB].w;  // bow-stbd corner (2-D)

            // ── Step 6: Bow edge geometry + vessel forward ────────────────────
            double muU  = (pu + su) * 0.5;
            double muW  = (pw + sw) * 0.5;
            double dirU = pu - su, dirW = pw - sw;
            double dL   = Math.Sqrt(dirU * dirU + dirW * dirW);
            if (dL < 1.0) return result;               // degenerate: corners coincide
            dirU /= dL; dirW /= dL;
            double halfLen = dL * 0.5;

            double d3x = dirU * ux + dirW * wx, d3y = dirU * uy + dirW * wy, d3z = dirU * uz + dirW * wz;
            double d3L = Math.Sqrt(d3x * d3x + d3y * d3y + d3z * d3z);
            if (d3L > 1e-10) { d3x /= d3L; d3y /= d3L; d3z /= d3L; }

            double mx = cx + muU * ux + muW * wx;
            double my = cy + muU * uy + muW * wy;
            double mz = cz + muU * uz + muW * wz;

            // Vessel forward: perpendicular to edge in the deck plane, toward sensor
            double fU = -dirW, fW = dirU;
            if (fU * (muU - lidarU) + fW * (muW - lidarW) < 0) { fU = -fU; fW = -fW; }
            double f3x = fU * ux + fW * wx, f3y = fU * uy + fW * wy, f3z = fU * uz + fW * wz;
            double fL  = Math.Sqrt(f3x * f3x + f3y * f3y + f3z * f3z);
            if (fL > 1e-10) { f3x /= fL; f3y /= fL; f3z /= fL; }

            // ── Step 7: Complete the hexagon — derive all 6 vertex positions ──
            // The two bow corners are detected from data; the remaining 4 are
            // derived from regular-hexagon geometry.
            // Circumradius r = 2·halfLen, apothem = halfLen·√3,
            // hex centre = bow-edge midpoint shifted one apothem toward stern.
            double hexR   = halfLen * 2.0;
            double hexApo = halfLen * Math.Sqrt(3.0);
            double hcX    = mx - hexApo * f3x;
            double hcY    = my - hexApo * f3y;
            double hcZ    = mz - hexApo * f3z;
            var hexVerts = new List<(double X, double Y, double Z)>
            {
                HexVert(hcX, hcY, hcZ, hexR,  sq3h, +0.5, d3x, d3y, d3z, f3x, f3y, f3z), // bow-port   (detected)
                HexVert(hcX, hcY, hcZ, hexR,  0.0,  +1.0, d3x, d3y, d3z, f3x, f3y, f3z), // port       (derived)
                HexVert(hcX, hcY, hcZ, hexR, -sq3h, +0.5, d3x, d3y, d3z, f3x, f3y, f3z), // stern-port (derived)
                HexVert(hcX, hcY, hcZ, hexR, -sq3h, -0.5, d3x, d3y, d3z, f3x, f3y, f3z), // stern-stbd (derived)
                HexVert(hcX, hcY, hcZ, hexR,  0.0,  -1.0, d3x, d3y, d3z, f3x, f3y, f3z), // stbd       (derived)
                HexVert(hcX, hcY, hcZ, hexR,  sq3h, -0.5, d3x, d3y, d3z, f3x, f3y, f3z), // bow-stbd   (detected)
            };

            // ── Step 8: Collect edge-band points for visualisation ────────────
            double uFront = Math.Max(pu, su);
            var edgeIdx = new List<int>();
            for (int i = 0; i < n; i++)
                if (proj[i].u >= uFront - EdgeBandMm)
                    edgeIdx.Add(i);
            result.EdgePointCount = edgeIdx.Count;
            var edgePts = new List<(float x, float y, float z)>(edgeIdx.Count);
            foreach (int idx in edgeIdx) edgePts.Add(inliers[idx]);

            double edgeAngle = Math.Atan2(dirW, dirU) * Rad2Deg;
            double fwdAngle  = Math.Atan2(f3y, f3x)  * Rad2Deg;

            // ── Step 9: Populate result ──────────────────────────────────────
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
            result.HalfLength            = halfLen;
            result.HexVertices3D         = hexVerts;
            result.EdgePoints            = edgePts;

            return result;
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

        // Returns the 3-D position of one hex vertex.
        // fwdComp / edgeComp are the coefficients along the vessel-forward and edge-direction axes.
        private static (double X, double Y, double Z) HexVert(
            double cx, double cy, double cz, double r,
            double fwdComp, double edgeComp,
            double d3x, double d3y, double d3z,
            double f3x, double f3y, double f3z)
            => (cx + r * (fwdComp * f3x + edgeComp * d3x),
                cy + r * (fwdComp * f3y + edgeComp * d3y),
                cz + r * (fwdComp * f3z + edgeComp * d3z));
    }
}
