using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Betty.Services
{
    public class Logger : ILogger, IDisposable
    {
        private readonly ConcurrentQueue<string> messagequeue;
        private readonly ManualResetEventSlim messagesavailable;
        private readonly Task loggertask;

        public LogSeverity LogSeverity { get; set; }
        public IStreamProvider StreamProvider { get; set; }

        public Logger()
        {
            messagequeue = new ConcurrentQueue<string>();
            messagesavailable = new ManualResetEventSlim(false);
            loggertask = new Task(LoggerProcess, TaskCreationOptions.LongRunning);
            loggertask.Start();
        }

        /// <summary>
        /// Task that writes all messages in the queue to a log stream
        /// </summary>
        private void LoggerProcess()
        {
            // keep logging while this object is not being disposed
            while (!isDisposing)
            {
                // only wait if there are no messages in the queue
                if (messagequeue.Count == 0)
                    messagesavailable.Wait();

                // get the output stream from the StreamProvider or get the null stream if StreamProvider is not set
                using (TextWriter log = GetLogStream())
                {
                    while (messagequeue.TryDequeue(out string message))
                    {
                        // write all the messages from the queue to the stream
                        log.WriteLine(message);
                        Console.WriteLine(message);
                    }

                    // make sure that all data is written out of the buffer
                    log.Flush();
                }

                // reset the awaitable event
                messagesavailable.Reset();
            }
        }

        /// <summary>
        /// Uses the given StreamProvider to create a Stream for log writing
        /// </summary>
        /// <returns>stream for the log to be written to</returns>
        private TextWriter GetLogStream()
        {
            if (StreamProvider == null) return TextWriter.Null;
            return StreamProvider.GetStream();
        }

        /// <summary>
        /// Adds a message to the log with given severity
        /// </summary>
        /// <param name="severity">severity of the message</param>
        /// <param name="message">message to be written</param>
        protected virtual void Log(LogSeverity severity, string source, object message)
        {
            // throw an exception if the logger process aborted prematurely
            if (loggertask.IsFaulted) { throw loggertask.Exception; }

            if (severity >= LogSeverity)
            {
                // add the message to the queue if it is severe enough
                string messagestr = $"[{DateTime.UtcNow}][{severity.ToString().PadLeft(7)}] {source.PadLeft(20)}:{message.ToString()}";
                messagequeue.Enqueue(messagestr);
                messagesavailable.Set();
            }
        }

        #region ILogger Support
        /// <summary>
        /// Write a debug message to the log
        /// </summary>
        /// <param name="message">object to be logged</param>
        public void LogDebug(string source, object message)
        {
            Log(LogSeverity.Debug, source, message);
        }

        /// <summary>
        /// Write an info message to the log
        /// </summary>
        /// <param name="message">object to be logged</param>
        public void LogInfo(string source, object message)
        {
            Log(LogSeverity.Info, source, message);
        }

        /// <summary>
        /// Write a warning message to the log
        /// </summary>
        /// <param name="message">object to be logged</param>
        public void LogWarning(string source, object message)
        {
            Log(LogSeverity.Warning, source, message);
        }

        /// <summary>
        /// Write an error message to the log
        /// </summary>
        /// <param name="message">object to be logged</param>
        public void LogError(string source, object message)
        {
            Log(LogSeverity.Error, source, message);
        }
        #endregion

        #region IDisposable Support
        private bool isDisposing = false;

        public void Dispose()
        {
            if (!isDisposing)
            {
                // round off any logging and wait for the logger process to finish
                isDisposing = true;
                messagesavailable.Set();
                loggertask.Wait();
            }
        }
        #endregion
    }


    public enum LogSeverity
    {
        Debug, Info, Warning, Error
    }
}
