using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.LivoxLidar
{
    [TestClass]
    public class PointCloudMeshBuilderTests
    {
        [TestMethod]
        public void Build_NullOrEmpty_ReturnsNull()
        {
            Assert.IsNull(PointCloudMeshBuilder.Build(null, 100));
            Assert.IsNull(PointCloudMeshBuilder.Build(new List<(float x, float y, float z)>(), 100));
        }

        [TestMethod]
        public void Build_ZeroMaxDisplay_ReturnsNull()
        {
            var pts = new List<(float x, float y, float z)> { (0, 0, 0) };
            Assert.IsNull(PointCloudMeshBuilder.Build(pts, 0));
        }

        [TestMethod]
        public void Build_SinglePoint_ProducesOneQuadAndTwoTriangles()
        {
            var pts = new List<(float x, float y, float z)> { (0, 0, 5) };
            var result = PointCloudMeshBuilder.Build(pts, maxDisplay: 1, quadHalfSize: 10f);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.DisplayedPointCount);
            Assert.AreEqual(4, result.Mesh.Positions.Count);
            Assert.AreEqual(6, result.Mesh.TriangleIndices.Count);
            Assert.AreEqual(4, result.Mesh.TextureCoordinates.Count);
        }

        [TestMethod]
        public void Build_DownsamplesWhenInputExceedsMax()
        {
            var pts = new List<(float x, float y, float z)>();
            for (int i = 0; i < 1000; i++) pts.Add((i, 0, 0));
            var result = PointCloudMeshBuilder.Build(pts, maxDisplay: 100);

            Assert.AreEqual(100, result.DisplayedPointCount);
            Assert.AreEqual(400, result.Mesh.Positions.Count);
        }

        [TestMethod]
        public void Build_ComputesZRangeAcrossDisplayedPoints()
        {
            var pts = new List<(float x, float y, float z)>
            {
                (0, 0, -3f),
                (0, 0,  0f),
                (0, 0,  7f),
            };
            var result = PointCloudMeshBuilder.Build(pts, maxDisplay: 3);
            Assert.AreEqual(-3f, result.ZMin);
            Assert.AreEqual( 7f, result.ZMax);
        }

        [TestMethod]
        public void Build_FlatScan_DoesNotDivideByZero()
        {
            var pts = new List<(float x, float y, float z)> { (0, 0, 4f), (1, 1, 4f) };
            var result = PointCloudMeshBuilder.Build(pts, maxDisplay: 2);
            Assert.IsNotNull(result);
            Assert.AreEqual(4f, result.ZMin);
            Assert.AreEqual(4f, result.ZMax);
        }

        [TestMethod]
        public void TryComputeFitBounds_EmptyOrNull_ReturnsFalse()
        {
            Assert.IsFalse(PointCloudMeshBuilder.TryComputeFitBounds(
                null, 10, out _, out _, out _, out _, out _));
            Assert.IsFalse(PointCloudMeshBuilder.TryComputeFitBounds(
                new List<(float, float, float)>(), 10, out _, out _, out _, out _, out _));
        }

        [TestMethod]
        public void TryComputeFitBounds_SymmetricPoints_CentroidIsZero()
        {
            var pts = new List<(float x, float y, float z)>
            {
                (-100, -50, 0), (100, -50, 0), (100, 50, 0), (-100, 50, 0)
            };
            bool ok = PointCloudMeshBuilder.TryComputeFitBounds(
                pts, 10, out float cx, out float cy, out float cz,
                out float hx, out float hy);

            Assert.IsTrue(ok);
            Assert.AreEqual(0f, cx, 1e-3f);
            Assert.AreEqual(0f, cy, 1e-3f);
            Assert.AreEqual(0f, cz, 1e-3f);
            Assert.AreEqual(100f, hx, 1e-3f);
            Assert.AreEqual( 50f, hy, 1e-3f);
        }
    }
}
