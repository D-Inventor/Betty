﻿using Betty.Utilities.DateTimeUtilities;
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
    }
}
