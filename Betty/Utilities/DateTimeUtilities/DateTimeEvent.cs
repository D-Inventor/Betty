using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Betty.Utilities.DateTimeUtilities
{
    /// <summary>
    /// This class implements waiting for a specific date/time.
    /// </summary>
    public class DateTimeEvent
    {
        private Task backgroundProcess;
        private CancellationTokenSource cancellationTokenSource;
        private readonly object activationLocker = new object();

        public IServiceProvider Services { get; set; }

        /// <summary>
        /// One should be able to create a new DateTimeEvent
        /// </summary>
        public DateTimeEvent(IServiceProvider services = null)
        {
            backgroundProcess = null;
            cancellationTokenSource = null;
            IsActive = false;
            Services = services;
        }

        /// <summary>
        /// One should be able to see if the event is active or not.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// One should be able to see for which DateTime this event is set.
        /// </summary>
        public DateTime Target { get; private set; }

        /// <summary>
        /// One should be able to start the event for a specific datetime.
        /// </summary>
        public void Start(DateTime target)
        {
            lock (activationLocker)
            {
                // make sure that the event hasn't already been started
                if (IsActive) { throw new InvalidOperationException("This event has already been started."); }

                // set data
                Target = target;
                IsActive = true;
            }

            // create and start the background process
            // use a local copy of the token source so that during notification, the member token can be overwritten.
            cancellationTokenSource = new CancellationTokenSource();
            var localTokenSource = cancellationTokenSource;
            backgroundProcess = new Task(() => { WaiterTask().Wait(); }, cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
            backgroundProcess.ContinueWith((_) => { localTokenSource.Dispose(); });
            backgroundProcess.Start();
        }

        /// <summary>
        /// The task that the background process runs.
        /// </summary>
        protected async Task WaiterTask()
        {
            IDateTimeProvider dateTimeProvider = Services?.GetService<IDateTimeProvider>() ?? new DateTimeProvider();
            while (true)
            {
                // stop waiting if the target time has passed
                DateTime now = dateTimeProvider.UtcNow;
                if(Target <= now) { break; }

                // wait for the given time or the maximum amount of milliseconds if date is too far away
                double waittime = (Target - now).TotalMilliseconds;
                if(waittime > int.MaxValue) { waittime = int.MaxValue; }
                await Task.Delay((int)waittime);
            }

            IsActive = false;

            // notify subscribers
            DateTimeReached(Target);
        }

        /// <summary>
        /// One should be able to stop this event.
        /// </summary>
        public void Stop()
        {
            IsActive = false;
            if(cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested) { cancellationTokenSource.Cancel(); }
        }

        #region events
        /// <summary>
        /// One should be able to be notified when this datetime is reached.
        /// </summary>
        public event EventHandler<DateTime> OnDateTimeReached;

        protected virtual void DateTimeReached(DateTime arg)
        {
            EventHandler<DateTime> handler = OnDateTimeReached;
            handler?.Invoke(this, arg);
        }
        #endregion
    }
}
