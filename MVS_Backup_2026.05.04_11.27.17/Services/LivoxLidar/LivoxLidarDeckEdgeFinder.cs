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
        private const double RansacThresholdMm = 50.0;
        private const double MinNormalZ        = 0.7;
        private const int    MinPoints         = 50;
        private const double EdgeBandMm        = 400.0;
        private const double MaxEdgeDeviationFrac = 0.06;
        private const int    MaxJacobiSweeps   = 50;
        private const double JacobiTolerance   = 1e-14;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Detect the front deck edge from a raw LiDAR point cloud.
        /// The <paramref name="deckShape"/> hint lets the algorithm prioritise
        /// edge candidates that match the expected geometry.
        /// Returns an invalid result if detection fails at any stage.
        /// </summary>
        public static LivoxLidarDeckEdgeResult FindEdge(
            List<(float x, float y, float z)> allPoints,
            HelideckShape deckShape = HelideckShape.Hexagon)
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

            var inliers = new List<(float x, float y, float z)>(bestCnt * 2);
            foreach (var p in allPoints)
                if (Math.Abs(bNx * p.x + bNy * p.y + bNz * p.z + bD) < RansacThresholdMm)
                    inliers.Add(p);
            result.DeckInlierCount = inliers.Count;
            if (inliers.Count < MinPoints) return result;

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

            // ── Step 4+: Shape-dependent edge detection ─────────────────────
            if (deckShape == HelideckShape.Hexagon)
                return FindEdgeHexagon(result, inliers, proj, n, cx, cy, cz, ux, uy, uz, wx, wy, wz);
            else if (deckShape == HelideckShape.Square
                  || deckShape == HelideckShape.SquareRoundedBow)
                return FindEdgeSquare(result, inliers, proj, n, cx, cy, cz, ux, uy, uz, wx, wy, wz, deckShape);
            else
                return FindEdgeConvexHull(result, inliers, proj, n, cx, cy, cz, ux, uy, uz, wx, wy, wz, deckShape);
        }

        // ── Hexagon-specific edge detection (6-vertex extremal model) ────────

        private static LivoxLidarDeckEdgeResult FindEdgeHexagon(
            LivoxLidarDeckEdgeResult result,
            List<(float x, float y, float z)> inliers,
            (double u, double w)[] proj, int n,
            double cx, double cy, double cz,
            double ux, double uy, double uz,
            double wx, double wy, double wz)
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

            result.HullVertexCount = 6;

            // Build 6 hex vertices in 3-D for visualisation
            var hexVerts3D = new List<(double X, double Y, double Z)>(6);
            for (int k = 0; k < 6; k++)
            {
                double hu = proj[vIdx[k]].u, hw = proj[vIdx[k]].w;
                hexVerts3D.Add((cx + hu * ux + hw * wx,
                                cy + hu * uy + hw * wy,
                                cz + hu * uz + hw * wz));
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

                double d3x = dirU * ux + dirW * wx, d3y = dirU * uy + dirW * wy, d3z = dirU * uz + dirW * wz;
                double d3L = Math.Sqrt(d3x * d3x + d3y * d3y + d3z * d3z);
                if (d3L > 1e-10) { d3x /= d3L; d3y /= d3L; d3z /= d3L; }

                double mx = cx + muU * ux + muW * wx;
                double my = cy + muU * uy + muW * wy;
                double mz = cz + muU * uz + muW * wz;

                // Vessel forward: outward normal of the bow side (perpendicular by construction)
                double f3x = fU * ux + fW * wx, f3y = fU * uy + fW * wy, f3z = fU * uz + fW * wz;
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
            (double u, double w)[] proj, int n,
            double cx, double cy, double cz,
            double ux, double uy, double uz,
            double wx, double wy, double wz,
            HelideckShape deckShape)
        {
            // 4 extremal directions at θk = 45° + k×90°  →  finds the 4 corners
            double sq2h = Math.Sqrt(2.0) * 0.5;
            double[] cosK = {  sq2h, -sq2h, -sq2h,  sq2h };
            double[] sinK = {  sq2h,  sq2h, -sq2h, -sq2h };

            int[] vIdx = new int[4];
            double[] vBest = new double[4];
            for (int k = 0; k < 4; k++) vBest[k] = double.MinValue;

            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < 4; k++)
                {
                    double dot = proj[i].u * cosK[k] + proj[i].w * sinK[k];
                    if (dot > vBest[k]) { vBest[k] = dot; vIdx[k] = i; }
                }
            }

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
                double midU = (proj[vIdx[k]].u + proj[vIdx[k2]].u) * 0.5;
                if (midU > bestScore) { bestScore = midU; bowK = k; }
            }

            if (deckShape == HelideckShape.SquareRoundedBow)
            {
                // Only the 2 aft corners are sharp
                int aftSide = (bowK + 2) % 4;
                int c0 = vIdx[aftSide], c1 = vIdx[(aftSide + 1) % 4];
                squareVerts3D.Add((cx + proj[c0].u * ux + proj[c0].w * wx,
                                   cy + proj[c0].u * uy + proj[c0].w * wy,
                                   cz + proj[c0].u * uz + proj[c0].w * wz));
                squareVerts3D.Add((cx + proj[c1].u * ux + proj[c1].w * wx,
                                   cy + proj[c1].u * uy + proj[c1].w * wy,
                                   cz + proj[c1].u * uz + proj[c1].w * wz));
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
                    double hu = proj[vIdx[k]].u, hw = proj[vIdx[k]].w;
                    squareVerts3D.Add((cx + hu * ux + hw * wx,
                                       cy + hu * uy + hw * wy,
                                       cz + hu * uz + hw * wz));
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
                int rA = vIdx[refK], rB = vIdx[(refK + 1) % 4];
                double rdU = proj[rA].u - proj[rB].u, rdW = proj[rA].w - proj[rB].w;
                double rL = Math.Sqrt(rdU * rdU + rdW * rdW);
                if (rL < 1e-6) { fU = 1.0; fW = 0.0; }
                else
                {
                    rdU /= rL; rdW /= rL;
                    // Outward normal of this reference edge
                    double nU = -rdW, nW = rdU;
                    double rmU = (proj[rA].u + proj[rB].u) * 0.5;
                    double rmW = (proj[rA].w + proj[rB].w) * 0.5;
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

                double d3x = dirU * ux + dirW * wx, d3y = dirU * uy + dirW * wy, d3z = dirU * uz + dirW * wz;
                double d3L = Math.Sqrt(d3x * d3x + d3y * d3y + d3z * d3z);
                if (d3L > 1e-10) { d3x /= d3L; d3y /= d3L; d3z /= d3L; }

                double mx = cx + muU * ux + muW * wx;
                double my = cy + muU * uy + muW * wy;
                double mz = cz + muU * uz + muW * wz;

                // Vessel forward: outward normal of the bow side (perpendicular by construction)
                double f3x = fU * ux + fW * wx, f3y = fU * uy + fW * wy, f3z = fU * uz + fW * wz;
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
            (double u, double w)[] proj, int n,
            double cx, double cy, double cz,
            double ux, double uy, double uz,
            double wx, double wy, double wz,
            HelideckShape deckShape)
        {
            // ── Convex hull of 2-D projected inliers ─────────────────────────
            var hull = ConvexHull2D(proj);
            result.HullVertexCount = hull.Count;
            if (hull.Count < 3) return result;

            var hullVerts3D = new List<(double X, double Y, double Z)>(hull.Count);
            foreach (int hi in hull)
            {
                double hu = proj[hi].u, hw = proj[hi].w;
                hullVerts3D.Add((cx + hu * ux + hw * wx,
                                 cy + hu * uy + hw * wy,
                                 cz + hu * uz + hw * wz));
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

                double d3x = dirU * ux + dirW * wx, d3y = dirU * uy + dirW * wy, d3z = dirU * uz + dirW * wz;
                double d3L = Math.Sqrt(d3x * d3x + d3y * d3y + d3z * d3z);
                if (d3L > 1e-10) { d3x /= d3L; d3y /= d3L; d3z /= d3L; }

                double mx = cx + muU * ux + muW * wx;
                double my = cy + muU * uy + muW * wy;
                double mz = cz + muU * uz + muW * wz;

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

                double f3x = fU * ux + fW * wx, f3y = fU * uy + fW * wy, f3z = fU * uz + fW * wz;
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
