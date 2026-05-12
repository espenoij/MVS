using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MVS
{
    /// <summary>
    /// Builds a cylindrical tube mesh between two points (no arrowhead).
    /// Pure: no WPF UI dependencies beyond <see cref="MeshGeometry3D"/> data types.
    /// </summary>
    public static class TubeMeshBuilder
    {
        private const int SegmentCount = 10;

        /// <summary>
        /// Build a tube of radius <paramref name="radius"/> from <paramref name="from"/> to <paramref name="to"/>.
        /// Returns an empty mesh if the endpoints coincide.
        /// </summary>
        public static MeshGeometry3D Build(Point3D from, Point3D to, double radius)
        {
            var dir = to - from;
            if (dir.Length < 1e-10) return new MeshGeometry3D();
            dir.Normalize();

            Vector3D refVec = Math.Abs(dir.Z) < 0.9 ? new Vector3D(0, 0, 1) : new Vector3D(1, 0, 0);
            Vector3D perp1  = Vector3D.CrossProduct(dir, refVec); perp1.Normalize();
            Vector3D perp2  = Vector3D.CrossProduct(perp1, dir);

            const int N         = SegmentCount;
            double    angleStep = 2.0 * Math.PI / N;
            var positions = new Point3DCollection();
            var indices   = new Int32Collection();

            for (int pass = 0; pass < 2; pass++)
            {
                Point3D centre = pass == 0 ? from : to;
                for (int i = 0; i < N; i++)
                {
                    double a = i * angleStep;
                    positions.Add(centre + radius * (Math.Cos(a) * perp1 + Math.Sin(a) * perp2));
                }
            }

            for (int i = 0; i < N; i++)
            {
                int next = (i + 1) % N;
                indices.Add(i);       indices.Add(i + N);    indices.Add(next + N);
                indices.Add(i);       indices.Add(next + N); indices.Add(next);
            }

            return new MeshGeometry3D { Positions = positions, TriangleIndices = indices };
        }
    }
}
