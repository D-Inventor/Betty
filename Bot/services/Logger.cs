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
	public class Logger
	{
		Constants constants;

		ConcurrentQueue<string> logQueue;
		ManualResetEventSlim loggingFlag;

		public Logger(IServiceProvider services)
		{
			constants = services.GetService<Constants>();

			logQueue = new ConcurrentQueue<string>();
			loggingFlag = new ManualResetEventSlim(false);
		}

		public void Start()
		{
			// start the logging process
			Task.Run((Action)LoggerProcess);
		}

		public void Log(LogMessage msg)
		{
			logQueue.Enqueue($"[{DateTime.UtcNow}] {msg.Source}: {msg.Message}");
			loggingFlag.Set();
		}

		private void LoggerProcess()
		{
			// keep logging forever
			while (true)
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
				string path = Directory.GetFiles(constants.PathToLogs()).Max(x => File.GetCreationTimeUtc(x));

				// open log file at given path if present and smaller than 20MB or create a new log file
				using (StreamWriter sw = new StreamWriter((path == null || new FileInfo(path).Length > 20 * 1024) ? Path.Combine(logpath, $"{DateTime.UtcNow:yyyyMMdd_HHmmss}.log") : path, true))
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
				if (files.Length > 2)
				{
					File.Delete(files.Min(x => File.GetCreationTimeUtc(x)));
				}

				// make sure that the logging flag is no longer set to prevent unnecessary work
				loggingFlag.Reset();
			}
		}
	}
}
