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
        }

        /// <summary>
        /// One should be able to plan an appointment
        /// </summary>
        /// <param name="appointment">The appointment to be planned</param>
        public async Task<MethodResult> PlanAsync(Appointment appointment)
        {
            ILogger logger = Services.GetService<ILogger>();
            IDateTimeProvider dateTimeProvider = Services.GetService<IDateTimeProvider>() ?? new DateTimeProvider();

            using (var database = Services.GetRequiredService<BettyDB>())
            {
                // check that appointment has all required fields
                var validationContext = new ValidationContext(appointment);
                try
                {
                    // attempt to validate
                    Validator.ValidateObject(appointment, validationContext);
                }
                catch (ValidationException e)
                {
                    // try log failure
                    if (logger != null)
                        logger.LogError("Agenda", "Attempted to plan appointment, but failed: The appointment was invalid.");
                    throw new ArgumentException("The given appointment is not valid.", e);
                }

                // check that the appointment is valid
                MethodResult result = ValidateAppointment(appointment);
                if (result != MethodResult.success) { return result; }

                // filter out all notifications that have already passed
                appointment.Notifications = appointment.Notifications?.Where(a => TimeZoneInfo.ConvertTimeToUtc(appointment.Date, appointment.Timezone) - a.Offset > dateTimeProvider.UtcNow.AddMinutes(2)).ToList();

                // add the appointment to the database
                database.Appointments.Add(appointment);
                await database.SaveChangesAsync();
            }

            // return success
            return MethodResult.success;
        }

        /// <summary>
        /// One should be able to cancel an appointment
        /// </summary>
        public async Task<MethodResult> CancelAsync(ulong id)
        {
            ILogger logger = Services.GetService<ILogger>();
            using (var database = Services.GetRequiredService<BettyDB>())
            {
                // try get the given appointment from the database
                Appointment appointment = (from app in database.Appointments.Include(a => a.Notifications)
                                           where app.Id == id
                                           select app).FirstOrDefault();

                if (appointment == null)
                {
                    // if there is no appointment with such id, try to log failure and return
                    if (logger != null)
                        logger.LogWarning("Agenda", $"Attempted to cancel appointment with id '{id}', but failed: No such appointment.");
                    return MethodResult.notfound;
                }

                database.AppointmentNotifications.RemoveRange(appointment.Notifications);
                database.Appointments.Remove(appointment);

                await database.SaveChangesAsync();
            }

            return MethodResult.success;
        }

        /// <summary>
        /// Check if the appointment is filled in as required
        /// </summary>
        /// <param name="appointment">The appointment to be validated</param>
        /// <returns></returns>
        private MethodResult ValidateAppointment(Appointment appointment)
        {
            IDateTimeProvider dateTimeProvider = Services.GetService<IDateTimeProvider>() ?? new DateTimeProvider();

            // a date has to be valid.
            if (appointment.Timezone.IsInvalidTime(appointment.Date)) { return dateinvalid; }

            DateTime utcdate = TimeZoneInfo.ConvertTimeToUtc(appointment.Date, appointment.Timezone);

            // a date should be at least 2 minutes into the future.
            if (utcdate < dateTimeProvider.UtcNow.AddMinutes(2)) { return datepassed; }

            return MethodResult.success;
        }

        #region IDisposable implementation
        public void Dispose()
        {
            dateTimeEvent?.Stop();
        }
        #endregion

        private void DateTimeEvent_OnDateTimeReached(object source, DateTime arg)
        {
            ILogger logger = Services.GetService<ILogger>();
            if (logger != null) { logger.LogInfo("Agenda", "A notification date was reached."); }

            using (var database = Services.GetRequiredService<BettyDB>())
            {
                // find all appointments which are due or have a notification which is due.
                var appointments = from app in database.Appointments.Include(a => a.Notifications)
                                   let t = TimeZoneInfo.ConvertTimeToUtc(app.Date, app.Timezone)
                                   where t == arg || app.Notifications.Any(n => t - n.Offset == arg)
                                   select new AppointmentEventArgs { Appointment = app, Notification = app.Notifications.FirstOrDefault(n => t - n.Offset == arg) };

                foreach (var a in appointments)
                {
                    // notify all subscribers of each event
                    AppointmentDue(a);

                    if(a.Appointment.Date == arg)
                    {
                        // if the appointment itself is now due, either update or remove it.
                        if(a.Appointment.Repetition.Unit == RepetitionUnit.Once)
                        {
                            // if this event was only once, remove it from the database
                            database.RemoveRange(a.Appointment.Notifications);
                            database.Remove(a.Appointment);
                        }
                        else
                        {
                            // if this appointment was repetitive, update the date to a new date.
                            a.Appointment.Date = a.Appointment.Repetition.GetNext(a.Appointment.Date, a.Appointment.Timezone);
                            database.Update(a.Appointment);
                        }
                    }
                }

                database.SaveChanges();
            }

            // restart the datetime event with the next upcoming date
            DateTime? newDate = GetNextNotificationDate();
            if(newDate.HasValue) { dateTimeEvent.Start(newDate.Value); }
        }

        private DateTime? GetNextNotificationDate()
        {
            using(var database = Services.GetRequiredService<BettyDB>())
            {
                IDateTimeProvider dateTimeProvider = Services.GetService<IDateTimeProvider>() ?? new DateTimeProvider();
                DateTime utcnow = dateTimeProvider.UtcNow;
                var dates = from date in
                                (from app in database.Appointments
                                 select TimeZoneInfo.ConvertTimeToUtc(app.Date, app.Timezone))
                                 .Union(from not in database.AppointmentNotifications.Include(n => n.Appointment)
                                        select TimeZoneInfo.ConvertTime(not.Appointment.Date, not.Appointment.Timezone) - not.Offset)
                            where date > utcnow
                            orderby date
                            select date;

                if (dates.Any()) { return dates.First(); }
                return null;
            }
        }

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
