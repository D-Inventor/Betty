using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Betty.UnitTest
{
    [TestClass]
    public class LinqExtensionsTest
    {
        [TestMethod]
        public void Max_ValidInput_ReturnsMaximum()
        {
            int[] Input = { 3, 4, 2, 7, 4, 6, 33, 2 };

            int Result = Input.Max(x => x);

            int Expected = 33;
            Assert.AreEqual(Result, Expected);
        }

        [TestMethod]
        public void Max_InvalidInput_ThrowsArgumentException()
        {
            int[] Input = { };

            try
            {
                Input.Max(x => x);
            }
            catch (ArgumentException)
            {
                return;
            }

            Assert.Fail("Empty input should throw an exception");
        }

        [TestMethod]
        public void Min_ValidInput_ReturnsMaximum()
        {
            int[] Input = { 3, 4, 2, 7, 4, 6, 33, 2 };

            int Result = Input.Min(x => x);

            int Expected = 2;
            Assert.AreEqual(Result, Expected);
        }

        [TestMethod]
        public void Min_InvalidInput_ThrowsArgumentException()
        {
            int[] Input = { };

            try
            {
                Input.Min(x => x);
            }
            catch (ArgumentException)
            {
                return;
            }

            Assert.Fail("Empty input should throw an exception");
        }
    }
}
