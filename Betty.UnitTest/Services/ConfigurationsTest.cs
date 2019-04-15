using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Betty.Services;
using SimpleSettings;

namespace Betty.UnitTest.Services
{
    [TestClass]
    public class ConfigurationsTest
    {
        [TestMethod]
        public void LogDirectory_PathWasProvided_ReturnsFullPath()
        {
            // Arrange
            string input = "LogDirectory:logfiles/betty";
            Configurations configurations;
            using (StringReader sr = new StringReader(input))
                configurations = Settings.FromFile<Configurations>(sr);

            // Act
            string result = configurations.LogDirectory;

            // Assert
            string expected = Path.GetFullPath("logfiles/betty");
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void LogDirectory_NoPathWasProvided_ReturnsDefaultPath()
        {
            // Arrange
            string input = "LogDirectory:";
            Configurations configurations;
            using (StringReader sr = new StringReader(input))
                configurations = Settings.FromFile<Configurations>(sr);

            // Act
            string result = configurations.LogDirectory;

            // Assert
            string expected = Path.GetFullPath("log");
            Assert.AreEqual(expected, result);
        }
    }
}
