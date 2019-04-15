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
            StringBuilder sb = new StringBuilder();
            Func<TextWriter> streamProvider = () => new StringWriter(sb);
            using (Logger logger = new Logger())
            {
                logger.StreamProvider = streamProvider;
                logger.LogSeverity = LogSeverity.Info;

                // act
                logger.LogWarning("Test", "DebugMessage");
            }

            // assert
            string output = sb.ToString();

            Assert.AreNotEqual(string.Empty, output);
        }

        [TestMethod]
        public void Log_SeverityEqualToMessage_WritesMessageToLog()
        {
            // arrange
            StringBuilder sb = new StringBuilder();
            Func<TextWriter> streamProvider = () => new StringWriter(sb);
            using (Logger logger = new Logger())
            {
                logger.StreamProvider = streamProvider;
                logger.LogSeverity = LogSeverity.Warning;

                // act
                logger.LogWarning("Test", "DebugMessage");
            }

            // assert
            string output = sb.ToString();
            Assert.AreNotEqual(string.Empty, output);
        }

        [TestMethod]
        public void Log_LogSeverityHigherThanMessage_IgnoresMessage()
        {
            // arrange
            StringBuilder sb = new StringBuilder();
            Func<TextWriter> streamProvider = () => new StringWriter(sb);
            using (Logger logger = new Logger())
            {
                logger.StreamProvider = streamProvider;
                logger.LogSeverity = LogSeverity.Error;

                // act
                logger.LogWarning("Test", "DebugMessage");
            }

            // assert
            string output = sb.ToString();

            Assert.AreEqual(string.Empty, output);
        }
    }
}
