using Betty.Services;
using NUnit.Framework;
using SimpleSettings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Betty.IntegrationTest.Services
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
            Configurations configurations;
            using (StringReader sr = new StringReader($"LogDirectory:{tempdirectoryname}"))
                configurations = Settings.FromFile<Configurations>(sr);

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
            Configurations configurations;
            using (StringReader sr = new StringReader($"LogDirectory:{tempdirectoryname}"))
                configurations = Settings.FromFile<Configurations>(sr);

            // Act
            configurations.GetCurrentLogstate(out string result, out bool fileLimitExceeded);

            // Assert
            Assert.AreEqual(tempdirectoryname, Path.GetDirectoryName(result));
            Assert.AreEqual(".log", Path.GetExtension(result));
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

            Configurations configurations;
            using (StringReader sr = new StringReader($"LogDirectory:{tempdirectoryname}"))
                configurations = Settings.FromFile<Configurations>(sr);

            // Act
            configurations.GetCurrentLogstate(out string result, out bool fileLimitExceeded);

            // Assert
            Assert.AreNotEqual(filename, result);
            Assert.AreEqual(tempdirectoryname, Path.GetDirectoryName(result));
            Assert.AreEqual(".log", Path.GetExtension(result));
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
            Configurations configurations;
            using (StringReader sr = new StringReader($"LogDirectory:{tempdirectoryname}\nMaxLogFiles:1"))
                configurations = Settings.FromFile<Configurations>(sr);

            // Act
            configurations.GetCurrentLogstate(out string result, out bool fileLimitExceeded);

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
            Thread.Sleep(1000);
            File.Create(filename2).Close();
            Configurations configurations;
            using (StringReader sr = new StringReader($"LogDirectory:{tempdirectoryname}"))
                configurations = Settings.FromFile<Configurations>(sr);

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
            Configurations configurations;
            using (StringReader sr = new StringReader($"LogDirectory:{tempdirectoryname}"))
                configurations = Settings.FromFile<Configurations>(sr);

            // Act
            configurations.RemoveOldestLogfile();

            // Assert
            Assert.IsFalse(File.Exists(filename));
        }
    }
}
