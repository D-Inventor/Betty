using NUnit.Framework;
using System.Text;

using Betty.Services;
using System.IO;
using System;
using Microsoft.Extensions.DependencyInjection;
using Betty.Utilities.DateTimeUtilities;

namespace Betty.NUnitTest.Services
{
    public class LoggerTest
    {
        [Test]
        public void Log_SeverityLowerThanMessage_WritesMessageToLog()
        {
            // arrange
            FakeStreamProvider streamProvider = new FakeStreamProvider();
            FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider
            {
                UtcNow = new DateTime(2019, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            };
            IServiceProvider services = new ServiceCollection().AddSingleton<IDateTimeProvider>(dateTimeProvider).BuildServiceProvider();
            Logger logger = new Logger(services)
            {
                StreamProvider = streamProvider,
                LogSeverity = LogSeverity.Info
            };

            // act
            logger.LogWarning("Test", "DebugMessage");
            logger.Dispose();

            // assert
            string output = streamProvider.StringBuilder.ToString();
            Assert.AreEqual($"[{dateTimeProvider.UtcNow}][{LogSeverity.Warning.ToString().PadLeft(7)}] {"Test".PadLeft(20)}:{"DebugMessage"}{Environment.NewLine}", output);
        }

        [Test]
        public void Log_SeverityEqualToMessage_WritesMessageToLog()
        {
            // arrange
            FakeStreamProvider streamProvider = new FakeStreamProvider();
            FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider
            {
                UtcNow = new DateTime(2019, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            };
            IServiceProvider services = new ServiceCollection().AddSingleton<IDateTimeProvider>(dateTimeProvider).BuildServiceProvider();
            Logger logger = new Logger(services)
            {
                StreamProvider = streamProvider,
                LogSeverity = LogSeverity.Warning
            };

            // act
            logger.LogWarning("Test", "DebugMessage");
            logger.Dispose();

            // assert
            string output = streamProvider.StringBuilder.ToString();
            Assert.AreEqual($"[{dateTimeProvider.UtcNow}][{LogSeverity.Warning.ToString().PadLeft(7)}] {"Test".PadLeft(20)}:{"DebugMessage"}{Environment.NewLine}", output);
        }

        [Test]
        public void Log_LogSeverityHigherThanMessage_IgnoresMessage()
        {
            // arrange
            FakeStreamProvider streamProvider = new FakeStreamProvider();
            Logger logger = new Logger
            {
                StreamProvider = streamProvider,
                LogSeverity = LogSeverity.Error
            };

            // act
            logger.LogWarning("Test", "DebugMessage");
            logger.Dispose();

            // assert
            string output = streamProvider.StringBuilder.ToString();
            Assert.AreEqual(string.Empty, output);
        }


        /// <summary>
        /// Test class that catches stream input into a string builder
        /// </summary>
        private class FakeStreamProvider : IStreamProvider
        {
            public FakeStreamProvider()
            {
                StringBuilder = new StringBuilder();
            }

            public StringBuilder StringBuilder { get; set; }

            public TextWriter GetStream()
            {
                return new StringWriter(StringBuilder);
            }
        }
    }
}
