using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MVS
{
    /// <summary>
    /// Result of building a point-cloud mesh: the geometry itself plus the
    /// Z-range used for heat-map normalisation.
    /// </summary>
    public class PointCloudMeshResult
    {
        public MeshGeometry3D Mesh { get; set; }
        public float ZMin { get; set; }
        public float ZMax { get; set; }
        public int DisplayedPointCount { get; set; }
    }

    /// <summary>
    /// Builds a quad-per-point mesh suitable for visualising a 3-D point cloud.
    /// Each point becomes a small square in the XY plane, textured by a
    /// height-based U coordinate (0 = lowest Z, 1 = highest Z) so the caller
    /// can apply a heat-map gradient brush.
    /// Pure: no WPF UI dependencies beyond <see cref="MeshGeometry3D"/> data types.
    /// </summary>
    public static class PointCloudMeshBuilder
    {
        public const float DefaultQuadHalfSize = 30f; // mm

        /// <summary>
        /// Build a downsampled quad mesh from <paramref name="points"/>.
        /// At most <paramref name="maxDisplay"/> points are rendered.
        /// </summary>
        public static PointCloudMeshResult Build(
            IList<(float x, float y, float z)> points,
            int maxDisplay,
            float quadHalfSize = DefaultQuadHalfSize)
        {
            if (points == null || points.Count == 0 || maxDisplay <= 0)
                return null;

            int displayCount = Math.Min(points.Count, maxDisplay);
            double stride = points.Count / (double)displayCount;

            // Z range across displayed points for heat-map normalisation
            float zMin = float.MaxValue, zMax = float.MinValue;
            for (int s = 0; s < displayCount; s++)
            {
                float z = points[(int)(s * stride)].z;
                if (z < zMin) zMin = z;
                if (z > zMax) zMax = z;
            }
            float zRange = zMax - zMin;
            if (zRange < 1f) zRange = 1f; // guard against flat scans

            var positions = new Point3DCollection();
            var indices   = new Int32Collection();
            var texCoords = new PointCollection();

            int idx = 0;
            for (int s = 0; s < displayCount; s++)
            {
                var (x, y, z) = points[(int)(s * stride)];

                double u = (z - zMin) / zRange;
                var uv = new Point(u, 0.5);

                positions.Add(new Point3D(x - quadHalfSize, y - quadHalfSize, z));
                positions.Add(new Point3D(x + quadHalfSize, y - quadHalfSize, z));
                positions.Add(new Point3D(x + quadHalfSize, y + quadHalfSize, z));
                positions.Add(new Point3D(x - quadHalfSize, y + quadHalfSize, z));

                texCoords.Add(uv); texCoords.Add(uv);
                texCoords.Add(uv); texCoords.Add(uv);

                indices.Add(idx);     indices.Add(idx + 1); indices.Add(idx + 2);
                indices.Add(idx);     indices.Add(idx + 2); indices.Add(idx + 3);
                idx += 4;
            }

            return new PointCloudMeshResult
            {
                Mesh = new MeshGeometry3D
                {
                    Positions          = positions,
                    TextureCoordinates = texCoords,
                    TriangleIndices    = indices
                },
                ZMin = zMin,
                ZMax = zMax,
                DisplayedPointCount = displayCount
            };
        }

        /// <summary>
        /// Compute the centroid and half-extents (in X and Y) of the displayed
        /// subset of <paramref name="points"/>. Useful for auto-fitting the camera.
        /// Returns false if there are no points to consider.
        /// </summary>
        public static bool TryComputeFitBounds(
            IList<(float x, float y, float z)> points,
            int maxDisplay,
            out float centroidX, out float centroidY, out float centroidZ,
            out float halfExtentX, out float halfExtentY)
        {
            centroidX = centroidY = centroidZ = 0f;
            halfExtentX = halfExtentY = 0f;

            if (points == null || points.Count == 0 || maxDisplay <= 0) return false;

            int displayCount = Math.Min(points.Count, maxDisplay);
            double stride = points.Count / (double)displayCount;

            float cx = 0, cy = 0, cz = 0;
            for (int j = 0; j < displayCount; j++)
            {
                var p = points[(int)(j * stride)];
                cx += p.x; cy += p.y; cz += p.z;
            }
            cx /= displayCount; cy /= displayCount; cz /= displayCount;

            float maxHalfX = 0, maxHalfY = 0;
            for (int j = 0; j < displayCount; j++)
            {
                var p = points[(int)(j * stride)];
                float dx = Math.Abs(p.x - cx);
                float dy = Math.Abs(p.y - cy);
                if (dx > maxHalfX) maxHalfX = dx;
                if (dy > maxHalfY) maxHalfY = dy;
            }

            centroidX = cx; centroidY = cy; centroidZ = cz;
            halfExtentX = maxHalfX; halfExtentY = maxHalfY;
            return true;
        }
    }
}
