using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Betty.Utilities.DateTimeUtilities
{
    /// <summary>
    /// This class implements waiting for a specific date/time.
    /// </summary>
    public class DateTimeEvent
    {
        private readonly List<Action<DateTime>> subscribers;
        private Task backgroundProcess;
        private CancellationTokenSource cancellationTokenSource;
        private readonly object activationLocker = new object();

        /// <summary>
        /// One should be able to create a new DateTimeEvent
        /// </summary>
        public DateTimeEvent()
        {
            subscribers = new List<Action<DateTime>>();
            backgroundProcess = null;
            cancellationTokenSource = null;
            IsActive = false;
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
            cancellationTokenSource = new CancellationTokenSource();
            backgroundProcess = new Task(async () => await WaiterTask(), cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
            backgroundProcess.ContinueWith((_) => cancellationTokenSource.Dispose());
            backgroundProcess.Start();
        }

        /// <summary>
        /// The task that the background process runs.
        /// </summary>
        protected async Task WaiterTask()
        {
            while (true)
            {
                // stop waiting if the target time has passed
                DateTime now = DateTime.UtcNow;
                if(Target <= now) { break; }

                // wait for the given time or the maximum amount of milliseconds if date is too far away
                double waittime = (Target - now).TotalMilliseconds;
                if(waittime > int.MaxValue) { waittime = int.MaxValue; }
                await Task.Delay((int)waittime);
            }

            // lock subscribers so that nobody can subscribe while notifications are happening
            lock (subscribers)
            {
                // notify all subscribers
                foreach (var s in subscribers)
                {
                    s(Target);
                }
            }

            IsActive = false;
        }

        /// <summary>
        /// One should be able to stop this event.
        /// </summary>
        public void Stop()
        {
            // lock prevents stop in the middle of a notification
            lock (subscribers)
            {
                IsActive = false;
                if(cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested) { cancellationTokenSource.Cancel(); }
            }
        }

        /// <summary>
        /// One should be able to be notified when this datetime is reached.
        /// </summary>
        public event Action<DateTime> OnDateTimeReached
        {
            add { lock (subscribers) { subscribers.Add(value); } }
            remove { lock (subscribers) { subscribers.Remove(value); } }
        }
    }
}
