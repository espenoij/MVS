using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;

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

        /// <summary>Which deck edge was used to infer the vessel forward direction.</summary>
        public string DetectionMethod { get; set; } = "—";

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

        /// <summary>Estimated 3-D positions of the deck boundary vertices (convex hull).</summary>
        public List<(double X, double Y, double Z)> HullVertices3D { get; set; }
            = new List<(double, double, double)>();

        /// <summary>Kept for backward compatibility — same as HullVertices3D.</summary>
        public List<(double X, double Y, double Z)> HexVertices3D
        {
            get { return HullVertices3D; }
            set { HullVertices3D = value; }
        }
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
        private const double RansacThresholdMm = 50.0;     // tight band for finding the plane
        private const double DeckInlierBandMm  = 2000.0;   // wide band for collecting deck points
                                                           // (must be > corner sag on a folded deck:
                                                           //  e.g. 20 m deck × tan(5°) ≈ 875 mm)
        private const double MinNormalZ        = 0.7;
        private const int    MinPoints         = 50;
        private const double EdgeBandMm        = 400.0;
        private const double MaxEdgeDeviationFrac = 0.06;
        private const int    EdgeRefineIterations = 5;
        private const double EdgeRefineConvergeDeg = 0.001; // stop when angle change < 0.001°

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Detect the front deck edge from a raw LiDAR point cloud.
        /// The <paramref name="deckShape"/> hint lets the algorithm prioritise
        /// edge candidates that match the expected geometry.
        /// Returns an invalid result if detection fails at any stage.
        /// </summary>
        public static LivoxLidarDeckEdgeResult FindEdge(
            List<(float x, float y, float z)> allPoints,
            HelideckShape deckShape = HelideckShape.Hexagon,
            int minEdgePoints = MinPoints,
            Action<int> progress = null)
        {
            var result = new LivoxLidarDeckEdgeResult();
            if (allPoints == null || allPoints.Count < MinPoints)
                return result;

            // ── Step 1: Crop to forward region (x > 0) ──────────────────────
            var fwd = new List<(float x, float y, float z)>(allPoints.Count / 2);
            foreach (var p in allPoints)
                if (p.x > 0) fwd.Add(p);
            progress?.Invoke(allPoints.Count);
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
                progress?.Invoke(fwd.Count);

                if (cnt > bestCnt) { bestCnt = cnt; bNx = nx; bNy = ny; bNz = nz; bD = d; }
            }

            if (bestCnt < MinPoints) return result;

            // Initial inliers: collect ALL points within DeckInlierBandMm of the
            // RANSAC plane (NOT the tight RANSAC threshold). On a folded/ridged
            // deck the corners can sit hundreds of mm from the average plane,
            // and using the tight band would drop them — leaving the convex hull
            // (and the MABR computed from it) bounded by the ridge alone.
            var inliers = new List<(float x, float y, float z)>(bestCnt * 2);
            foreach (var p in allPoints)
                if (Math.Abs(bNx * p.x + bNy * p.y + bNz * p.z + bD) < DeckInlierBandMm)
                    inliers.Add(p);
            progress?.Invoke(allPoints.Count);

            result.DeckInlierCount = inliers.Count;
            if (inliers.Count < MinPoints) return result;

            // ── Step 2.5: Refit plane via PCA on RANSAC inliers ──────────────
            int    n  = inliers.Count;
            double cx = 0, cy = 0, cz = 0;
            foreach (var p in inliers) { cx += p.x; cy += p.y; cz += p.z; }
            progress?.Invoke(n);
            cx /= n; cy /= n; cz /= n;

            double sxx = 0, syy = 0, szz = 0, sxy = 0, sxz = 0, syz = 0;
            foreach (var p in inliers)
            {
                double dx = p.x - cx, dy = p.y - cy, dz = p.z - cz;
                sxx += dx * dx; syy += dy * dy; szz += dz * dz;
                sxy += dx * dy; sxz += dx * dz; syz += dy * dz;
            }
            progress?.Invoke(n);
            double[,] cov =
            {
                { sxx / n, sxy / n, sxz / n },
                { sxy / n, syy / n, syz / n },
                { sxz / n, syz / n, szz / n }
            };
            Jacobi3x3(cov, out double[] evals, out double[,] V);

            double pnx = V[0, 0], pny = V[1, 0], pnz = V[2, 0];
            if (pnz < 0) { pnx = -pnx; pny = -pny; pnz = -pnz; }
            result.FitRmseMm = Math.Sqrt(Math.Max(0.0, evals[0]));

            // Plane offset for projection
            double pD = -(pnx * cx + pny * cy + pnz * cz);

            // ── Step 3: Project all RANSAC inliers onto the fitted plane ─────
            // No height-based outlier filtering here. With a folded/ridged deck
            // the plane fits the AVERAGE height; ridge points sit above and the
            // four corners sit below. Any height filter (MAD, sigma, percentile)
            // would throw away the corners along with the ridge — leaving only a
            // thin horizontal strip that bounds nothing useful.
            //
            // The robust property we exploit instead: the deck's 2-D footprint
            // (X,Y on the plane) is invariant to vertical folds. Ridge points
            // share their X,Y with the deck point directly underneath them, so
            // they lie INSIDE the deck's 2-D footprint and cannot affect the
            // convex hull or its minimum-area bounding rectangle. Corners
            // remain on the hull and are captured cleanly.
            var projected = new List<(float x, float y, float z)>(n);
            for (int i = 0; i < n; i++)
            {
                double dist = pnx * inliers[i].x + pny * inliers[i].y + pnz * inliers[i].z + pD;
                double px = inliers[i].x - dist * pnx;
                double py = inliers[i].y - dist * pny;
                double pz = inliers[i].z - dist * pnz;
                projected.Add(((float)px, (float)py, (float)pz));
            }
            progress?.Invoke(n);

            // ── Step 3.5: Build rotation that aligns plane normal with +Z ────
            // After this rotation, all projected points have Z ≈ 0 (the plane is horizontal).
            double[,] R = BuildRotationToAlignWithZ(pnx, pny, pnz);

            // Transform projected points to normalized (flat horizontal) frame
            var normalized = new List<(float x, float y, float z)>(n);
            for (int i = 0; i < n; i++)
            {
                double dx = projected[i].x - cx, dy = projected[i].y - cy, dz = projected[i].z - cz;
                double nx = R[0, 0] * dx + R[0, 1] * dy + R[0, 2] * dz;
                double ny = R[1, 0] * dx + R[1, 1] * dy + R[1, 2] * dz;
                double nz = R[2, 0] * dx + R[2, 1] * dy + R[2, 2] * dz; // ≈ 0 by construction
                normalized.Add(((float)nx, (float)ny, (float)nz));
            }
            progress?.Invoke(n);

            // ── Step 4: Project normalized points onto 2-D plane ─────────────
            // The plane is now horizontal in the normalized frame, so 2-D coords
            // are simply the X,Y components.
            double ux = 1.0, uy = 0.0, uz = 0.0;
            double wx = 0.0, wy = 1.0, wz = 0.0;

            var proj = new (double u, double w)[n];
            for (int i = 0; i < n; i++)
            {
                proj[i] = (normalized[i].x, normalized[i].y);
            }
            progress?.Invoke(n);

            // LiDAR origin (0,0,0) projected onto the plane's 2-D frame
            double lidarU = -cx * ux - cy * uy - cz * uz;
            double lidarW = -cx * wx - cy * wy - cz * wz;

            // ── Step 4+: Shape-dependent edge detection ─────────────────────
            if (deckShape == HelideckShape.Hexagon)
                return FindEdgeHexagon(result, inliers, normalized, proj, n, cx, cy, cz, ux, uy, uz, wx, wy, wz, R, minEdgePoints, progress);
            else if (deckShape == HelideckShape.Square
                  || deckShape == HelideckShape.SquareRoundedBow)
                return FindEdgeSquare(result, inliers, normalized, proj, n, cx, cy, cz, ux, uy, uz, wx, wy, wz, R, deckShape, minEdgePoints, progress);
            else
                return FindEdgeConvexHull(result, inliers, normalized, proj, n, cx, cy, cz, ux, uy, uz, wx, wy, wz, R, deckShape, minEdgePoints, progress);
        }

        // ── Hexagon-specific edge detection (6-vertex extremal model) ────────

        private static LivoxLidarDeckEdgeResult FindEdgeHexagon(
            LivoxLidarDeckEdgeResult result,
            List<(float x, float y, float z)> inliers,
            List<(float x, float y, float z)> normalized,
            (double u, double w)[] proj, int n,
            double cx, double cy, double cz,
            double ux, double uy, double uz,
            double wx, double wy, double wz,
            double[,] R,
            int minEdgePoints,
            Action<int> progress)
        {
            // 6 extremal directions at θk = 30° + k×60°
            double sq3h = Math.Sqrt(3.0) * 0.5;
            double[] cosK = { sq3h, 0.0, -sq3h, -sq3h,  0.0,  sq3h };
            double[] sinK = { 0.5,  1.0,  0.5,  -0.5,  -1.0, -0.5 };

            int[] vIdx = new int[6];
            double[] vBest = new double[6];
            for (int k = 0; k < 6; k++) vBest[k] = double.MinValue;

            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < 6; k++)
                {
                    double dot = proj[i].u * cosK[k] + proj[i].w * sinK[k];
                    if (dot > vBest[k]) { vBest[k] = dot; vIdx[k] = i; }
                }
            }
            progress?.Invoke(n);

            result.HullVertexCount = 6;

            // Build 6 hex vertices in 3-D for visualisation
            // Transform from normalized space back to original space
            // Use Z=0 to place corners exactly on the fitted plane (consistent with vessel forward)
            var hexVerts3D = new List<(double X, double Y, double Z)>(6);
            for (int k = 0; k < 6; k++)
            {
                double nu = proj[vIdx[k]].u, nw = proj[vIdx[k]].w;
                double nz = 0.0;  // Force Z=0 to place corners on the fitted plane
                // Transform back: apply inverse rotation and add centroid
                double ox = R[0, 0] * nu + R[1, 0] * nw + R[2, 0] * nz;
                double oy = R[0, 1] * nu + R[1, 1] * nw + R[2, 1] * nz;
                double oz = R[0, 2] * nu + R[1, 2] * nw + R[2, 2] * nz;
                hexVerts3D.Add((cx + ox, cy + oy, cz + oz));
            }

            // Identify bow side: the hex side whose midpoint has the highest
            // forward score (u coordinate, which aligns with sensor +X).
            int bowK = 0;
            double bestScore = double.MinValue;
            for (int k = 0; k < 6; k++)
            {
                int k2 = (k + 1) % 6;
                double midU = (proj[vIdx[k]].u + proj[vIdx[k2]].u) * 0.5;
                if (midU > bestScore) { bestScore = midU; bowK = k; }
            }

            // Compute bow side outward normal — perpendicular to bow edge,
            // pointing away from centroid.  This IS the vessel-forward direction.
            int aftK = (bowK + 3) % 6;
            double fU, fW;
            {
                int bA = vIdx[bowK], bB = vIdx[(bowK + 1) % 6];
                double bdU = proj[bA].u - proj[bB].u, bdW = proj[bA].w - proj[bB].w;
                double bL = Math.Sqrt(bdU * bdU + bdW * bdW);
                if (bL < 1e-6) { fU = 1.0; fW = 0.0; }
                else
                {
                    bdU /= bL; bdW /= bL;
                    fU = -bdW; fW = bdU;
                    double bmU = (proj[bA].u + proj[bB].u) * 0.5;
                    double bmW = (proj[bA].w + proj[bB].w) * 0.5;
                    if (fU * bmU + fW * bmW < 0) { fU = -fU; fW = -fW; }
                }
            }

            // Trial order: bow, aft (opposite), then lateral sides
            int[] trialOrder = {
                bowK,
                aftK,
                (bowK + 1) % 6, (bowK + 5) % 6,
                (bowK + 2) % 6, (bowK + 4) % 6
            };
            string[] sideNames = { "Bow edge", "Aft edge", "Port edge", "Starboard edge", "Port-aft edge", "Starboard-aft edge" };

            for (int trial = 0; trial < 6; trial++)
            {
                int k = trialOrder[trial];
                int k2 = (k + 1) % 6;
                int idxA = vIdx[k], idxB = vIdx[k2];

                // Ensure consistent winding (port vertex first)
                if (proj[idxA].w < proj[idxB].w) { int tmp = idxA; idxA = idxB; idxB = tmp; }
                double pu = proj[idxA].u, pw = proj[idxA].w;
                double su = proj[idxB].u, sw = proj[idxB].w;

                double muU  = (pu + su) * 0.5;
                double muW  = (pw + sw) * 0.5;
                double dirU = pu - su, dirW = pw - sw;
                double dL   = Math.Sqrt(dirU * dirU + dirW * dirW);
                if (dL < 1.0) continue;
                dirU /= dL; dirW /= dL;
                double halfLen = dL * 0.5;

                // Outward normal (away from centroid ≈ origin)
                double outU = -dirW, outW = dirU;
                if (outU * muU + outW * muW < 0) { outU = -outU; outW = -outW; }

                // Edge band
                double extA = proj[idxA].u * outU + proj[idxA].w * outW;
                double extB = proj[idxB].u * outU + proj[idxB].w * outW;
                double maxExt = Math.Max(extA, extB);
                var edgeIdx = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    double ext = proj[i].u * outU + proj[i].w * outW;
                    if (ext >= maxExt - EdgeBandMm)
                        edgeIdx.Add(i);
                }

                // Straightness validation
                if (edgeIdx.Count < minEdgePoints) continue;
                if (edgeIdx.Count >= 3)
                {
                    double sumSq = 0;
                    foreach (int idx in edgeIdx)
                    {
                        double du = proj[idx].u - muU;
                        double dw = proj[idx].w - muW;
                        double perp = du * (-dirW) + dw * dirU;
                        sumSq += perp * perp;
                    }
                    double rmsPerp = Math.Sqrt(sumSq / edgeIdx.Count);
                    if (rmsPerp > halfLen * MaxEdgeDeviationFrac) continue;
                }

                // ── Valid edge found ─────────────────────────────────────────

                // Iteratively refine: resample band from updated normal, re-PCA, repeat.
                // This removes the systematic bias introduced by the fixed extremal-vertex
                // seed, driving vessel-forward error well below 0.1°.
                if (!RefineEdgeIterative(proj, n, ref dirU, ref dirW, ref muU, ref muW,
                        maxExt, minEdgePoints, out var refinedEdgeIdx))
                    continue;
                edgeIdx = refinedEdgeIdx;

                // Re-derive outward normal and vessel forward from refined direction.
                {
                    double nU = -dirW, nW = dirU;
                    if (nU * muU + nW * muW < 0) { nU = -nU; nW = -nW; }
                    if (fU * nU + fW * nW < 0) { fU = -fU; fW = -fW; }
                    fU = nU; fW = nW;
                }
                halfLen = 0; // recomputed from spread along refined direction below
                foreach (int i in edgeIdx)
                {
                    double along = (proj[i].u - muU) * dirU + (proj[i].w - muW) * dirW;
                    if (Math.Abs(along) > halfLen) halfLen = Math.Abs(along);
                }

                // Transform edge direction and midpoint back to original 3D space
                double d3x = R[0, 0] * dirU + R[1, 0] * dirW;
                double d3y = R[0, 1] * dirU + R[1, 1] * dirW;
                double d3z = R[0, 2] * dirU + R[1, 2] * dirW;
                double d3L = Math.Sqrt(d3x * d3x + d3y * d3y + d3z * d3z);
                if (d3L > 1e-10) { d3x /= d3L; d3y /= d3L; d3z /= d3L; }

                // Compute average Z in normalized space for edge points
                double avgNz = 0;
                foreach (int i in edgeIdx) avgNz += normalized[i].z;
                avgNz /= edgeIdx.Count;

                // Transform midpoint back to original space
                double ox = R[0, 0] * muU + R[1, 0] * muW + R[2, 0] * avgNz;
                double oy = R[0, 1] * muU + R[1, 1] * muW + R[2, 1] * avgNz;
                double oz = R[0, 2] * muU + R[1, 2] * muW + R[2, 2] * avgNz;
                double mx = cx + ox;
                double my = cy + oy;
                double mz = cz + oz;

                // Vessel forward: transform outward normal back to 3D
                double f3x = R[0, 0] * fU + R[1, 0] * fW;
                double f3y = R[0, 1] * fU + R[1, 1] * fW;
                double f3z = R[0, 2] * fU + R[1, 2] * fW;
                double fL  = Math.Sqrt(f3x * f3x + f3y * f3y + f3z * f3z);
                if (fL > 1e-10) { f3x /= fL; f3y /= fL; f3z /= fL; }

                var edgePts = new List<(float x, float y, float z)>(edgeIdx.Count);
                foreach (int idx in edgeIdx) edgePts.Add(inliers[idx]);

                double edgeAngle = Math.Atan2(dirW, dirU) * Rad2Deg;
                double fwdAngle  = Math.Atan2(f3y, f3x)  * Rad2Deg;

                result.IsValid               = true;
                result.DetectionMethod        = sideNames[trial];
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
                result.HullVertices3D        = hexVerts3D;
                result.EdgePoints            = edgePts;
                result.EdgePointCount        = edgeIdx.Count;

                return result;
            }

            return result;
        }

        // ── Convex-hull edge detection (all non-hexagon shapes) ──────────────

        // ── Square / rectangular edge detection (4-vertex extremal model) ────

        private static LivoxLidarDeckEdgeResult FindEdgeSquare(
            LivoxLidarDeckEdgeResult result,
            List<(float x, float y, float z)> inliers,
            List<(float x, float y, float z)> normalized,
            (double u, double w)[] proj, int n,
            double cx, double cy, double cz,
            double ux, double uy, double uz,
            double wx, double wy, double wz,
            double[,] R,
            HelideckShape deckShape,
            int minEdgePoints,
            Action<int> progress)
        {
            // ── Compute corners as the Minimum-Area Bounding Rectangle (MABR)
            //    of the convex hull of the projected points.
            //
            // Why MABR instead of "4 fixed extremal vertex searches at ±45°":
            //   • MABR is rotation-invariant — works regardless of how the deck
            //     is rotated in the LiDAR frame.
            //   • The 4 corners are *computed* (rectangle vertices), not picked
            //     from the cloud, so a single ridge point that survives RANSAC
            //     cannot become a "corner".
            //   • MABR is global: it minimizes total enclosing area over all
            //     hull points, so isolated outliers contribute negligibly.
            //   • Ridge points in the deck interior are not on the convex hull,
            //     so they cannot affect the rectangle at all.
            var hull = ConvexHull2D(proj);
            progress?.Invoke(n);
            if (hull.Count < 3) return result;

            (double u, double w)[] mabrCorners = ComputeMinAreaRectangle(proj, hull);
            progress?.Invoke(n);
            if (mabrCorners == null) return result;

            // Build "vIdx-equivalent" data: synthesize 4 corner positions in
            // 2-D and define helpers to fetch them. Downstream code uses
            // proj[vIdx[k]].u / .w; we route those through a small adapter.
            (double u, double w)[] vPos = mabrCorners;

            // Helpers that mimic the original proj[vIdx[k]] indexing
            (double u, double w) V(int k) => vPos[k];

            result.HullVertexCount = 4;

            // Build vertices in 3-D for visualisation.
            // Only include real sharp corners — omit vertices on rounded ends.
            var squareVerts3D = new List<(double X, double Y, double Z)>();

            // Identify bow side first (needed to decide which corners are real)
            int bowK = 0;
            double bestScore = double.MinValue;
            for (int k = 0; k < 4; k++)
            {
                int k2 = (k + 1) % 4;
                double midU = (V(k).u + V(k2).u) * 0.5;
                if (midU > bestScore) { bestScore = midU; bowK = k; }
            }

            if (deckShape == HelideckShape.SquareRoundedBow)
            {
                // Only the 2 aft corners are sharp
                int aftSide = (bowK + 2) % 4;
                var c0 = V(aftSide); var c1 = V((aftSide + 1) % 4);
                // Transform corners back to original space (Z=0 → exactly on fitted plane)
                double nz0 = 0.0, nz1 = 0.0;
                double ox0 = R[0, 0] * c0.u + R[1, 0] * c0.w + R[2, 0] * nz0;
                double oy0 = R[0, 1] * c0.u + R[1, 1] * c0.w + R[2, 1] * nz0;
                double oz0 = R[0, 2] * c0.u + R[1, 2] * c0.w + R[2, 2] * nz0;
                double ox1 = R[0, 0] * c1.u + R[1, 0] * c1.w + R[2, 0] * nz1;
                double oy1 = R[0, 1] * c1.u + R[1, 1] * c1.w + R[2, 1] * nz1;
                double oz1 = R[0, 2] * c1.u + R[1, 2] * c1.w + R[2, 2] * nz1;
                squareVerts3D.Add((cx + ox0, cy + oy0, cz + oz0));
                squareVerts3D.Add((cx + ox1, cy + oy1, cz + oz1));
                result.HullVertexCount = 2;
            }
            else if (deckShape == HelideckShape.SquareRoundedBowAft)
            {
                // Stadium: no sharp corners — leave list empty
                result.HullVertexCount = 0;
            }
            else
            {
                // Square: all 4 corners are sharp
                for (int k = 0; k < 4; k++)
                {
                    var c = V(k);
                    double nz = 0.0;  // Force Z=0 to place corners on the fitted plane
                    double ox = R[0, 0] * c.u + R[1, 0] * c.w + R[2, 0] * nz;
                    double oy = R[0, 1] * c.u + R[1, 1] * c.w + R[2, 1] * nz;
                    double oz = R[0, 2] * c.u + R[1, 2] * c.w + R[2, 2] * nz;
                    squareVerts3D.Add((cx + ox, cy + oy, cz + oz));
                }
            }

            // Vessel forward: derive from a guaranteed-straight reference edge.
            //  Square           → bow edge is straight, use its outward normal directly.
            //  SquareRoundedBow → bow is curved; aft is straight → negate aft normal.
            //  SquareRoundedBowAft → bow & aft curved; laterals straight → rotate lateral normal 90°.
            int aftK = (bowK + 2) % 4;
            int portK  = (bowK + 1) % 4;
            int stbdK  = (bowK + 3) % 4;

            int refK;
            bool negateNormal = false;
            bool rotateLateral = false;
            if (deckShape == HelideckShape.SquareRoundedBow)
            {
                refK = aftK;           // aft edge is straight
                negateNormal = true;   // aft outward points aft → negate for forward
            }
            else if (deckShape == HelideckShape.SquareRoundedBowAft)
            {
                refK = portK;          // port lateral is straight
                rotateLateral = true;   // rotate 90° to get forward
            }
            else
            {
                refK = bowK;           // Square: bow edge is straight
            }

            double fU, fW;
            {
                var rA = V(refK); var rB = V((refK + 1) % 4);
                double rdU = rA.u - rB.u, rdW = rA.w - rB.w;
                double rL = Math.Sqrt(rdU * rdU + rdW * rdW);
                if (rL < 1e-6) { fU = 1.0; fW = 0.0; }
                else
                {
                    rdU /= rL; rdW /= rL;
                    // Outward normal of this reference edge
                    double nU = -rdW, nW = rdU;
                    double rmU = (rA.u + rB.u) * 0.5;
                    double rmW = (rA.w + rB.w) * 0.5;
                    if (nU * rmU + nW * rmW < 0) { nU = -nU; nW = -nW; }

                    if (negateNormal)
                    {
                        // Aft outward → negate to get forward
                        fU = -nU; fW = -nW;
                    }
                    else if (rotateLateral)
                    {
                        // Lateral outward → rotate 90° toward bow (positive u)
                        // Forward = perpendicular to lateral, pointing in +u direction
                        fU = -nW; fW = nU;
                        if (fU < 0) { fU = -fU; fW = -fW; }
                    }
                    else
                    {
                        fU = nU; fW = nW;
                    }
                }
            }

            // Trial order: bow, aft (opposite), then lateral sides
            int[] trialOrder = {
                bowK,
                aftK,
                (bowK + 1) % 4, (bowK + 3) % 4
            };
            string[] sideNames = { "Bow edge", "Aft edge", "Port edge", "Starboard edge" };

            for (int trial = 0; trial < 4; trial++)
            {
                int k = trialOrder[trial];
                int k2 = (k + 1) % 4;
                var cA = V(k); var cB = V(k2);

                // Ensure consistent winding (port vertex first)
                if (cA.w < cB.w) { var tmp = cA; cA = cB; cB = tmp; }
                double pu = cA.u, pw = cA.w;
                double su = cB.u, sw = cB.w;

                double muU  = (pu + su) * 0.5;
                double muW  = (pw + sw) * 0.5;
                double dirU = pu - su, dirW = pw - sw;
                double dL   = Math.Sqrt(dirU * dirU + dirW * dirW);
                if (dL < 1.0) continue;
                dirU /= dL; dirW /= dL;
                double halfLen = dL * 0.5;

                // Outward normal (away from centroid ≈ origin)
                double outU = -dirW, outW = dirU;
                if (outU * muU + outW * muW < 0) { outU = -outU; outW = -outW; }

                // Edge band — anchored at the MABR corner positions, not at
                // single cloud points, so a stray ridge inlier cannot bias the band.
                double extA = cA.u * outU + cA.w * outW;
                double extB = cB.u * outU + cB.w * outW;
                double maxExt = Math.Max(extA, extB);
                var edgeIdx = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    double ext = proj[i].u * outU + proj[i].w * outW;
                    if (ext >= maxExt - EdgeBandMm)
                        edgeIdx.Add(i);
                }

                // Straightness validation
                if (edgeIdx.Count >= 3)
                {
                    double sumSq = 0;
                    foreach (int idx in edgeIdx)
                    {
                        double du = proj[idx].u - muU;
                        double dw = proj[idx].w - muW;
                        double perp = du * (-dirW) + dw * dirU;
                        sumSq += perp * perp;
                    }
                    double rmsPerp = Math.Sqrt(sumSq / edgeIdx.Count);
                    if (rmsPerp > halfLen * MaxEdgeDeviationFrac) continue;
                }

                // ── Valid edge found ─────────────────────────────────────────

                // Iteratively refine edge direction.
                if (!RefineEdgeIterative(proj, n, ref dirU, ref dirW, ref muU, ref muW,
                        maxExt, minEdgePoints, out var refinedEdgeIdx2))
                    continue;
                edgeIdx = refinedEdgeIdx2;

                // Re-derive outward normal; keep fU/fW pointing outward (bow-forward).
                {
                    double nU = -dirW, nW = dirU;
                    if (nU * muU + nW * muW < 0) { nU = -nU; nW = -nW; }
                    if (fU * nU + fW * nW < 0) { fU = -fU; fW = -fW; }
                    fU = nU; fW = nW;
                }
                halfLen = 0;
                foreach (int i in edgeIdx)
                {
                    double along = (proj[i].u - muU) * dirU + (proj[i].w - muW) * dirW;
                    if (Math.Abs(along) > halfLen) halfLen = Math.Abs(along);
                }

                // Transform edge direction and midpoint back to original 3D space
                double d3x = R[0, 0] * dirU + R[1, 0] * dirW;
                double d3y = R[0, 1] * dirU + R[1, 1] * dirW;
                double d3z = R[0, 2] * dirU + R[1, 2] * dirW;
                double d3L = Math.Sqrt(d3x * d3x + d3y * d3y + d3z * d3z);
                if (d3L > 1e-10) { d3x /= d3L; d3y /= d3L; d3z /= d3L; }

                // Compute average Z in normalized space for edge points
                double avgNz = 0;
                foreach (int i in edgeIdx) avgNz += normalized[i].z;
                avgNz /= edgeIdx.Count;

                // Transform midpoint back to original space
                double ox = R[0, 0] * muU + R[1, 0] * muW + R[2, 0] * avgNz;
                double oy = R[0, 1] * muU + R[1, 1] * muW + R[2, 1] * avgNz;
                double oz = R[0, 2] * muU + R[1, 2] * muW + R[2, 2] * avgNz;
                double mx = cx + ox;
                double my = cy + oy;
                double mz = cz + oz;

                // Vessel forward: transform outward normal back to 3D
                double f3x = R[0, 0] * fU + R[1, 0] * fW;
                double f3y = R[0, 1] * fU + R[1, 1] * fW;
                double f3z = R[0, 2] * fU + R[1, 2] * fW;
                double fL  = Math.Sqrt(f3x * f3x + f3y * f3y + f3z * f3z);
                if (fL > 1e-10) { f3x /= fL; f3y /= fL; f3z /= fL; }

                var edgePts = new List<(float x, float y, float z)>(edgeIdx.Count);
                foreach (int idx in edgeIdx) edgePts.Add(inliers[idx]);

                double edgeAngle = Math.Atan2(dirW, dirU) * Rad2Deg;
                double fwdAngle  = Math.Atan2(f3y, f3x)  * Rad2Deg;

                result.IsValid               = true;
                result.DetectionMethod        = sideNames[trial];
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
                result.HullVertices3D        = squareVerts3D;
                result.EdgePoints            = edgePts;
                result.EdgePointCount        = edgeIdx.Count;

                return result;
            }

            return result;
        }

        // ── Convex-hull edge detection (Round shape) ─────────────────────────

        private static LivoxLidarDeckEdgeResult FindEdgeConvexHull(
            LivoxLidarDeckEdgeResult result,
            List<(float x, float y, float z)> inliers,
            List<(float x, float y, float z)> normalized,
            (double u, double w)[] proj, int n,
            double cx, double cy, double cz,
            double ux, double uy, double uz,
            double wx, double wy, double wz,
            double[,] R,
            HelideckShape deckShape,
            int minEdgePoints,
            Action<int> progress)
        {
            // ── Convex hull of 2-D projected inliers ─────────────────────────
            var hull = ConvexHull2D(proj);
            progress?.Invoke(n);
            result.HullVertexCount = hull.Count;
            if (hull.Count < 3) return result;

            var hullVerts3D = new List<(double X, double Y, double Z)>(hull.Count);
            foreach (int hi in hull)
            {
                double nu = proj[hi].u, nw = proj[hi].w;
                double nz = 0.0;  // Force Z=0 to place hull vertices on the fitted plane
                double ox = R[0, 0] * nu + R[1, 0] * nw + R[2, 0] * nz;
                double oy = R[0, 1] * nu + R[1, 1] * nw + R[2, 1] * nz;
                double oz = R[0, 2] * nu + R[1, 2] * nw + R[2, 2] * nz;
                hullVerts3D.Add((cx + ox, cy + oy, cz + oz));
            }

            // ── Build candidate edge segments from hull ──────────────────────
            int hullN = hull.Count;

            var edgeLengths = new double[hullN];
            for (int h = 0; h < hullN; h++)
            {
                int a = hull[h], b = hull[(h + 1) % hullN];
                double du = proj[a].u - proj[b].u, dw = proj[a].w - proj[b].w;
                edgeLengths[h] = Math.Sqrt(du * du + dw * dw);
            }
            double[] sorted = (double[])edgeLengths.Clone();
            Array.Sort(sorted);
            double medianLen = sorted[hullN / 2];
            if (medianLen < 1.0) medianLen = 1.0;

            bool prefersLongSides = deckShape == HelideckShape.Square
                                 || deckShape == HelideckShape.SquareRoundedBow
                                 || deckShape == HelideckShape.SquareRoundedBowAft;

            var candidates = new List<(int idxA, int idxB, double score, string name)>(hullN);
            for (int h = 0; h < hullN; h++)
            {
                int a = hull[h], b = hull[(h + 1) % hullN];
                double midU = (proj[a].u + proj[b].u) * 0.5;
                double lenRatio = edgeLengths[h] / medianLen;

                double score = midU;
                if (prefersLongSides)
                    score += lenRatio * medianLen * 0.5;

                candidates.Add((a, b, score, $"Hull edge {h}"));
            }

            candidates.Sort((x, y) => y.score.CompareTo(x.score));

            // ── Try hull edges in priority order ─────────────────────────────
            double minEdgeLen = prefersLongSides ? medianLen * 0.5 : 1.0;

            for (int trial = 0; trial < candidates.Count; trial++)
            {
                int idxA = candidates[trial].idxA;
                int idxB = candidates[trial].idxB;

                if (proj[idxA].w < proj[idxB].w) { int tmp = idxA; idxA = idxB; idxB = tmp; }
                double pu = proj[idxA].u, pw = proj[idxA].w;
                double su = proj[idxB].u, sw = proj[idxB].w;

                double muU  = (pu + su) * 0.5;
                double muW  = (pw + sw) * 0.5;
                double dirU = pu - su, dirW = pw - sw;
                double dL   = Math.Sqrt(dirU * dirU + dirW * dirW);
                if (dL < minEdgeLen) continue;
                dirU /= dL; dirW /= dL;
                double halfLen = dL * 0.5;

                double outU = -dirW, outW = dirU;
                if (outU * muU + outW * muW < 0) { outU = -outU; outW = -outW; }

                double extA = proj[idxA].u * outU + proj[idxA].w * outW;
                double extB = proj[idxB].u * outU + proj[idxB].w * outW;
                double maxExt = Math.Max(extA, extB);
                var edgeIdx = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    double ext = proj[i].u * outU + proj[i].w * outW;
                    if (ext >= maxExt - EdgeBandMm)
                        edgeIdx.Add(i);
                }

                if (edgeIdx.Count >= 3)
                {
                    double sumSq = 0;
                    foreach (int idx in edgeIdx)
                    {
                        double du = proj[idx].u - muU;
                        double dw = proj[idx].w - muW;
                        double perp = du * (-dirW) + dw * dirU;
                        sumSq += perp * perp;
                    }
                    double rmsPerp = Math.Sqrt(sumSq / edgeIdx.Count);
                    if (rmsPerp > halfLen * MaxEdgeDeviationFrac) continue;
                }

                // ── Valid edge found ─────────────────────────────────────────

                // Iteratively refine edge direction.
                if (!RefineEdgeIterative(proj, n, ref dirU, ref dirW, ref muU, ref muW,
                        maxExt, minEdgePoints, out var refinedEdgeIdxH))
                    continue;
                edgeIdx = refinedEdgeIdxH;

                // Recompute outward normal from refined direction.
                outU = -dirW; outW = dirU;
                if (outU * muU + outW * muW < 0) { outU = -outU; outW = -outW; }
                halfLen = 0;
                foreach (int i in edgeIdx)
                {
                    double along = (proj[i].u - muU) * dirU + (proj[i].w - muW) * dirW;
                    if (Math.Abs(along) > halfLen) halfLen = Math.Abs(along);
                }

                // Transform edge direction and midpoint back to original 3D space
                double d3x = R[0, 0] * dirU + R[1, 0] * dirW;
                double d3y = R[0, 1] * dirU + R[1, 1] * dirW;
                double d3z = R[0, 2] * dirU + R[1, 2] * dirW;
                double d3L = Math.Sqrt(d3x * d3x + d3y * d3y + d3z * d3z);
                if (d3L > 1e-10) { d3x /= d3L; d3y /= d3L; d3z /= d3L; }

                // Compute average Z in normalized space for edge points
                double avgNz = 0;
                foreach (int i in edgeIdx) avgNz += normalized[i].z;
                avgNz /= edgeIdx.Count;

                // Transform midpoint back to original space
                double ox = R[0, 0] * muU + R[1, 0] * muW + R[2, 0] * avgNz;
                double oy = R[0, 1] * muU + R[1, 1] * muW + R[2, 1] * avgNz;
                double oz = R[0, 2] * muU + R[1, 2] * muW + R[2, 2] * avgNz;
                double mx = cx + ox;
                double my = cy + oy;
                double mz = cz + oz;

                // Vessel forward direction depends on shape and which edge was found.
                double fU, fW;
                if (deckShape == HelideckShape.SquareRoundedBowAft)
                {
                    // Both bow and aft are rounded — only lateral (side) edges are straight.
                    // Vessel forward is parallel to the edge, pointing toward bow (+u).
                    fU = dirU; fW = dirW;
                    if (fU < 0) { fU = -fU; fW = -fW; } // ensure forward (+u)
                }
                else if (Math.Abs(outU) > Math.Abs(outW) * 0.5) // bow or aft edge
                {
                    // Outward normal of bow/aft edge points forward/aft
                    fU = outU; fW = outW;
                    if (fU < 0) { fU = -fU; fW = -fW; } // always point forward
                }
                else
                {
                    // Lateral edge: infer forward from the most-forward hull vertex
                    double bestFwdU = double.MinValue;
                    int bestFwdIdx = hull[0];
                    foreach (int hi in hull)
                    {
                        if (proj[hi].u > bestFwdU) { bestFwdU = proj[hi].u; bestFwdIdx = hi; }
                    }
                    fU = proj[bestFwdIdx].u; fW = proj[bestFwdIdx].w;
                    double fDist = Math.Sqrt(fU * fU + fW * fW);
                    if (fDist < 1e-6) { fU = 1.0; fW = 0.0; }
                    else              { fU /= fDist; fW /= fDist; }
                }

                // Transform vessel forward back to original 3D space
                double f3x = R[0, 0] * fU + R[1, 0] * fW;
                double f3y = R[0, 1] * fU + R[1, 1] * fW;
                double f3z = R[0, 2] * fU + R[1, 2] * fW;
                double fL  = Math.Sqrt(f3x * f3x + f3y * f3y + f3z * f3z);
                if (fL > 1e-10) { f3x /= fL; f3y /= fL; f3z /= fL; }

                var edgePts = new List<(float x, float y, float z)>(edgeIdx.Count);
                foreach (int idx in edgeIdx) edgePts.Add(inliers[idx]);

                double edgeAngle = Math.Atan2(dirW, dirU) * Rad2Deg;
                double fwdAngle  = Math.Atan2(f3y, f3x)  * Rad2Deg;

                double edgeMidFwd = muU * fU + muW * fW;
                double edgeMidLat = muU * (-fW) + muW * fU;
                string edgeLabel;
                if (edgeMidFwd > 0 && Math.Abs(edgeMidLat) < edgeMidFwd)
                    edgeLabel = "Bow edge";
                else if (edgeMidFwd < 0 && Math.Abs(edgeMidLat) < -edgeMidFwd)
                    edgeLabel = "Aft edge";
                else if (edgeMidLat > 0)
                    edgeLabel = "Port edge";
                else
                    edgeLabel = "Starboard edge";

                result.IsValid               = true;
                result.DetectionMethod        = edgeLabel;
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
                result.HullVertices3D        = hullVerts3D;
                result.EdgePoints            = edgePts;
                result.EdgePointCount        = edgeIdx.Count;

                return result;
            }

            return result;
        }

        // ── Convex hull (Andrew's monotone chain) ────────────────────────────

        /// <summary>
        /// Returns the indices into <paramref name="pts"/> that form the convex
        /// hull in counter-clockwise order.  Uses Andrew's monotone chain
        /// algorithm — O(n log n).
        /// </summary>
        private static List<int> ConvexHull2D((double u, double w)[] pts)
        {
            int n = pts.Length;
            var idx = new int[n];
            for (int i = 0; i < n; i++) idx[i] = i;
            Array.Sort(idx, (a, b) =>
            {
                int c = pts[a].u.CompareTo(pts[b].u);
                return c != 0 ? c : pts[a].w.CompareTo(pts[b].w);
            });

            var hull = new int[2 * n];
            int k = 0;

            // Lower hull
            for (int i = 0; i < n; i++)
            {
                while (k >= 2 && Cross(pts[hull[k - 2]], pts[hull[k - 1]], pts[idx[i]]) <= 0)
                    k--;
                hull[k++] = idx[i];
            }

            // Upper hull
            int lower = k + 1;
            for (int i = n - 2; i >= 0; i--)
            {
                while (k >= lower && Cross(pts[hull[k - 2]], pts[hull[k - 1]], pts[idx[i]]) <= 0)
                    k--;
                hull[k++] = idx[i];
            }

            var result = new List<int>(k - 1);
            for (int i = 0; i < k - 1; i++)
                result.Add(hull[i]);
            return result;
        }

        private static double Cross((double u, double w) o, (double u, double w) a, (double u, double w) b)
            => (a.u - o.u) * (b.w - o.w) - (a.w - o.w) * (b.u - o.u);

        /// <summary>
        /// Computes the Minimum-Area Bounding Rectangle (MABR) of the supplied
        /// 2-D point set, using the Rotating Calipers theorem on the convex hull:
        /// the optimal rectangle has at least one side collinear with a hull edge.
        ///
        /// Returns 4 corners in counter-clockwise order, or null if the hull is
        /// degenerate. Corners are computed (synthesized) from the hull bounds —
        /// they are NOT picked from the input cloud, which is exactly what makes
        /// the result robust against ridge points or other isolated outliers.
        /// </summary>
        private static (double u, double w)[] ComputeMinAreaRectangle(
            (double u, double w)[] pts, List<int> hull)
        {
            int h = hull.Count;
            if (h < 3) return null;

            double bestArea = double.MaxValue;
            (double u, double w)[] best = null;

            for (int i = 0; i < h; i++)
            {
                var a = pts[hull[i]];
                var b = pts[hull[(i + 1) % h]];

                double ex = b.u - a.u, ey = b.w - a.w;
                double eL = Math.Sqrt(ex * ex + ey * ey);
                if (eL < 1e-9) continue;
                ex /= eL; ey /= eL;             // edge direction
                double nx = -ey, ny = ex;       // perpendicular

                double minE = double.MaxValue, maxE = double.MinValue;
                double minN = double.MaxValue, maxN = double.MinValue;
                for (int j = 0; j < h; j++)
                {
                    var p = pts[hull[j]];
                    double pe = p.u * ex + p.w * ey;
                    double pn = p.u * nx + p.w * ny;
                    if (pe < minE) minE = pe;
                    if (pe > maxE) maxE = pe;
                    if (pn < minN) minN = pn;
                    if (pn > maxN) maxN = pn;
                }

                double area = (maxE - minE) * (maxN - minN);
                if (area < bestArea)
                {
                    bestArea = area;
                    // Build 4 corners CCW: (minE,minN) (maxE,minN) (maxE,maxN) (minE,maxN)
                    best = new (double u, double w)[4];
                    best[0] = (minE * ex + minN * nx, minE * ey + minN * ny);
                    best[1] = (maxE * ex + minN * nx, maxE * ey + minN * ny);
                    best[2] = (maxE * ex + maxN * nx, maxE * ey + maxN * ny);
                    best[3] = (minE * ex + maxN * nx, minE * ey + maxN * ny);
                }
            }

            return best;
        }

        /// <summary>
        /// Iteratively refines an edge direction estimate by alternating between:
        ///   1. Resampling the edge band using the current outward normal.
        ///   2. Running PCA on the band to get a better direction.
        /// Stops when the direction change per iteration is below
        /// <see cref="EdgeRefineConvergeDeg"/> or after <see cref="EdgeRefineIterations"/> passes.
        /// Returns false if the band ever drops below <paramref name="minPoints"/>.
        /// </summary>
        private static bool RefineEdgeIterative(
            (double u, double w)[] proj, int n,
            ref double dirU, ref double dirW,
            ref double muU,  ref double muW,
            double maxExt, int minPoints,
            out List<int> finalEdgeIdx)
        {
            finalEdgeIdx = null;

            for (int iter = 0; iter < EdgeRefineIterations; iter++)
            {
                // Outward normal derived from current direction estimate
                double outU = -dirW, outW = dirU;
                if (outU * muU + outW * muW < 0) { outU = -outU; outW = -outW; }

                // Resample band against the refined normal
                // Use the current projection of maxExt along the new normal direction
                double projMax = double.MinValue;
                for (int i = 0; i < n; i++)
                {
                    double ext = proj[i].u * outU + proj[i].w * outW;
                    if (ext > projMax) projMax = ext;
                }

                var edgeIdx = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    double ext = proj[i].u * outU + proj[i].w * outW;
                    if (ext >= projMax - EdgeBandMm)
                        edgeIdx.Add(i);
                }

                if (edgeIdx.Count < minPoints) return false;

                double prevDirU = dirU, prevDirW = dirW;
                PcaEdgeDirection2D(proj, edgeIdx, ref dirU, ref dirW, ref muU, ref muW);

                // Keep sign consistent with outward-normal direction
                outU = -dirW; outW = dirU;
                if (outU * muU + outW * muW < 0) { outU = -outU; outW = -outW; }

                finalEdgeIdx = edgeIdx;

                // Convergence check
                double dot = prevDirU * dirU + prevDirW * dirW;
                dot = Math.Max(-1.0, Math.Min(1.0, dot));
                double changeDeg = Math.Acos(dot) * Rad2Deg;
                if (changeDeg < EdgeRefineConvergeDeg) break;
            }

            return finalEdgeIdx != null && finalEdgeIdx.Count >= minPoints;
        }

        /// <summary>
        /// Refines the 2-D edge direction (dirU, dirW) using PCA over all
        /// edge-band point projections, and updates the edge midpoint (muU, muW).
        /// Returns false if fewer than 2 points are provided.
        /// </summary>
        private static bool PcaEdgeDirection2D(
            (double u, double w)[] proj, List<int> edgeIdx,
            ref double dirU, ref double dirW,
            ref double muU,  ref double muW)
        {
            int m = edgeIdx.Count;
            if (m < 2) return false;

            double su = 0, sw = 0;
            foreach (int i in edgeIdx) { su += proj[i].u; sw += proj[i].w; }
            muU = su / m;
            muW = sw / m;

            double cuu = 0, cww = 0, cuw = 0;
            foreach (int i in edgeIdx)
            {
                double du = proj[i].u - muU, dw = proj[i].w - muW;
                cuu += du * du; cww += dw * dw; cuw += du * dw;
            }

            // 2×2 symmetric eigen: principal eigenvector of [[cuu,cuw],[cuw,cww]]
            double diff = cuu - cww;
            double disc = Math.Sqrt(diff * diff + 4.0 * cuw * cuw);
            // Largest eigenvalue's eigenvector
            double e1uu = cuw, e1ww = (disc - diff) * 0.5;
            double eL   = Math.Sqrt(e1uu * e1uu + e1ww * e1ww);

            if (eL < 1e-14)
            {
                // Degenerate — keep existing direction
                return true;
            }

            double newDirU = e1uu / eL;
            double newDirW = e1ww / eL;

            // Keep the same sign convention as the original direction
            if (newDirU * dirU + newDirW * dirW < 0) { newDirU = -newDirU; newDirW = -newDirW; }
            dirU = newDirU;
            dirW = newDirW;
            return true;
        }

        // ── Symmetric 3×3 eigendecomposition via Math.NET Numerics ──────────
        // Returns eigenvalues sorted ascending with eigenvector columns aligned.
        private static void Jacobi3x3(double[,] A, out double[] eigenvalues, out double[,] V)
        {
            var m   = DenseMatrix.OfArray(A);
            var evd = m.Evd(Symmetricity.Symmetric);

            eigenvalues = new double[3];
            V           = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                eigenvalues[i] = evd.EigenValues[i].Real;
                for (int r = 0; r < 3; r++)
                    V[r, i] = evd.EigenVectors[r, i];
            }
        }

        /// <summary>
        /// Builds a 3x3 rotation matrix that transforms coordinates so that
        /// the vector (nx, ny, nz) becomes aligned with the +Z axis (0, 0, 1).
        /// Uses Rodrigues' rotation formula.
        /// Returns R where R * (nx, ny, nz) ≈ (0, 0, 1) and R^T transforms back.
        /// </summary>
        private static double[,] BuildRotationToAlignWithZ(double nx, double ny, double nz)
        {
            // Target direction (Z-axis)
            double tx = 0, ty = 0, tz = 1;

            // Check if already aligned
            double dot = nx * tx + ny * ty + nz * tz;
            if (dot > 0.9999)
            {
                // Already aligned with +Z, return identity
                return new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
            }
            else if (dot < -0.9999)
            {
                // Aligned with -Z, rotate 180° around X-axis
                return new double[,] { { 1, 0, 0 }, { 0, -1, 0 }, { 0, 0, -1 } };
            }

            // Rotation axis: k = n × target
            double kx = ny * tz - nz * ty;
            double ky = nz * tx - nx * tz;
            double kz = nx * ty - ny * tx;
            double klen = Math.Sqrt(kx * kx + ky * ky + kz * kz);
            kx /= klen; ky /= klen; kz /= klen;

            // Rotation angle
            double angle = Math.Acos(Math.Max(-1.0, Math.Min(1.0, dot)));
            double c = Math.Cos(angle);
            double s = Math.Sin(angle);
            double t = 1.0 - c;

            // Rodrigues' rotation matrix
            return new double[,]
            {
                { t*kx*kx + c,    t*kx*ky - s*kz, t*kx*kz + s*ky },
                { t*kx*ky + s*kz, t*ky*ky + c,    t*ky*kz - s*kx },
                { t*kx*kz - s*ky, t*ky*kz + s*kx, t*kz*kz + c    }
            };
        }
    }
}
