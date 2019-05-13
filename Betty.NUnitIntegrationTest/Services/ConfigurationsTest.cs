using Betty.Services;
using Betty.Utilities.DateTimeUtilities;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SimpleSettings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Betty.NUnitIntegrationTest.Services
{
    public class ConfigurationsTest
    {
        private string tempdirectoryname;

        [SetUp]
        public void InitialiseTest()
        {
            tempdirectoryname = Path.Combine(Directory.GetCurrentDirectory(), "integrationtests");
            Directory.CreateDirectory(tempdirectoryname);
        }

        [TearDown]
        public void CleanupTest()
        {
            Directory.Delete(tempdirectoryname, true);
        }

        [Test]
        public void GetCurrentLogfile_ValidLogfilePresent_ReturnsLogfile()
        {
            // Arrange
            string filename = Path.Combine(tempdirectoryname, "mylog.log");
            File.Create(filename).Close();
            Configurations configurations = new Configurations();
            using (StringReader sr = new StringReader($"LogDirectory:{tempdirectoryname}"))
                Settings.FromFile(configurations, sr);

            // Act
            configurations.GetCurrentLogstate(out string result, out bool fileLimitExceeded);

            // Assert
            Assert.AreEqual(filename, result);
            Assert.IsFalse(fileLimitExceeded);
        }

        [Test]
        public void GetCurrentLogfile_NoLogfilesPresent_ReturnsNewName()
        {
            // Arrange
            FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider();
            IServiceProvider services = new ServiceCollection().AddSingleton<IDateTimeProvider>(dateTimeProvider).BuildServiceProvider();
            dateTimeProvider.UtcNow = new DateTime(2019, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            Configurations configurations = new Configurations(services);
            using (StringReader sr = new StringReader($"LogDirectory:{tempdirectoryname}"))
                Settings.FromFile(configurations, sr);

            // Act
            configurations.GetCurrentLogstate(out string result, out bool fileLimitExceeded);

            // Assert
            Assert.AreEqual(Path.Combine(tempdirectoryname, $"log_{dateTimeProvider.UtcNow:yyyyMMdd_HHmmss}.log"), result);
            Assert.IsFalse(fileLimitExceeded);
        }

        [Test]
        public void GetCurrentLogfile_LogIsFull_ReturnsNewName()
        {
            // Arrange
            string filename = Path.Combine(tempdirectoryname, "mylog.log");
            using(StreamWriter sw = new StreamWriter(filename))
            {
                sw.Write(new string('.', 250 * 1024));
            }

            FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider
            {
                UtcNow = new DateTime(2019, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            };
            IServiceProvider services = new ServiceCollection().AddSingleton<IDateTimeProvider>(dateTimeProvider).BuildServiceProvider();

            Configurations configurations = new Configurations(services);
            using (StringReader sr = new StringReader($"LogDirectory:{tempdirectoryname}"))
                Settings.FromFile(configurations, sr);

            // Act
            configurations.GetCurrentLogstate(out string result, out bool fileLimitExceeded);

            // Assert
            Assert.AreEqual(Path.Combine(tempdirectoryname, $"log_{dateTimeProvider.UtcNow:yyyyMMdd_HHmmss}.log"), result);
            Assert.IsFalse(fileLimitExceeded);
        }

        [Test]
        public void GetCurrentLogfile_TooManyLogfiles_fileLimitExceededIsTrue()
        {
            // Arrange
            string filename = Path.Combine(tempdirectoryname, "mylog.log");
            string filename2 = Path.Combine(tempdirectoryname, "mylog2.log");
            File.Create(filename).Close();
            File.Create(filename2).Close();
            Configurations configurations = new Configurations();
            using (StringReader sr = new StringReader($"LogDirectory:{tempdirectoryname}\nMaxLogFiles:1"))
                Settings.FromFile(configurations, sr);

            // Act
            configurations.GetCurrentLogstate(out _, out bool fileLimitExceeded);

            // Assert
            Assert.IsTrue(fileLimitExceeded);
        }

        [Test]
        public void GetCurrentLogfile_TwoLogFiles_ReturnsLatestFile()
        {
            // Arrange
            string filename = Path.Combine(tempdirectoryname, "mylog.log");
            string filename2 = Path.Combine(tempdirectoryname, "Amylog2.log");
            File.Create(filename).Close();
            File.Create(filename2).Close();
            File.SetCreationTimeUtc(filename, DateTime.UtcNow - new TimeSpan(0, 2, 0));
            File.SetCreationTimeUtc(filename2, DateTime.UtcNow - new TimeSpan(0, 1, 0));

            Configurations configurations = new Configurations();
            using (StringReader sr = new StringReader($"LogDirectory:{tempdirectoryname}"))
                Settings.FromFile(configurations, sr);

            // Act
            configurations.GetCurrentLogstate(out string result, out _);

            // Assert
            Assert.AreEqual(filename2, result);
        }

        [Test]
        public void RemoveOldestLogfile_Twologfiles_DeletesOldest()
        {
            // Arrange
            string filename = Path.Combine(tempdirectoryname, "mylog.log");
            string filename2 = Path.Combine(tempdirectoryname, "Amylog2.log");
            File.Create(filename).Close();
            File.Create(filename2).Close();
            Configurations configurations = new Configurations();
            using (StringReader sr = new StringReader($"LogDirectory:{tempdirectoryname}"))
                Settings.FromFile(configurations, sr);

            // Act
            configurations.RemoveOldestLogfile();

            // Assert
            Assert.IsFalse(File.Exists(filename));
        }
    }
}
