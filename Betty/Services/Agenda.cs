using Betty.Database;
using Betty.Utilities;
using Betty.Utilities.DateTimeUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace Betty.Services
{
    /// <summary>
    /// An agenda is a combination of a database and a datetime event.
    /// </summary>
    public class Agenda : IDisposable
    {
        private readonly DateTimeEvent dateTimeEvent;
        public IServiceProvider Services { get; set; }

        /// <summary>
        /// One should be able to make an agenda
        /// </summary>
        public Agenda(IServiceProvider services)
        {
            Services = services;
            dateTimeEvent = new DateTimeEvent();
            dateTimeEvent.OnDateTimeReached += DateTimeEvent_OnDateTimeReached;

            IDateTimeProvider dateTimeProvider = Services.GetService<IDateTimeProvider>() ?? new DateTimeProvider();
            DateTime currentdate = dateTimeProvider.UtcNow;

            UpdateAppointmentsBefore(currentdate);

            DateTime? NearestDate = GetNearestNotificationDate(currentdate);
            if(NearestDate != null) { dateTimeEvent.Start(NearestDate.Value); }
        }

        private void UpdateAppointmentsBefore(DateTime date)
        {
            ILogger logger = Services?.GetService<ILogger>();
            using(var database = Services.GetRequiredService<BettyDB>())
            {
                // get all appointments from the database that are in the past
                var appointments = from app in database.Appointments
                                   where app.Date < date
                                   select app;

                foreach(var app in appointments)
                {
                    if(app.Repetition.Unit == RepetitionUnit.Once)
                    {
                        // if this appointment was only once, remove it from the database
                        database.Entry(app)
                            .Collection(a => a.Notifications)
                            .Load();

                        database.AppointmentNotifications.RemoveRange(app.Notifications);
                        database.Appointments.Remove(app);

                        // log removal
                        logger?.LogInfo("Agenda", $"Appointment '{app.Title}' is removed from the database because its date is in the past and is not repetitive.");
                    }
                    else
                    {
                        // if this appointment is repetitive, update it so it contains a date in the future
                        do
                        {
                            app.Date = app.Repetition.GetNext(app.Date, app.Timezone);
                        } while (TimeZoneInfo.ConvertTimeToUtc(app.Date, app.Timezone) < date);
                        database.Appointments.Update(app);

                        // log update
                        logger?.LogInfo("Agenda", $"Appointment '{app.Title}' is updated, because its date was in the past, but was repetitive.");
                    }
                }

                database.SaveChanges();
            }
        }

        public DateTime? GetNearestNotificationDate(DateTime minimumDate)
        {
            using (var database = Services.GetRequiredService<BettyDB>())
            {
                var dates = from date in
                                (from app in database.Appointments
                                 select TimeZoneInfo.ConvertTimeToUtc(app.Date, app.Timezone))
                                 .Union(
                                 from not in database.AppointmentNotifications.Include(n => n.Appointment)
                                 select TimeZoneInfo.ConvertTimeToUtc(not.Appointment.Date, not.Appointment.Timezone) - not.Offset)
                            where date > minimumDate
                            orderby date
                            select date;

                if (dates.Any()) { return dates.First(); }
                return null;
            }
        }

        /// <summary>
        /// One should be able to plan an appointment
        /// </summary>
        /// <param name="appointment">The appointment to be planned</param>
        public async Task<MethodResult> PlanAsync(Appointment appointment)
        {
            // get all desired services
            IDateTimeProvider dateTimeProvider = Services.GetService<IDateTimeProvider>() ?? new DateTimeProvider();
            DateTime currentdate = dateTimeProvider.UtcNow;

            // validate the appointment
            MethodResult validationResult = ValidateAppointment(appointment, currentdate);
            if(validationResult != MethodResult.success) { return validationResult; }

            // add this appointment to the database
            using (var database = Services.GetRequiredService<BettyDB>())
            {
                database.Appointments.Add(appointment);
                await database.SaveChangesAsync();
            }

            // get the earliest notification date
            DateTime targetDate = appointment.Date;
            if (appointment.Notifications != null)
            {
                IEnumerable<TimeSpan> offsets = appointment.Notifications.Select(x => x.Offset).Where(x => targetDate - x > currentdate);
                if (offsets.Any())
                    targetDate -= offsets.Aggregate((x, y) => x > y ? x : y);
            }

            // update the DateTimeEvent
            // when handling the dateTimeEvent, it should be locked so that concurrent changes to the dateTimeEvent cannot cause any dates to be skipped.
            lock (dateTimeEvent)
            {
                if (dateTimeEvent.Target > targetDate)
                {
                    dateTimeEvent.Stop();
                    dateTimeEvent.Start(targetDate);
                }
            }

            // return success
            return MethodResult.success;
        }

        /// <summary>
        /// Check if the appointment is filled in as required
        /// </summary>
        /// <param name="appointment">The appointment to be validated</param>
        /// <returns></returns>
        public MethodResult ValidateAppointment(Appointment appointment, DateTime date)
        {
            ValidationContext validationContext = new ValidationContext(appointment);
            IList<ValidationResult> validationResults = new List<ValidationResult>();

            if(!Validator.TryValidateObject(appointment, validationContext, validationResults))
            {
                // return validation failure with all feedback
                return new MethodResult(103, $"The appointment is invalid. Feedback:\n{validationResults.Select(v => v.ErrorMessage).Aggregate((x, y) => $"{x}\n{y}")}");
            }

            // make sure that given time is valid in this timezone
            if (appointment.Timezone.IsInvalidTime(appointment.Date)) { return dateinvalid; }

            // make sure that given time is in the future
            if(TimeZoneInfo.ConvertTimeToUtc(appointment.Date, appointment.Timezone) < date) { return datepassed; }

            // return success
            return MethodResult.success;
        }

        /// <summary>
        /// One should be able to cancel an appointment
        /// </summary>
        public async Task<MethodResult> CancelAsync(ulong id)
        {
            ILogger logger = Services?.GetService<ILogger>();
            IDateTimeProvider dateTimeProvider = Services?.GetService<IDateTimeProvider>() ?? new DateTimeProvider();
            DateTime currentdate = dateTimeProvider.UtcNow;
            using(var database = Services.GetRequiredService<BettyDB>())
            {
                // find the appointment with given id
                var appointment = await (from app in database.Appointments.Include(a => a.Notifications)
                                         where app.Id == id
                                         select app).FirstOrDefaultAsync();

                // if there is no appointment with this id, return failure
                if(appointment == null)
                {
                    logger?.LogWarning("Agenda", $"Attempted to cancel event with id '{id}', but failed: no appointment with that id.");
                    return MethodResult.notfound;
                }

                // delete appointment from the database
                database.AppointmentNotifications.RemoveRange(appointment.Notifications);
                database.Appointments.Remove(appointment);
                await database.SaveChangesAsync();

                // get the earliest notification date
                DateTime targetDate = appointment.Date;
                if (appointment.Notifications != null)
                {
                    IEnumerable<TimeSpan> offsets = appointment.Notifications.Select(x => x.Offset).Where(x => targetDate - x >= currentdate);
                    if (offsets.Any())
                        targetDate -= offsets.Aggregate((x, y) => x > y ? x : y);
                }

                // update the datetime event if necessary
                lock (dateTimeEvent)
                {
                    if(dateTimeEvent.Target == targetDate)
                    {
                        dateTimeEvent.Stop();
                        DateTime? newDate = GetNearestNotificationDate(currentdate);
                        if(newDate != null) { dateTimeEvent.Start(newDate.Value); }
                    }
                }
            }

            return MethodResult.success;
        }

        private void DateTimeEvent_OnDateTimeReached(object source, DateTime arg)
        {
            ILogger logger = Services?.GetService<ILogger>();
            using(var database = Services.GetRequiredService<BettyDB>())
            {
                // get all appointments that need to be notified of
                var appointments =  from app in database.Appointments.Include(a => a.Notifications)
                                    let t = TimeZoneInfo.ConvertTimeToUtc(app.Date, app.Timezone)
                                    where t == arg || app.Notifications.Any(n => t - n.Offset == arg)
                                    select new AppointmentEventArgs { Appointment = app, Notification = app.Notifications.FirstOrDefault(n => t - n.Offset == arg) };

                foreach(var a in appointments)
                {
                    // notify subscribers of each appointment
                    AppointmentDue(a);
                    
                    // update database accordingly
                    if(TimeZoneInfo.ConvertTimeToUtc(a.Appointment.Date, a.Appointment.Timezone) == arg)
                    {
                        if(a.Appointment.Repetition.Unit == RepetitionUnit.Once)
                        {
                            // appointments that don't repeat get removed from the database
                            database.AppointmentNotifications.RemoveRange(a.Appointment.Notifications);
                            database.Appointments.Remove(a.Appointment);
                        }
                        else
                        {
                            // find the next datetime for repeating events
                            do
                            {
                                a.Appointment.Date = a.Appointment.Repetition.GetNext(a.Appointment.Date, a.Appointment.Timezone);
                            } while (TimeZoneInfo.ConvertTimeToUtc(a.Appointment.Date, a.Appointment.Timezone) < arg);
                            database.Appointments.Update(a.Appointment);
                        }
                    }
                }

                database.SaveChanges();

                // update DateTimeEvent
                // should be executed in a lock to prevent parallel handling of dateTimeEvent and accidentally forget appointments
                lock (dateTimeEvent)
                {
                    DateTime? newDate = GetNearestNotificationDate(arg);
                    if(newDate != null) { dateTimeEvent.Start(newDate.Value); }
                }
            }
        }

        #region IDisposable implementation
        public void Dispose()
        {
            dateTimeEvent?.Stop();
        }
        #endregion

        #region events
        public event EventHandler<AppointmentEventArgs> OnAppointmentDue;
        protected virtual void AppointmentDue(AppointmentEventArgs args)
        {
            EventHandler<AppointmentEventArgs> handler = OnAppointmentDue;
            if(handler != null)
            {
                OnAppointmentDue.Invoke(this, args);
            }
        }
        #endregion

        public static readonly MethodResult datepassed = new MethodResult(101, "The appointment has not been planned, because it is in the past.");
        public static readonly MethodResult dateinvalid = new MethodResult(102, "The appointment has not been planned, because the given date does not exist.");
    }










    /// <summary>
    /// Event arguments container for agenda events
    /// </summary>
    public class AppointmentEventArgs
    {
        /// <summary>
        /// The appointment which is being notified of
        /// </summary>
        public Appointment Appointment { get; set; }

        /// <summary>
        /// The notification that triggered the event if any
        /// </summary>
        public AppointmentNotification Notification { get; set; }
    }
}
