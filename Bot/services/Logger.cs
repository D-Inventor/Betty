using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

using Discord;

using Microsoft.Extensions.DependencyInjection;

namespace Betty
{
	public class Logger : IDisposable
	{
		Constants constants;

		ConcurrentQueue<string> logQueue;
		ManualResetEventSlim loggingFlag;
		ManualResetEventSlim DisposeReady;
		bool DisposeFlag = false;

		public Logger(IServiceProvider services)
		{
			constants = services.GetService<Constants>();

			logQueue = new ConcurrentQueue<string>();
			loggingFlag = new ManualResetEventSlim(false);
			DisposeReady = new ManualResetEventSlim(false);
		}

		public void Init()
		{
			// start the logging process
			Task.Run((Action)LoggerProcess);
			return;
		}

		public void Log(LogMessage msg)
		{
			// put message in the queue and signal that there are new messages to be logged
			logQueue.Enqueue($"[{DateTime.UtcNow}] [{msg.Severity.ToString().PadRight(8)}] {msg.Source.PadLeft(10)}: {msg.Message}");
			loggingFlag.Set();
		}

		public void Dispose()
		{
			// signal the logger thread to quit
			DisposeFlag = true;
			loggingFlag.Set();

			// wait for logger thread to finish
			DisposeReady.Wait();
		}

		private void LoggerProcess()
		{
			// keep logging forever
			while (!DisposeFlag)
			{
				// wait until a signal for flushing has been given, but only if the queue is currently empty.
				if (logQueue.Count == 0)
					loggingFlag.Wait();

				// make sure that the log directory exists
				string logpath = constants.PathToLogs();
				if (!Directory.Exists(logpath))
				{
					Directory.CreateDirectory(logpath);
				}

                // find the most recent log file in this directory
                string[] logfiles = Directory.GetFiles(constants.PathToLogs());
                string path = logfiles.Length > 0 ? logfiles.Max(x => File.GetCreationTimeUtc(x)) : null;

				// open log file at given path if present and smaller than 20MB or create a new log file
				using (StreamWriter sw = new StreamWriter((path == null || new FileInfo(path).Length > constants.MaxLogSize) ? Path.Combine(logpath, $"{DateTime.UtcNow:yyyyMMdd_HHmmss}.log") : path, true))
				{
					// write all entries to the log file
					while (logQueue.TryDequeue(out string msg))
					{
						sw.WriteLine(msg);
						Console.WriteLine(msg);
					}
				}

				// make sure that there are not more than 2 logfiles in the folder
				string[] files = Directory.GetFiles(logpath);
				if (files.Length > constants.MaxLogs)
				{
					File.Delete(files.Min(x => File.GetCreationTimeUtc(x)));
				}

				// make sure that the logging flag is no longer set to prevent unnecessary work
				loggingFlag.Reset();
			}

			DisposeReady.Set();
		}
	}
}
