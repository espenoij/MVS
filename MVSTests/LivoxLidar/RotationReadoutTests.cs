using System.Globalization;
using System.Threading;
using System.Windows.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.LivoxLidar
{
    [TestClass]
    public class RotationReadoutTests
    {
        [TestMethod]
        public void Update_NullTextBlocks_DoesNotThrow()
        {
            var readout = new RotationReadout(null, null, null);
            readout.Update(1.234, -5.678, 0);
        }

        [TestMethod]
        public void Update_WritesFormattedValuesIntoTextBlocks()
        {
            // WPF DependencyObjects require an STA thread; run the body on one.
            string xText = null, yText = null, zText = null;
            var thread = new Thread(() =>
            {
                var tx = new TextBlock();
                var ty = new TextBlock();
                var tz = new TextBlock();
                var readout = new RotationReadout(tx, ty, tz);

                readout.Update(12.34, -5.6, 0);

                xText = tx.Text;
                yText = ty.Text;
                zText = tz.Text;
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            // RotationReadout formats with the current culture, so build the
            // expected strings using the same culture's decimal separator.
            string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            Assert.AreEqual("+12" + sep + "3°", xText);
            Assert.AreEqual("-5" + sep + "6°", yText);
            Assert.AreEqual("0" + sep + "0°",  zText);
        }
    }
}
