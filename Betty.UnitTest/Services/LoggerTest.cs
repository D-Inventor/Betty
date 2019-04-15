using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

using Betty.Services;
using System.IO;

namespace Betty.UnitTest.Services
{
    [TestClass]
    public class LoggerTest
    {
        [TestMethod]
        public void Log_SeverityLowerThanMessage_WritesMessageToLog()
        {
            // arrange
            FakeStreamProvider streamProvider = new FakeStreamProvider();
            using (Logger logger = new Logger())
            {
                logger.StreamProvider = streamProvider;
                logger.LogSeverity = LogSeverity.Info;

                // act
                logger.LogWarning("Test", "DebugMessage");
            }

            // assert
            string output = streamProvider.StringBuilder.ToString();

            Assert.AreNotEqual(string.Empty, output);
        }

        [TestMethod]
        public void Log_SeverityEqualToMessage_WritesMessageToLog()
        {
            // arrange
            FakeStreamProvider streamProvider = new FakeStreamProvider();
            using (Logger logger = new Logger())
            {
                logger.StreamProvider = streamProvider;
                logger.LogSeverity = LogSeverity.Warning;

                // act
                logger.LogWarning("Test", "DebugMessage");
            }

            // assert
            string output = streamProvider.StringBuilder.ToString();
            Assert.AreNotEqual(string.Empty, output);
        }

        [TestMethod]
        public void Log_LogSeverityHigherThanMessage_IgnoresMessage()
        {
            // arrange
            FakeStreamProvider streamProvider = new FakeStreamProvider();
            using (Logger logger = new Logger())
            {
                logger.StreamProvider = streamProvider;
                logger.LogSeverity = LogSeverity.Error;

                // act
                logger.LogWarning("Test", "DebugMessage");
            }

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
