using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.Services
{
    [TestClass]
    public class ModbusHelperTests
    {
        private readonly ModbusHelper _helper = new ModbusHelper();

        [TestMethod]
        public void ValidAddressSpace_BoundariesOfEachRegion_AreValid()
        {
            Assert.IsTrue(_helper.ValidAddressSpace(1));      // coil min
            Assert.IsTrue(_helper.ValidAddressSpace(9999));   // coil max
            Assert.IsTrue(_helper.ValidAddressSpace(10001));  // discrete input min
            Assert.IsTrue(_helper.ValidAddressSpace(19999));  // discrete input max
            Assert.IsTrue(_helper.ValidAddressSpace(30001));  // input register min
            Assert.IsTrue(_helper.ValidAddressSpace(39999));  // input register max
            Assert.IsTrue(_helper.ValidAddressSpace(40001));  // holding register min
            Assert.IsTrue(_helper.ValidAddressSpace(49999));  // holding register max
        }

        [TestMethod]
        public void ValidAddressSpace_GapsBetweenRegions_AreInvalid()
        {
            Assert.IsFalse(_helper.ValidAddressSpace(0));
            Assert.IsFalse(_helper.ValidAddressSpace(10000));
            Assert.IsFalse(_helper.ValidAddressSpace(20000));
            Assert.IsFalse(_helper.ValidAddressSpace(40000));
            Assert.IsFalse(_helper.ValidAddressSpace(50000));
        }

        [TestMethod]
        public void ValidAddressSpaceCoil_OnlyAcceptsCoilRange()
        {
            Assert.IsTrue(_helper.ValidAddressSpaceCoil(1));
            Assert.IsTrue(_helper.ValidAddressSpaceCoil(9999));
            Assert.IsFalse(_helper.ValidAddressSpaceCoil(10001));
            Assert.IsFalse(_helper.ValidAddressSpaceCoil(0));
        }

        [TestMethod]
        public void AddressToOffset_RebasesAddressToZero_ForEachRegion()
        {
            Assert.AreEqual((ushort)0, _helper.AddressToOffset(1));
            Assert.AreEqual((ushort)0, _helper.AddressToOffset(10001));
            Assert.AreEqual((ushort)0, _helper.AddressToOffset(30001));
            Assert.AreEqual((ushort)0, _helper.AddressToOffset(40001));
            Assert.AreEqual((ushort)9, _helper.AddressToOffset(40010));
        }

        [TestMethod]
        public void AddressToOffset_OutOfRange_ReturnsZero()
        {
            Assert.AreEqual((ushort)0, _helper.AddressToOffset(99999));
        }

        [TestMethod]
        public void AddressToObjectType_ReturnsExpectedTypePerRegion()
        {
            Assert.AreEqual(ModbusObjectType.Coil,            _helper.AddressToObjectType(5));
            Assert.AreEqual(ModbusObjectType.DiscreteInput,   _helper.AddressToObjectType(15000));
            Assert.AreEqual(ModbusObjectType.InputRegister,   _helper.AddressToObjectType(35000));
            Assert.AreEqual(ModbusObjectType.HoldingRegister, _helper.AddressToObjectType(45000));
            Assert.AreEqual(ModbusObjectType.None,            _helper.AddressToObjectType(99999));
        }
    }
}
