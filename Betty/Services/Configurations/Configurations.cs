using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SimpleSettings;

namespace Betty.Services
{
    public class Configurations
    {
        private string logDirectory;

        [Group("Discord settings"), Default("YOUR SECRET TOKEN"), Description("The secret token for the bot to connect to discord")]
        public string Token { get; set; }

        [Group("Log settings"), Default(LogSeverity.Info), Description("Minimum severity for the message to be written to the log.\nPossible values: Debug | Info | Warning | Error")]
        public LogSeverity LogSeverity { get; set; }

        [Group("Log settings"), Default("log"), Description("Directory to which the log files get written")]
        public string LogDirectory
        {
            get { return logDirectory; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "log");
                else
                    logDirectory = Path.GetFullPath(value);
            }
        }

        [Group("Log settings"), Default(200 * 1024), Description("Maximum size of a log file in bytes")]
        public int MaxLogSize { get; set; }

        [Group("Log settings"), Default(2), Description("Amount of log files that may exist before old files get deleted")]
        public int MaxLogFiles { get; set; }

        public TextWriter LogfileProvider()
        {
            // make sure that the directory exists
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);

            GetCurrentLogstate(out string file, out bool fileLimitExceeded);

            if (fileLimitExceeded)
                RemoveOldestLogfile();

            // create and return a new filestream for this log file
            return new StreamWriter(file, true);
        }

        public void RemoveOldestLogfile()
        {
            IEnumerable<string> logfiles = Directory.GetFiles(LogDirectory).Where(x => Path.HasExtension(".log"));
            if (logfiles.Any())
            {
                string path = logfiles.Aggregate((x, y) => File.GetCreationTimeUtc(x) < File.GetCreationTimeUtc(y) ? x : y);
                File.Delete(path);
            }
        }

        public void GetCurrentLogstate(out string currentLogFile, out bool fileLimitExceeded)
        {
            // find the most recent logfile
            IEnumerable<string> logfiles = Directory.GetFiles(LogDirectory).Where(x => Path.HasExtension(".log"));
            string path = null;
            if(logfiles.Any())
            {
                path = logfiles.Aggregate((x, y) => File.GetCreationTimeUtc(x) > File.GetCreationTimeUtc(y) ? x : y);
                if (new FileInfo(path).Length > MaxLogSize)
                    path = null;
            }

            currentLogFile = path ?? Path.Combine(LogDirectory, $"log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.log");
            fileLimitExceeded = logfiles.Count() > MaxLogFiles;
        }
    }
}
