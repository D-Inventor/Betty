using Microsoft.VisualStudio.TestTools.UnitTesting;

using Betty.utilities;
using System;

namespace Betty.UnitTest
{
    [TestClass]
    public class DateTimeMethodsTest
    {
        [TestMethod]
        public void StringToTime_ValidInput_ReturnsCorrectTime()
        {
            // Arrange
            string Input = "8:30pm";

            // Act
            TimeSpan? Result = DateTimeMethods.StringToTime(Input, true);

            // Assert
            TimeSpan Expected = new TimeSpan(20, 30, 0);
            Assert.IsNotNull(Result);
            Assert.AreEqual(Result.Value, Expected);
        }

        [TestMethod]
        public void StringToTime_InvalidInput_ReturnsNull()
        {
            // Arrange
            string Input = "6om";

            // Act
            TimeSpan? Result = DateTimeMethods.StringToTime(Input, true);

            // Assert
            Assert.IsNull(Result);
        }

        [TestMethod]
        public void StringToTime_TimeInAString_ReturnsCorrectTime()
        {
            // Arrange
            string Input = "Our event starts at $7am tomorrow.";

            // Act
            TimeSpan? Result = DateTimeMethods.StringToTime(Input);

            // Assert
            TimeSpan Expected = new TimeSpan(7, 0, 0);
            Assert.IsNotNull(Result);
            Assert.AreEqual(Result, Expected);
        }

        [TestMethod]
        public void StringToTime_NoTimeInAString_ReturnsNull()
        {
            // Arrange
            string Input = "Our event starts at $7sm today";

            // Act
            TimeSpan? Result = DateTimeMethods.StringToTime(Input);

            // Assert
            Assert.IsNull(Result);
        }

        [TestMethod]
        public void StringToDateTime_ValidInput_ReturnsCorrectDateTime()
        {
            // Arrange
            string Input = "23:07:1998 8pm";

            // Act
            DateTime? Result = DateTimeMethods.StringToDatetime(Input);

            // Assert
            DateTime Expected = new DateTime(1998, 7, 23, 20, 0, 0);
            Assert.IsNotNull(Result);
            Assert.AreEqual(Expected, Result);
        }

        [TestMethod]
        public void StringToDateTime_InvalidDate_ReturnsNull()
        {
            string Input = "30:02:2019 8pm";

            DateTime? Result = DateTimeMethods.StringToDatetime(Input);

            Assert.IsNull(Result);
        }

        [TestMethod]
        public void StringToDateTime_NotADateTime_ReturnsNull()
        {
            string Input = "7837:68 8ml";

            DateTime? Result = DateTimeMethods.StringToDatetime(Input);

            Assert.IsNull(Result);
        }

        [TestMethod]
        public void IsTimezone_Timezone_ReturnsTrue()
        {
            Assert.IsTrue(DateTimeMethods.IsTimezone("Central European Standard Time"));
        }

        [TestMethod]
        public void IsTimeZone_NoTimezone_ReturnsFalse()
        {
            Assert.IsFalse(DateTimeMethods.IsTimezone("Central Easter Island GMT Time"));
        }
    }
}
