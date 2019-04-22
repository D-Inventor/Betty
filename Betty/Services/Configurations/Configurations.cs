using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Betty.Utilities.DateTimeUtilities;
using Microsoft.Extensions.DependencyInjection;
using SimpleSettings;

namespace Betty.Services
{
    public class Configurations : IStreamProvider
    {
        #region Configurations
        private string logDirectory;

        [Group("Discord settings"), Default("YOUR SECRET TOKEN"), Description("The secret token for the bot to connect to discord")]
        public string Token { get; set; }

        [Group("Discord settings"), Description("The user id of the owner of this bot")]
        public ulong OwnerId { get; set; }

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
        #endregion

        public IServiceProvider Services { get; set; }

        public Configurations(IServiceProvider services = null)
        {
            Services = services;
        }

        public TextWriter GetStream()
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
            // find all the logfiles
            IEnumerable<string> logfiles = Directory.GetFiles(LogDirectory).Where(x => Path.GetExtension(x) == ".log");
            string path = null;
            if(logfiles.Any())
            {
                // if there are any log files, find the one that is most recent
                path = logfiles.Aggregate((x, y) => File.GetCreationTimeUtc(x) > File.GetCreationTimeUtc(y) ? x : y);
                if (new FileInfo(path).Length > MaxLogSize)

                    // only remember the file if it hasn't exceeded the maximum log size
                    path = null;
            }

            // set the out variables to the desired values
            IDateTimeProvider dateTimeProvider = Services?.GetService<IDateTimeProvider>() ?? new DateTimeProvider();
            currentLogFile = path ?? Path.Combine(LogDirectory, $"log_{dateTimeProvider.UtcNow:yyyyMMdd_HHmmss}.log");
            fileLimitExceeded = logfiles.Count() > MaxLogFiles;
        }
    }
}
