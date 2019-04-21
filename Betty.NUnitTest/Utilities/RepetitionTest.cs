using System;
using System.Collections.Generic;
using System.Text;
using Betty.Utilities.DateTimeUtilities;
using NUnit.Framework;

namespace Betty.NUnitTest.Utilities
{
    public class RepetitionTest
    {
        [Test, Description("Tests if the constructor throws an argument exception when called with an invalid amount.")]
        public void Constructor_InvalidAmount_ThrowsArgumentException()
        {
            // arrange
            int amount = -2;
            RepetitionUnit unit = RepetitionUnit.Day;

            // act
            void Result() => new Repetition(unit, amount);

            // assert
            Assert.Throws<ArgumentException>(Result);
        }

        [Test, Description("Tests if the constructor doesn't throw an exception when the unit is once.")]
        public void Constructor_OnceButInvalidAmount_DoesNotThrowException()
        {
            // arrange
            int amount = -2;
            RepetitionUnit unit = RepetitionUnit.Once;

            // act
            void Result() => new Repetition(unit, amount);

            // assert
            Assert.DoesNotThrow(Result);
        }

        [Test, Description("Tests if 'Id' returns the correct identifier with once")]
        public void Id_Once_Returnso()
        {
            // arrange
            Repetition repetition = new Repetition(RepetitionUnit.Once, 1);

            // act
            string id = repetition.Id;

            // assert
            Assert.AreEqual("o", id);
        }

        [Test, Description("Tests if 'Id' returns the correct identifier with days")]
        public void Id_EveryTwoDays_Returnsd2()
        {
            // arrange
            Repetition repetition = new Repetition(RepetitionUnit.Day, 2);

            // act
            string id = repetition.Id;

            // assert
            Assert.AreEqual("d2", id);
        }

        [Test, Description("Tests if 'Id' returns the correct identifier with weeks")]
        public void Id_EveryTwoWeeks_Returnsw2()
        {
            // arrange
            Repetition repetition = new Repetition(RepetitionUnit.Week, 2);

            // act
            string id = repetition.Id;

            // assert
            Assert.AreEqual("w2", id);
        }

        [Test, Description("Tests if 'Id' returns the correct identifier with months")]
        public void Id_EveryTwoMonths_Returnsm2()
        {
            // arrange
            Repetition repetition = new Repetition(RepetitionUnit.Month, 2);

            // act
            string id = repetition.Id;

            // assert
            Assert.AreEqual("m2", id);
        }

        [Test, Description("Tests if 'Id' returns the correct identifier with years")]
        public void Id_EveryTwoYears_Returnsy2()
        {
            // arrange
            Repetition repetition = new Repetition(RepetitionUnit.Year, 2);

            // act
            string id = repetition.Id;

            // assert
            Assert.AreEqual("y2", id);
        }

        [Test, Description("Tests if the 'FromId' method returns a repetition Once from this input")]
        public void FromId_o_ReturnsRepetitionOnce()
        {
            // arrange
            string input = "o";

            // act
            Repetition result = Repetition.FromId(input);

            // assert
            Assert.AreEqual(RepetitionUnit.Once, result.Unit);
        }

        [Test, Description("Tests if the 'FromId' method returns a repetition every 2 days from this input")]
        public void FromId_d2_ReturnsRepetitionEveryTwoDays()
        {
            // arrange
            string input = "d2";

            // act
            Repetition result = Repetition.FromId(input);

            // assert
            Assert.AreEqual(RepetitionUnit.Day, result.Unit);
            Assert.AreEqual(2, result.Amount);
        }

        [Test, Description("Tests if the 'FromId' method returns a repetition every 2 weeks from this input")]
        public void FromId_w2_ReturnsRepetitionEveryTwoWeeks()
        {
            // arrange
            string input = "w2";

            // act
            Repetition result = Repetition.FromId(input);

            // assert
            Assert.AreEqual(RepetitionUnit.Week, result.Unit);
            Assert.AreEqual(2, result.Amount);
        }

        [Test, Description("Tests if the 'FromId' method returns a repetition every 2 months from this input")]
        public void FromId_m2_ReturnsRepetitionEveryTwoMonths()
        {
            // arrange
            string input = "m2";

            // act
            Repetition result = Repetition.FromId(input);

            // assert
            Assert.AreEqual(RepetitionUnit.Month, result.Unit);
            Assert.AreEqual(2, result.Amount);
        }

        [Test, Description("Tests if the 'FromId' method returns a repetition every 2 years from this input")]
        public void FromId_y2_ReturnsRepetitionEveryTwoYears()
        {
            // arrange
            string input = "y2";

            // act
            Repetition result = Repetition.FromId(input);

            // assert
            Assert.AreEqual(RepetitionUnit.Year, result.Unit);
            Assert.AreEqual(2, result.Amount);
        }

        [Test, Description("Tests if the 'FromId' method throws an Format exception when the wrong unit character is given in the input string.")]
        public void FromId_WrongUnitCharacter_ThrowsFormatException()
        {
            // arrange
            string input = "j2";

            // act
            void Result() => Repetition.FromId(input);

            // assert
            Assert.Throws<FormatException>(Result);
        }

