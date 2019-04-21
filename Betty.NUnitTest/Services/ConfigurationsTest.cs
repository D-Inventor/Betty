﻿using NUnit.Framework;
using System.IO;

using Betty.Services;
using SimpleSettings;

namespace Betty.NUnitTest.Services
{
    public class ConfigurationsTest
    {
        [Test]
        public void LogDirectory_PathWasProvided_ReturnsFullPath()
        {
            // Arrange
            string input = "LogDirectory:logfiles/betty";
            Configurations configurations = new Configurations();
            using (StringReader sr = new StringReader(input))
                Settings.FromFile(configurations, sr);

            // Act
            string result = configurations.LogDirectory;

            // Assert
            string expected = Path.GetFullPath("logfiles/betty");
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void LogDirectory_NoPathWasProvided_ReturnsDefaultPath()
        {
            // Arrange
            string input = "LogDirectory:";
            Configurations configurations = new Configurations();
            using (StringReader sr = new StringReader(input))
                Settings.FromFile(configurations, sr);

            // Act
            string result = configurations.LogDirectory;

            // Assert
            string expected = Path.GetFullPath("log");
            Assert.AreEqual(expected, result);
        }
    }
}
