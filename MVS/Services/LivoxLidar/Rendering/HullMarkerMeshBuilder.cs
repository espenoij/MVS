using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MVS
{
    /// <summary>
    /// Builds a single mesh containing one axis-aligned cube marker per
    /// supplied vertex. Used to mark estimated deck-boundary (convex hull)
    /// vertices in the 3-D scene.
    /// Pure: no WPF UI dependencies beyond <see cref="MeshGeometry3D"/> data types.
    /// </summary>
    public static class HullMarkerMeshBuilder
    {
        /// <summary>
        /// Build a mesh with one cube of half-size <paramref name="halfSize"/>
        /// centred on each vertex in <paramref name="vertices"/>.
        /// Returns null if there are no vertices.
        /// </summary>
        public static MeshGeometry3D Build(
            IList<(double X, double Y, double Z)> vertices,
            double halfSize)
        {
            if (vertices == null || vertices.Count == 0) return null;

            double h = halfSize;
            var positions = new Point3DCollection();
            var indices   = new Int32Collection();

            for (int v = 0; v < vertices.Count; v++)
            {
                var vert = vertices[v];
                int b = positions.Count;
                double vx = vert.X, vy = vert.Y, vz = vert.Z;

                positions.Add(new Point3D(vx - h, vy - h, vz - h)); // b+0
                positions.Add(new Point3D(vx + h, vy - h, vz - h)); // b+1
                positions.Add(new Point3D(vx + h, vy + h, vz - h)); // b+2
                positions.Add(new Point3D(vx - h, vy + h, vz - h)); // b+3
                positions.Add(new Point3D(vx - h, vy - h, vz + h)); // b+4
                positions.Add(new Point3D(vx + h, vy - h, vz + h)); // b+5
                positions.Add(new Point3D(vx + h, vy + h, vz + h)); // b+6
                positions.Add(new Point3D(vx - h, vy + h, vz + h)); // b+7

                // bottom (z-)
                indices.Add(b);   indices.Add(b + 1); indices.Add(b + 2);
                indices.Add(b);   indices.Add(b + 2); indices.Add(b + 3);
                // top (z+)
                indices.Add(b + 4); indices.Add(b + 6); indices.Add(b + 5);
                indices.Add(b + 4); indices.Add(b + 7); indices.Add(b + 6);
                // front (y-)
                indices.Add(b);     indices.Add(b + 5); indices.Add(b + 1);
                indices.Add(b);     indices.Add(b + 4); indices.Add(b + 5);
                // back (y+)
                indices.Add(b + 2); indices.Add(b + 7); indices.Add(b + 3);
                indices.Add(b + 2); indices.Add(b + 6); indices.Add(b + 7);
                // left (x-)
                indices.Add(b);     indices.Add(b + 3); indices.Add(b + 7);
                indices.Add(b);     indices.Add(b + 7); indices.Add(b + 4);
                // right (x+)
                indices.Add(b + 1); indices.Add(b + 6); indices.Add(b + 2);
                indices.Add(b + 1); indices.Add(b + 5); indices.Add(b + 6);
            }

            return new MeshGeometry3D { Positions = positions, TriangleIndices = indices };
        }
    }
}
