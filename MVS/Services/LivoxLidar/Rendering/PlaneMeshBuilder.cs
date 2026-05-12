using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MVS
{
    /// <summary>
    /// Builds the rectangular mesh that visualises the fitted helideck plane.
    /// The rectangle is centred on the fit centroid, oriented by the fit's
    /// primary in-plane axis (vessel forward) and the implicit secondary axis
    /// (normal × forward), and sized to the fit extents plus a margin.
    /// Pure: no WPF UI dependencies beyond <see cref="MeshGeometry3D"/> data types.
    /// </summary>
    public static class PlaneMeshBuilder
    {
        public const double DefaultMarginMm = 200.0;

        /// <summary>
        /// Build the plane rectangle mesh for <paramref name="fit"/>.
        /// Returns a mesh with both front- and back-facing triangles so it is
        /// visible from either side without a back material.
        /// </summary>
        public static MeshGeometry3D Build(LivoxLidarPlaneFitResult fit, double marginMm = DefaultMarginMm)
        {
            // Build a rectangle in the fitted plane, centred on the centroid
            double cx = fit.CentroidX, cy = fit.CentroidY, cz = fit.CentroidZ;

            // Primary in-plane axis
            double ax = fit.VesselForwardX, ay = fit.VesselForwardY, az = fit.VesselForwardZ;

            // Secondary axis = normal × primary
            double nx = fit.NormalX, ny = fit.NormalY, nz = fit.NormalZ;
            double sx = ny * az - nz * ay;
            double sy = nz * ax - nx * az;
            double sz = nx * ay - ny * ax;
            double slen = Math.Sqrt(sx * sx + sy * sy + sz * sz);
            if (slen > 0) { sx /= slen; sy /= slen; sz /= slen; }

            double ep = fit.ExtentPrimary   + marginMm;
            double es = fit.ExtentSecondary + marginMm;

            var p0 = new Point3D(cx - ax * ep - sx * es, cy - ay * ep - sy * es, cz - az * ep - sz * es);
            var p1 = new Point3D(cx + ax * ep - sx * es, cy + ay * ep - sy * es, cz + az * ep - sz * es);
            var p2 = new Point3D(cx + ax * ep + sx * es, cy + ay * ep + sy * es, cz + az * ep + sz * es);
            var p3 = new Point3D(cx - ax * ep + sx * es, cy - ay * ep + sy * es, cz - az * ep + sz * es);

            return new MeshGeometry3D
            {
                Positions = new Point3DCollection { p0, p1, p2, p3 },
                // Both winding orders so the rectangle is visible from either side.
                TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 }
            };
        }
    }
}
