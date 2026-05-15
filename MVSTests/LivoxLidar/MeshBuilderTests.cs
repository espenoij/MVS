using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.LivoxLidar
{
    [TestClass]
    public class MeshBuilderTests
    {
        // ── ArrowMeshBuilder ─────────────────────────────────────────────────

        [TestMethod]
        public void ArrowMeshBuilder_Build_ProducesNonEmptyGeometry()
        {
            var mesh = ArrowMeshBuilder.Build(
                new Point3D(0, 0, 0), new Vector3D(0, 0, 1), length: 100, shaftRadius: 5);

            Assert.IsNotNull(mesh);
            Assert.IsTrue(mesh.Positions.Count > 0);
            Assert.IsTrue(mesh.TriangleIndices.Count > 0);
            Assert.AreEqual(0, mesh.TriangleIndices.Count % 3, "indices must be triples");

            int max = -1;
            foreach (var i in mesh.TriangleIndices) if (i > max) max = i;
            Assert.IsTrue(max < mesh.Positions.Count, "all indices must be in range");
        }

        [TestMethod]
        public void ArrowMeshBuilder_Build_TipIsAtOriginPlusLengthAlongDir()
        {
            var origin = new Point3D(10, 20, 30);
            var dir = new Vector3D(1, 0, 0);
            double length = 50;

            var mesh = ArrowMeshBuilder.Build(origin, dir, length, 2);

            // The tip is the last vertex appended in Build().
            var tip = mesh.Positions[mesh.Positions.Count - 1];
            Assert.AreEqual(origin.X + length, tip.X, 1e-9);
            Assert.AreEqual(origin.Y, tip.Y, 1e-9);
            Assert.AreEqual(origin.Z, tip.Z, 1e-9);
        }

        // ── TubeMeshBuilder ──────────────────────────────────────────────────

        [TestMethod]
        public void TubeMeshBuilder_CoincidentEndpoints_ReturnsEmptyMesh()
        {
            var p = new Point3D(1, 2, 3);
            var mesh = TubeMeshBuilder.Build(p, p, 1.0);
            Assert.IsNotNull(mesh);
            Assert.AreEqual(0, mesh.Positions.Count);
            Assert.AreEqual(0, mesh.TriangleIndices.Count);
        }

        [TestMethod]
        public void TubeMeshBuilder_DistinctEndpoints_ProducesIndexedMesh()
        {
            var mesh = TubeMeshBuilder.Build(new Point3D(0, 0, 0), new Point3D(0, 0, 100), 1.0);

            Assert.IsTrue(mesh.Positions.Count >= 6);
            Assert.AreEqual(0, mesh.TriangleIndices.Count % 3);

            int max = -1;
            foreach (var i in mesh.TriangleIndices) if (i > max) max = i;
            Assert.IsTrue(max < mesh.Positions.Count);
        }

        // ── EdgePointsMeshBuilder ────────────────────────────────────────────

        [TestMethod]
        public void EdgePointsMeshBuilder_NullOrEmpty_ReturnsNull()
        {
            Assert.IsNull(EdgePointsMeshBuilder.Build(null));
            Assert.IsNull(EdgePointsMeshBuilder.Build(new List<(float, float, float)>()));
        }

        [TestMethod]
        public void EdgePointsMeshBuilder_OneQuadPerInputPoint_WhenNoDownsampling()
        {
            var pts = new List<(float x, float y, float z)> { (0, 0, 0), (1, 1, 1), (2, 2, 2) };
            var mesh = EdgePointsMeshBuilder.Build(pts, maxDisplay: 1000, quadHalfSize: 10f);

            Assert.AreEqual(pts.Count * 4, mesh.Positions.Count);
            Assert.AreEqual(pts.Count * 6, mesh.TriangleIndices.Count);
        }

        // ── HullMarkerMeshBuilder ────────────────────────────────────────────

        [TestMethod]
        public void HullMarkerMeshBuilder_NullOrEmpty_ReturnsNull()
        {
            Assert.IsNull(HullMarkerMeshBuilder.Build(null, 10));
            Assert.IsNull(HullMarkerMeshBuilder.Build(new List<(double, double, double)>(), 10));
        }

        [TestMethod]
        public void HullMarkerMeshBuilder_OneCubePerVertex()
        {
            var verts = new List<(double X, double Y, double Z)>
            {
                (0, 0, 0), (100, 0, 0), (0, 100, 0)
            };
            var mesh = HullMarkerMeshBuilder.Build(verts, halfSize: 5);

            // 8 verts and 12 triangles (36 indices) per cube.
            Assert.AreEqual(verts.Count * 8, mesh.Positions.Count);
            Assert.AreEqual(verts.Count * 36, mesh.TriangleIndices.Count);
        }

        // ── PlaneMeshBuilder ─────────────────────────────────────────────────

        [TestMethod]
        public void PlaneMeshBuilder_Build_ProducesQuadCenteredOnCentroid()
        {
            var fit = new LivoxLidarPlaneFitResult
            {
                IsValid = true,
                CentroidX = 0, CentroidY = 0, CentroidZ = 0,
                NormalX = 0, NormalY = 0, NormalZ = 1,
                VesselForwardX = 1, VesselForwardY = 0, VesselForwardZ = 0,
                ExtentPrimary = 1000, ExtentSecondary = 500,
            };

            var mesh = PlaneMeshBuilder.Build(fit, marginMm: 0);

            Assert.AreEqual(4, mesh.Positions.Count);

            // Centroid should be the average of the four corners.
            double avgX = 0, avgY = 0, avgZ = 0;
            foreach (var p in mesh.Positions) { avgX += p.X; avgY += p.Y; avgZ += p.Z; }
            avgX /= 4; avgY /= 4; avgZ /= 4;

            Assert.AreEqual(0, avgX, 1e-9);
            Assert.AreEqual(0, avgY, 1e-9);
            Assert.AreEqual(0, avgZ, 1e-9);

            // All four corners should lie on the plane z = 0.
            foreach (var p in mesh.Positions)
                Assert.AreEqual(0, p.Z, 1e-9);
        }

        [TestMethod]
        public void PlaneMeshBuilder_Build_AppliesMarginToExtents()
        {
            var fit = new LivoxLidarPlaneFitResult
            {
                IsValid = true,
                CentroidX = 0, CentroidY = 0, CentroidZ = 0,
                NormalX = 0, NormalY = 0, NormalZ = 1,
                VesselForwardX = 1, VesselForwardY = 0, VesselForwardZ = 0,
                ExtentPrimary = 100, ExtentSecondary = 50,
            };

            var mesh = PlaneMeshBuilder.Build(fit, marginMm: 200);

            // ep = 300, es = 250 → max |x| = 300, max |y| = 250
            double maxX = 0, maxY = 0;
            foreach (var p in mesh.Positions)
            {
                if (Math.Abs(p.X) > maxX) maxX = Math.Abs(p.X);
                if (Math.Abs(p.Y) > maxY) maxY = Math.Abs(p.Y);
            }
            Assert.AreEqual(300, maxX, 1e-9);
            Assert.AreEqual(250, maxY, 1e-9);
        }

        [TestMethod]
        public void PlaneMeshBuilder_Build_IsDoubleSided()
        {
            var fit = new LivoxLidarPlaneFitResult
            {
                IsValid = true,
                NormalX = 0, NormalY = 0, NormalZ = 1,
                VesselForwardX = 1, VesselForwardY = 0, VesselForwardZ = 0,
                ExtentPrimary = 10, ExtentSecondary = 10,
            };
            var mesh = PlaneMeshBuilder.Build(fit, marginMm: 0);

            // Two triangles per side × two sides = 12 indices.
            Assert.AreEqual(12, mesh.TriangleIndices.Count);
        }
    }
}
