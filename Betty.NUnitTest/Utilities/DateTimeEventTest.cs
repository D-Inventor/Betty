using Betty.Utilities.DateTimeUtilities;
using NUnit.Framework;
using System;

namespace Betty.NUnitTest.Utilities
{
    public class DateTimeEventTest
    {
        [Test]
        public void Start_InactiveEvent_IsActiveBecomesTrue()
        {
            // arrange
            DateTimeEvent dte = new DateTimeEvent();

            // act
            dte.Start(DateTime.UtcNow.AddDays(2));

            // assert
            Assert.IsTrue(dte.IsActive);
            dte.Stop();
        }

        [Test]
        public void Start_InactiveEvent_TargetBecomesGivenDatetime()
        {
            // arrange
            DateTimeEvent dte = new DateTimeEvent();
            DateTime input = DateTime.UtcNow.AddDays(2);

            // act
            dte.Start(input);

            // assert
            Assert.AreEqual(input, dte.Target);
            dte.Stop();
        }

        [Test]
        public void Start_ActiveEvent_ThrowsInvalidOperationException()
        {
            // arrange
            DateTimeEvent dte = new DateTimeEvent();
            dte.Start(DateTime.UtcNow.AddDays(2));

            // act
            void result() => dte.Start(DateTime.UtcNow.AddDays(1));

            // assert
            Assert.Throws<InvalidOperationException>(result);
            dte.Stop();
        }

        [Test]
        public void Stop_ActiveEvent_IsActiveBecomesFalse()
        {
            // arrange
            DateTimeEvent dte = new DateTimeEvent();
            dte.Start(DateTime.UtcNow.AddDays(2));

            // act
            dte.Stop();

            // assert
            Assert.IsFalse(dte.IsActive);
        }

        [Test]
        public void Stop_InactiveEvent_NothingChanges()
        {
            // arrange
            DateTimeEvent dte = new DateTimeEvent();

            // act
            dte.Stop();

            // assert
            Assert.IsFalse(dte.IsActive);
        }
    }
}
