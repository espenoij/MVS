using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MVS
{
    /// <summary>
    /// Builds a quad-per-point mesh used to highlight detected deck-edge points.
    /// Pure: no WPF UI dependencies beyond <see cref="MeshGeometry3D"/> data types.
    /// </summary>
    public static class EdgePointsMeshBuilder
    {
        public const int   DefaultMaxDisplay  = 5000;
        public const float DefaultQuadHalfSize = 50f; // mm

        /// <summary>
        /// Build a downsampled quad mesh from <paramref name="points"/>.
        /// Returns null if the input is empty.
        /// </summary>
        public static MeshGeometry3D Build(
            IList<(float x, float y, float z)> points,
            int maxDisplay = DefaultMaxDisplay,
            float quadHalfSize = DefaultQuadHalfSize)
        {
            if (points == null || points.Count == 0) return null;

            int step = Math.Max(1, points.Count / Math.Max(maxDisplay, 1));

            var positions = new Point3DCollection();
            var indices   = new Int32Collection();

            int idx = 0;
            for (int i = 0; i < points.Count; i += step)
            {
                var (x, y, z) = points[i];
                positions.Add(new Point3D(x - quadHalfSize, y - quadHalfSize, z));
                positions.Add(new Point3D(x + quadHalfSize, y - quadHalfSize, z));
                positions.Add(new Point3D(x + quadHalfSize, y + quadHalfSize, z));
                positions.Add(new Point3D(x - quadHalfSize, y + quadHalfSize, z));

                indices.Add(idx);     indices.Add(idx + 1); indices.Add(idx + 2);
                indices.Add(idx);     indices.Add(idx + 2); indices.Add(idx + 3);
                idx += 4;
            }

            return new MeshGeometry3D { Positions = positions, TriangleIndices = indices };
        }
    }
}
