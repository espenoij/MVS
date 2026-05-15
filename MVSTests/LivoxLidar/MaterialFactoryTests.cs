using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.LivoxLidar
{
    [TestClass]
    public class MaterialFactoryTests
    {
        [TestMethod]
        public void HeatmapBrush_IsFrozenAndHasFiveStops()
        {
            var factory = new MaterialFactory(emissiveEnabled: false);
            var brush = factory.HeatmapBrush;

            Assert.IsNotNull(brush);
            Assert.IsTrue(brush.IsFrozen);
            Assert.AreEqual(5, brush.GradientStops.Count);
        }

        [TestMethod]
        public void HeatmapBrush_CachedAcrossCalls()
        {
            var factory = new MaterialFactory(emissiveEnabled: false);
            Assert.AreSame(factory.HeatmapBrush, factory.HeatmapBrush);
        }

        [TestMethod]
        public void HeatmapMaterial_CachedAcrossCalls()
        {
            var factory = new MaterialFactory(emissiveEnabled: false);
            Assert.AreSame(factory.HeatmapMaterial, factory.HeatmapMaterial);
        }

        [TestMethod]
        public void HeatmapMaterial_IsFrozen()
        {
            var factory = new MaterialFactory(emissiveEnabled: true);
            Assert.IsTrue(factory.HeatmapMaterial.IsFrozen);
        }

        [TestMethod]
        public void SolidBrush_CachedPerColor()
        {
            var factory = new MaterialFactory(emissiveEnabled: false);
            var red1 = factory.GetSolidBrush(Colors.Red);
            var red2 = factory.GetSolidBrush(Colors.Red);
            var blue = factory.GetSolidBrush(Colors.Blue);

            Assert.AreSame(red1, red2);
            Assert.AreNotSame(red1, blue);
            Assert.IsTrue(red1.IsFrozen);
            Assert.AreEqual(Colors.Red, red1.Color);
        }

        [TestMethod]
        public void SolidMaterial_CachedPerColor()
        {
            var factory = new MaterialFactory(emissiveEnabled: false);
            var m1 = factory.SolidMaterial(Colors.Lime);
            var m2 = factory.SolidMaterial(Colors.Lime);
            var m3 = factory.SolidMaterial(Colors.Yellow);

            Assert.AreSame(m1, m2);
            Assert.AreNotSame(m1, m3);
            Assert.IsTrue(m1.IsFrozen);
        }

        [TestMethod]
        public void EmissiveEnabled_FlagIsExposed()
        {
            Assert.IsTrue(new MaterialFactory(true).EmissiveEnabled);
            Assert.IsFalse(new MaterialFactory(false).EmissiveEnabled);
        }
    }
}
