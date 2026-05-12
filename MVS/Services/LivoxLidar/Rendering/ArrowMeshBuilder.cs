using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MVS
{
    /// <summary>
    /// Builds a 3-D arrow mesh (cylindrical shaft + cone head) along a given direction.
    /// Pure: no WPF UI dependencies beyond <see cref="MeshGeometry3D"/> data types.
    /// </summary>
    public static class ArrowMeshBuilder
    {
        private const int    SegmentCount   = 10;
        private const double ShaftLengthFraction = 0.75; // shaft = 75% of total length, head = 25%
        private const double HeadRadiusToShaftRatio = 2.5;

        /// <summary>
        /// Build an arrow mesh starting at <paramref name="origin"/>, pointing along
        /// <paramref name="dir"/>, with total length <paramref name="length"/> and
        /// shaft radius <paramref name="shaftRadius"/>.
        /// </summary>
        public static MeshGeometry3D Build(Point3D origin, Vector3D dir, double length, double shaftRadius)
        {
            dir.Normalize();

            // Perpendicular basis around the arrow axis
            Vector3D refVec = Math.Abs(dir.Z) < 0.9 ? new Vector3D(0, 0, 1) : new Vector3D(1, 0, 0);
            Vector3D perp1  = Vector3D.CrossProduct(dir, refVec);
            perp1.Normalize();
            Vector3D perp2 = Vector3D.CrossProduct(perp1, dir); // already unit-length

            double shaftLen   = length * ShaftLengthFraction;
            double headRadius = shaftRadius * HeadRadiusToShaftRatio;

            const int N         = SegmentCount;
            double    angleStep = 2.0 * Math.PI / N;

            var positions = new Point3DCollection();
            var indices   = new Int32Collection();

            Point3D shaftBase = origin;
            Point3D shaftTop  = origin + dir * shaftLen;
            Point3D headBase  = shaftTop;
            Point3D tip       = origin + dir * length;

            // Shaft cylinder — bottom ring (0..N-1), top ring (N..2N-1)
            for (int pass = 0; pass < 2; pass++)
            {
                Point3D centre = pass == 0 ? shaftBase : shaftTop;
                for (int i = 0; i < N; i++)
                {
                    double a = i * angleStep;
                    positions.Add(centre + shaftRadius * (Math.Cos(a) * perp1 + Math.Sin(a) * perp2));
                }
            }

            // Shaft side faces
            for (int i = 0; i < N; i++)
            {
                int next = (i + 1) % N;
                indices.Add(i);      indices.Add(i + N);    indices.Add(next + N);
                indices.Add(i);      indices.Add(next + N); indices.Add(next);
            }

            // Shaft bottom cap (fan from vertex 0)
            for (int i = 1; i < N - 1; i++)
            {
                indices.Add(0); indices.Add(i + 1); indices.Add(i);
            }

            // Cone head — base ring, then tip
            int headStart = positions.Count;
            for (int i = 0; i < N; i++)
            {
                double a = i * angleStep;
                positions.Add(headBase + headRadius * (Math.Cos(a) * perp1 + Math.Sin(a) * perp2));
            }
            int tipIdx = positions.Count;
            positions.Add(tip);

            // Cone side faces
            for (int i = 0; i < N; i++)
            {
                int next = (i + 1) % N;
                indices.Add(headStart + i); indices.Add(tipIdx); indices.Add(headStart + next);
            }

            // Cone base cap (fan from headStart)
            for (int i = 1; i < N - 1; i++)
            {
                indices.Add(headStart); indices.Add(headStart + i); indices.Add(headStart + i + 1);
            }

            return new MeshGeometry3D { Positions = positions, TriangleIndices = indices };
        }
    }
}
