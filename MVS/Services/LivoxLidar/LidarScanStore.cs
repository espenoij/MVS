using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace MVS
{
    /// <summary>
    /// Persists LiDAR scan data (raw point cloud + plane-fit result + deck-edge
    /// result) per project so that Step 2 can be restored when a project is
    /// re-opened. Data is stored as a compact binary file per project under
    /// %APPDATA%\MVS\LidarScans\scan_{projectId}.bin.
    ///
    /// File format:
    ///   int32  magic   (0x4C534341 = "LSCA")
    ///   int32  version (1)
    ///   int32  pointCount
    ///   pointCount * (float x, float y, float z)
    ///   int32  fitJsonByteLength   (0 = no fit)
    ///   fitJson  (UTF-8)
    ///   int32  edgeJsonByteLength  (0 = no edge)
    ///   edgeJson (UTF-8)
    /// </summary>
    public static class LidarScanStore
    {
        private const int Magic   = 0x4C534341; // "LSCA"
        private const int Version = 1;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            IncludeFields = false
        };

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Writes the scan data for the given project to disk. Overwrites any
        /// existing file. A null/empty point cloud with a null fit is treated as
        /// "nothing to save" and any existing file is removed instead.
        /// </summary>
        public static void Save(int projectId,
                                List<(float x, float y, float z)> points,
                                LivoxLidarPlaneFitResult fit,
                                LivoxLidarDeckEdgeResult edge)
        {
            if (projectId <= 0)
                return;

            bool hasPoints = points != null && points.Count > 0;
            if (!hasPoints && fit == null)
            {
                Delete(projectId);
                return;
            }

            Directory.CreateDirectory(GetStorageDirectory());
            string path = GetScanFilePath(projectId);
            string tmp  = path + ".tmp";

            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var bw = new BinaryWriter(fs, Encoding.UTF8))
            {
                bw.Write(Magic);
                bw.Write(Version);

                int count = hasPoints ? points.Count : 0;
                bw.Write(count);
                for (int i = 0; i < count; i++)
                {
                    var p = points[i];
                    bw.Write(p.x);
                    bw.Write(p.y);
                    bw.Write(p.z);
                }

                WriteJsonBlock(bw, fit != null ? JsonSerializer.Serialize(FitDto.From(fit), JsonOptions) : null);
                WriteJsonBlock(bw, edge != null ? JsonSerializer.Serialize(EdgeDto.From(edge), JsonOptions) : null);
            }

            // Atomic-ish replace so a crash mid-write never corrupts a good file.
            if (File.Exists(path))
                File.Delete(path);
            File.Move(tmp, path);
        }

        /// <summary>
        /// Loads scan data for the given project. Returns false (with empty
        /// out-parameters) when no scan exists or the file cannot be read.
        /// </summary>
        public static bool Load(int projectId,
                                out List<(float x, float y, float z)> points,
                                out LivoxLidarPlaneFitResult fit,
                                out LivoxLidarDeckEdgeResult edge)
        {
            points = new List<(float, float, float)>();
            fit    = null;
            edge   = null;

            if (projectId <= 0)
                return false;

            string path = GetScanFilePath(projectId);
            if (!File.Exists(path))
                return false;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var br = new BinaryReader(fs, Encoding.UTF8))
            {
                if (br.ReadInt32() != Magic)
                    return false;

                int version = br.ReadInt32();
                if (version != Version)
                    return false;

                int count = br.ReadInt32();
                if (count < 0)
                    return false;

                points.Capacity = count;
                for (int i = 0; i < count; i++)
                {
                    float x = br.ReadSingle();
                    float y = br.ReadSingle();
                    float z = br.ReadSingle();
                    points.Add((x, y, z));
                }

                string fitJson = ReadJsonBlock(br);
                if (!string.IsNullOrEmpty(fitJson))
                {
                    var dto = JsonSerializer.Deserialize<FitDto>(fitJson, JsonOptions);
                    fit = dto?.ToResult();
                }

                string edgeJson = ReadJsonBlock(br);
                if (!string.IsNullOrEmpty(edgeJson))
                {
                    var dto = JsonSerializer.Deserialize<EdgeDto>(edgeJson, JsonOptions);
                    edge = dto?.ToResult();
                }
            }

            return true;
        }

        /// <summary>Removes the persisted scan file for the given project, if any.</summary>
        public static void Delete(int projectId)
        {
            if (projectId <= 0)
                return;

            string path = GetScanFilePath(projectId);
            if (File.Exists(path))
                File.Delete(path);
        }

        // ── Paths ────────────────────────────────────────────────────────────

        private static string GetStorageDirectory()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "MVS", "LidarScans");
        }

        private static string GetScanFilePath(int projectId)
            => Path.Combine(GetStorageDirectory(), $"scan_{projectId}.bin");

        // ── Block helpers ────────────────────────────────────────────────────

        private static void WriteJsonBlock(BinaryWriter bw, string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                bw.Write(0);
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            bw.Write(bytes.Length);
            bw.Write(bytes);
        }

        private static string ReadJsonBlock(BinaryReader br)
        {
            int length = br.ReadInt32();
            if (length <= 0)
                return null;

            byte[] bytes = br.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        // ── DTOs ─────────────────────────────────────────────────────────────
        // System.Text.Json cannot serialize List<ValueTuple<...>> members, so the
        // result objects are mapped to DTOs whose point collections are stored as
        // parallel primitive arrays.

        private sealed class FitDto
        {
            public bool   IsValid { get; set; }
            public double PitchDeg { get; set; }
            public double RollDeg { get; set; }
            public double FitRmse { get; set; }
            public double SurfaceRmse { get; set; }
            public double ClearanceMm { get; set; }
            public int    PointCount { get; set; }
            public double CentroidX { get; set; }
            public double CentroidY { get; set; }
            public double CentroidZ { get; set; }
            public double NormalX { get; set; }
            public double NormalY { get; set; }
            public double NormalZ { get; set; }
            public double VesselForwardX { get; set; }
            public double VesselForwardY { get; set; }
            public double VesselForwardZ { get; set; }
            public double ExtentPrimary { get; set; }
            public double ExtentSecondary { get; set; }
            public double FwdExtentMin { get; set; }
            public double FwdExtentMax { get; set; }
            public double LatExtentMin { get; set; }
            public double LatExtentMax { get; set; }
            public DeckSlantType DetectedSlantType { get; set; }
            public double DetectedSlantDeg { get; set; }

            public static FitDto From(LivoxLidarPlaneFitResult r) => new FitDto
            {
                IsValid = r.IsValid,
                PitchDeg = r.PitchDeg,
                RollDeg = r.RollDeg,
                FitRmse = r.FitRmse,
                SurfaceRmse = r.SurfaceRmse,
                ClearanceMm = r.ClearanceMm,
                PointCount = r.PointCount,
                CentroidX = r.CentroidX,
                CentroidY = r.CentroidY,
                CentroidZ = r.CentroidZ,
                NormalX = r.NormalX,
                NormalY = r.NormalY,
                NormalZ = r.NormalZ,
                VesselForwardX = r.VesselForwardX,
                VesselForwardY = r.VesselForwardY,
                VesselForwardZ = r.VesselForwardZ,
                ExtentPrimary = r.ExtentPrimary,
                ExtentSecondary = r.ExtentSecondary,
                FwdExtentMin = r.FwdExtentMin,
                FwdExtentMax = r.FwdExtentMax,
                LatExtentMin = r.LatExtentMin,
                LatExtentMax = r.LatExtentMax,
                DetectedSlantType = r.DetectedSlantType,
                DetectedSlantDeg = r.DetectedSlantDeg
            };

            public LivoxLidarPlaneFitResult ToResult() => new LivoxLidarPlaneFitResult
            {
                IsValid = IsValid,
                PitchDeg = PitchDeg,
                RollDeg = RollDeg,
                FitRmse = FitRmse,
                SurfaceRmse = SurfaceRmse,
                ClearanceMm = ClearanceMm,
                PointCount = PointCount,
                CentroidX = CentroidX,
                CentroidY = CentroidY,
                CentroidZ = CentroidZ,
                NormalX = NormalX,
                NormalY = NormalY,
                NormalZ = NormalZ,
                VesselForwardX = VesselForwardX,
                VesselForwardY = VesselForwardY,
                VesselForwardZ = VesselForwardZ,
                ExtentPrimary = ExtentPrimary,
                ExtentSecondary = ExtentSecondary,
                FwdExtentMin = FwdExtentMin,
                FwdExtentMax = FwdExtentMax,
                LatExtentMin = LatExtentMin,
                LatExtentMax = LatExtentMax,
                DetectedSlantType = DetectedSlantType,
                DetectedSlantDeg = DetectedSlantDeg
            };
        }

        private sealed class EdgeDto
        {
            public bool   IsValid { get; set; }
            public string DetectionMethod { get; set; }
            public double DirectionX { get; set; }
            public double DirectionY { get; set; }
            public double DirectionZ { get; set; }
            public double EdgeAngleDeg { get; set; }
            public double VesselForwardX { get; set; }
            public double VesselForwardY { get; set; }
            public double VesselForwardZ { get; set; }
            public double VesselForwardAngleDeg { get; set; }
            public double MidpointX { get; set; }
            public double MidpointY { get; set; }
            public double MidpointZ { get; set; }
            public double HalfLength { get; set; }
            public int    ForwardPointCount { get; set; }
            public int    DeckInlierCount { get; set; }
            public int    HullVertexCount { get; set; }
            public int    EdgePointCount { get; set; }
            public double FitRmseMm { get; set; }

            // Edge points stored as parallel arrays.
            public float[] EdgePointsX { get; set; }
            public float[] EdgePointsY { get; set; }
            public float[] EdgePointsZ { get; set; }

            // Hull vertices stored as parallel arrays.
            public double[] HullX { get; set; }
            public double[] HullY { get; set; }
            public double[] HullZ { get; set; }

            public static EdgeDto From(LivoxLidarDeckEdgeResult r)
            {
                var dto = new EdgeDto
                {
                    IsValid = r.IsValid,
                    DetectionMethod = r.DetectionMethod,
                    DirectionX = r.DirectionX,
                    DirectionY = r.DirectionY,
                    DirectionZ = r.DirectionZ,
                    EdgeAngleDeg = r.EdgeAngleDeg,
                    VesselForwardX = r.VesselForwardX,
                    VesselForwardY = r.VesselForwardY,
                    VesselForwardZ = r.VesselForwardZ,
                    VesselForwardAngleDeg = r.VesselForwardAngleDeg,
                    MidpointX = r.MidpointX,
                    MidpointY = r.MidpointY,
                    MidpointZ = r.MidpointZ,
                    HalfLength = r.HalfLength,
                    ForwardPointCount = r.ForwardPointCount,
                    DeckInlierCount = r.DeckInlierCount,
                    HullVertexCount = r.HullVertexCount,
                    EdgePointCount = r.EdgePointCount,
                    FitRmseMm = r.FitRmseMm
                };

                var edgePts = r.EdgePoints ?? new List<(float, float, float)>();
                dto.EdgePointsX = new float[edgePts.Count];
                dto.EdgePointsY = new float[edgePts.Count];
                dto.EdgePointsZ = new float[edgePts.Count];
                for (int i = 0; i < edgePts.Count; i++)
                {
                    dto.EdgePointsX[i] = edgePts[i].x;
                    dto.EdgePointsY[i] = edgePts[i].y;
                    dto.EdgePointsZ[i] = edgePts[i].z;
                }

                var hull = r.HullVertices3D ?? new List<(double, double, double)>();
                dto.HullX = new double[hull.Count];
                dto.HullY = new double[hull.Count];
                dto.HullZ = new double[hull.Count];
                for (int i = 0; i < hull.Count; i++)
                {
                    dto.HullX[i] = hull[i].X;
                    dto.HullY[i] = hull[i].Y;
                    dto.HullZ[i] = hull[i].Z;
                }

                return dto;
            }

            public LivoxLidarDeckEdgeResult ToResult()
            {
                var r = new LivoxLidarDeckEdgeResult
                {
                    IsValid = IsValid,
                    DetectionMethod = DetectionMethod ?? "—",
                    DirectionX = DirectionX,
                    DirectionY = DirectionY,
                    DirectionZ = DirectionZ,
                    EdgeAngleDeg = EdgeAngleDeg,
                    VesselForwardX = VesselForwardX,
                    VesselForwardY = VesselForwardY,
                    VesselForwardZ = VesselForwardZ,
                    VesselForwardAngleDeg = VesselForwardAngleDeg,
                    MidpointX = MidpointX,
                    MidpointY = MidpointY,
                    MidpointZ = MidpointZ,
                    HalfLength = HalfLength,
                    ForwardPointCount = ForwardPointCount,
                    DeckInlierCount = DeckInlierCount,
                    HullVertexCount = HullVertexCount,
                    EdgePointCount = EdgePointCount,
                    FitRmseMm = FitRmseMm
                };

                r.EdgePoints = new List<(float, float, float)>();
                if (EdgePointsX != null && EdgePointsY != null && EdgePointsZ != null)
                {
                    int n = Math.Min(EdgePointsX.Length, Math.Min(EdgePointsY.Length, EdgePointsZ.Length));
                    for (int i = 0; i < n; i++)
                        r.EdgePoints.Add((EdgePointsX[i], EdgePointsY[i], EdgePointsZ[i]));
                }

                r.HullVertices3D = new List<(double, double, double)>();
                if (HullX != null && HullY != null && HullZ != null)
                {
                    int n = Math.Min(HullX.Length, Math.Min(HullY.Length, HullZ.Length));
                    for (int i = 0; i < n; i++)
                        r.HullVertices3D.Add((HullX[i], HullY[i], HullZ[i]));
                }

                return r;
            }
        }
    }
}
