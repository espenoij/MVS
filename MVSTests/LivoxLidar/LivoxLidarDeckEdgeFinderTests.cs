using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.LivoxLidar
{
    [TestClass]
    public class LivoxLidarDeckEdgeFinderTests
    {
        [TestMethod]
        public void FindEdge_NullInput_ReturnsInvalidResult()
        {
            var r = LivoxLidarDeckEdgeFinder.FindEdge(null);
            Assert.IsFalse(r.IsValid);
        }

        [TestMethod]
        public void FindEdge_EmptyInput_ReturnsInvalidResult()
        {
            var r = LivoxLidarDeckEdgeFinder.FindEdge(new List<(float, float, float)>());
            Assert.IsFalse(r.IsValid);
        }

        [TestMethod]
        public void FindEdge_TooFewPoints_ReturnsInvalidResult()
        {
            var pts = new List<(float, float, float)>();
            for (int i = 0; i < 5; i++) pts.Add((i, 0, 0));
            var r = LivoxLidarDeckEdgeFinder.FindEdge(pts);
            Assert.IsFalse(r.IsValid);
        }

        [TestMethod]
        public void FindEdge_AllNegativeX_NoForwardPoints_ReturnsInvalid()
        {
            // The pipeline crops to x > 0; with an all-stern cloud we expect zero forward points.
            var pts = new List<(float, float, float)>();
            for (int i = 0; i < 200; i++)
                pts.Add((-1000 - i, i, 0));

            var r = LivoxLidarDeckEdgeFinder.FindEdge(pts);
            Assert.IsFalse(r.IsValid);
            Assert.AreEqual(0, r.ForwardPointCount);
        }

        [TestMethod]
        public void FindEdge_HullVertices3D_AndHexVertices3D_AreSameUnderlyingList()
        {
            var r = new LivoxLidarDeckEdgeResult();
            r.HullVertices3D.Add((1, 2, 3));
            // Backward-compatibility shim should expose the same list.
            CollectionAssert.AreEqual(r.HullVertices3D, r.HexVertices3D);
        }
    }
}
