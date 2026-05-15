using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MVSTests.Services
{
    [TestClass]
    public class WeatherTests
    {
        [TestMethod]
        public void Encode_Decode_RoundTrip_PreservesAllFields()
        {
            int encoded = Weather.Encode((int)WeatherSeverity.Light, (int)WeatherPhenomena.RA, (int)WeatherPhenomena.SN);
            Assert.AreEqual(WeatherSeverity.Light,  Weather.DecodeSeverity(encoded));
            Assert.AreEqual(WeatherPhenomena.RA,    Weather.DecodePhenomena1(encoded));
            Assert.AreEqual(WeatherPhenomena.SN,    Weather.DecodePhenomena2(encoded));
        }

        [TestMethod]
        public void Encode_PacksFieldsIntoDistinctByteSlots()
        {
            int encoded = Weather.Encode(0xAB, 0xCD, 0xEF);
            Assert.AreEqual(0xAB, (int)Weather.DecodeSeverity(encoded));
            Assert.AreEqual(0xCD, (int)Weather.DecodePhenomena1(encoded));
            Assert.AreEqual(0xEF, (int)Weather.DecodePhenomena2(encoded));
        }

        [TestMethod]
        public void GetPhenomena_KnownCode_ReturnsMatchingEnum()
        {
            Assert.AreEqual(WeatherPhenomena.RA, Weather.GetPhenomena("RA"));
            Assert.AreEqual(WeatherPhenomena.FG, Weather.GetPhenomena("FG"));
            Assert.AreEqual(WeatherPhenomena.SN, Weather.GetPhenomena("SN"));
        }

        [TestMethod]
        public void GetPhenomena_UnknownCode_ReturnsNone()
        {
            Assert.AreEqual(WeatherPhenomena.None, Weather.GetPhenomena("XX"));
            Assert.AreEqual(WeatherPhenomena.None, Weather.GetPhenomena(""));
        }
    }
}
