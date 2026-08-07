using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.Helpers
{
    [TestClass]
    public class EnumExtensionTests
    {
        private enum SampleEnum
        {
            [System.ComponentModel.Description("First value")]
            First = 1,

            [System.ComponentModel.Description("Second value")]
            Second = 2,

            // Deliberately no Description attribute.
            NoDescription = 3
        }

        // ── GetDescription ──────────────────────────────────────────────────

        [TestMethod]
        public void GetDescription_ValueWithAttribute_ReturnsDescription()
        {
            Assert.AreEqual("First value", SampleEnum.First.GetDescription());
            Assert.AreEqual("Second value", SampleEnum.Second.GetDescription());
        }

        [TestMethod]
        public void GetDescription_ValueWithoutAttribute_ReturnsNull()
        {
            Assert.IsNull(SampleEnum.NoDescription.GetDescription());
        }

        [TestMethod]
        public void GetDescription_UndefinedValue_ReturnsNull()
        {
            // A numeric value that is not a defined enum member has no name/field.
            Assert.IsNull(((SampleEnum)999).GetDescription());
        }

        // ── GetEnumValueFromDescription ─────────────────────────────────────

        [TestMethod]
        public void GetEnumValueFromDescription_KnownDescription_ReturnsMatchingValue()
        {
            Assert.AreEqual(SampleEnum.First,
                EnumExtension.GetEnumValueFromDescription<SampleEnum>("First value"));
            Assert.AreEqual(SampleEnum.Second,
                EnumExtension.GetEnumValueFromDescription<SampleEnum>("Second value"));
        }

        [TestMethod]
        public void GetEnumValueFromDescription_UnknownDescription_ReturnsDefault()
        {
            // No match -> default(T), which is 0 (undefined here).
            Assert.AreEqual(default(SampleEnum),
                EnumExtension.GetEnumValueFromDescription<SampleEnum>("Does not exist"));
        }

        [TestMethod]
        public void GetEnumValueFromDescription_NonEnumType_ThrowsArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() =>
                EnumExtension.GetEnumValueFromDescription<int>("anything"));
        }
    }
}
