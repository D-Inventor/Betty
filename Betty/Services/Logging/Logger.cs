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
        private ConcurrentQueue<string> messagequeue;
        private ManualResetEventSlim messagesavailable;
        private Task loggertask;
        private bool isdisposing = false;

        public LogSeverity LogSeverity { get; set; }
        public Func<TextWriter> StreamProvider { get; set; }

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
            while (!isdisposing)
            {
                // only wait if there are no messages in the queue
                if(messagequeue.Count == 0)
                    messagesavailable.Wait();

                // get the output stream from the StreamProvider or get the null stream if StreamProvider is not set
                using(TextWriter log = (StreamProvider ?? (() => TextWriter.Null)).Invoke())
                {
                    while(messagequeue.TryDequeue(out string message))
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
        /// Adds a message to the log with given severity
        /// </summary>
        /// <param name="severity">severity of the message</param>
        /// <param name="message">message to be written</param>
        protected virtual void Log(LogSeverity severity, string source, object message)
        {
            if (loggertask.IsFaulted)
                // throw an exception if the logger process aborted prematurely
                throw loggertask.Exception;

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
        /// Write an error message to the log
        /// </summary>
        /// <param name="message">object to be logged</param>
        public void LogError(string source, object message)
        {
            Log(LogSeverity.Error, source, message);
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
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // round off any logging and wait for the logger process to finish
                    isdisposing = true;
                    messagesavailable.Set();
                    loggertask.Wait();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }


    public enum LogSeverity
    {
        Debug, Info, Warning, Error
    }
}