        [Test, Description("Tests if the 'FromId' method throws an Format exception when after the unit character, no amount is given.")]
        public void FromId_NoAmountNumber_ThrowsFormatException()
        {
            // arrange
            string input = "we";

            // act
            void Result() => Repetition.FromId(input);

            // assert
            Assert.Throws<FormatException>(Result);
        }

        [Test, Description("Tests the default scenario for dayly repetition on 'GetNext'.")]
        public void GetNext_RepetitionEveryDay_ReturnsNextDay()
        {
            // arrange
            Repetition repetition = new Repetition(RepetitionUnit.Day, 1);
            DateTime input = new DateTime(2019, 1, 1, 12, 0, 0);
            TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

            // act
            DateTime result = repetition.GetNext(input, timezone);

            // assert
            DateTime expected = new DateTime(2019, 1, 2, 12, 0, 0);
            Assert.AreEqual(expected, result);
        }

        [Test, Description("If the method encounters an invalid day, it should skip it.")]
        public void GetNext_DayIsInvalid_ReturnsDayAfter()
        {
            // arrange
            Repetition repetition = new Repetition(RepetitionUnit.Day, 1);
            DateTime input = new DateTime(2022, 3, 26, 2, 30, 0);
            TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

            // act
            DateTime result = repetition.GetNext(input, timezone);

            // assert
            DateTime expected = new DateTime(2022, 3, 28, 2, 30, 0);
            Assert.AreEqual(expected, result);
        }

        [Test, Description("Tests the default scenario for weekly repetition on 'GetNext'.")]
        public void GetNext_RepetitionEveryWeek_ReturnsNextWeek()
        {
            // arrange
            Repetition repetition = new Repetition(RepetitionUnit.Week, 1);
            DateTime input = new DateTime(2019, 1, 1, 12, 0, 0);
            TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

            // act
            DateTime result = repetition.GetNext(input, timezone);

            // assert
            DateTime expected = new DateTime(2019, 1, 8, 12, 0, 0);
            Assert.AreEqual(expected, result);
        }

        [Test, Description("If the method encounters an invalid day, the week after should be given instead.")]
        public void GetNext_WeekIsInvalid_ReturnsWeekAfter()
        {
            // arrange
            Repetition repetition = new Repetition(RepetitionUnit.Week, 1);
            DateTime input = new DateTime(2022, 3, 20, 2, 30, 0);
            TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

            // act
            DateTime result = repetition.GetNext(input, timezone);

            // assert
            DateTime expected = new DateTime(2022, 4, 3, 2, 30, 0);
            Assert.AreEqual(expected, result);
        }

        [Test, Description("Tests the default scenario for monthly repetition on 'GetNext'.")]
        public void GetNext_RepetitionEveryMonth_ReturnsNextMonth()
        {
            // arrange
            Repetition repetition = new Repetition(RepetitionUnit.Month, 1);
            DateTime input = new DateTime(2019, 1, 1, 12, 0, 0);
            TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

            // act
            DateTime result = repetition.GetNext(input, timezone);

            // assert
            DateTime expected = new DateTime(2019, 2, 1, 12, 0, 0);
            Assert.AreEqual(expected, result);
        }

        [Test, Description("If the method encounters an invalid date, it should return the month after that instead.")]
        public void GetNext_MonthIsInvalid_ReturnsMonthAfter()
        {
            // arrange
            Repetition repetition = new Repetition(RepetitionUnit.Month, 1);
            DateTime input = new DateTime(2022, 2, 27, 2, 30, 0);
            TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

            // act
            DateTime result = repetition.GetNext(input, timezone);

            // assert
            DateTime expected = new DateTime(2022, 4, 27, 2, 30, 0);
            Assert.AreEqual(expected, result);
        }



        [Test, Description("If the next month does not have given day, then it should skip that month.")]
        public void GetNext_NextMonthDoesNotHaveDay_ReturnsMonthAfter()
        {
            // arrange
            Repetition repetition = new Repetition(RepetitionUnit.Month, 1);
            DateTime input = new DateTime(2019, 1, 30, 12, 0, 0);
            TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

            // act
            DateTime result = repetition.GetNext(input, timezone);

            // assert
            DateTime expected = new DateTime(2019, 3, 30, 12, 0, 0);
            Assert.AreEqual(expected, result);
        }

        [Test, Description("Tests the default scenario for yearly repetition on 'GetNext'.")]
        public void GetNext_RepetitionEveryYear_ReturnsNextYear()
        {
            // arrange
            Repetition repetition = new Repetition(RepetitionUnit.Year, 1);
            DateTime input = new DateTime(2019, 1, 1, 12, 0, 0);
            TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

            // act
            DateTime result = repetition.GetNext(input, timezone);

            // assert
            DateTime expected = new DateTime(2020, 1, 1, 12, 0, 0);
            Assert.AreEqual(expected, result);
        }

        [Test, Description("If the next year does not have this day, the year should be skipped.")]
        public void GetNext_NextYearDoesNotHaveDay_ReturnsYearAfterThatHasDay()
        {
            // arrange
            Repetition repetition = new Repetition(RepetitionUnit.Year, 1);
            DateTime input = new DateTime(2020, 2, 29, 12, 0, 0);
            TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

            // act
            DateTime result = repetition.GetNext(input, timezone);

            // assert
            DateTime expected = new DateTime(2024, 2, 29, 12, 0, 0);
            Assert.AreEqual(expected, result);
        }
    }
}
