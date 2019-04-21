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
    public class Agenda
    {
        private readonly IServiceProvider services;
        private readonly DateTimeEvent dateTimeEvent;

        /// <summary>
        /// One should be able to make an agenda
        /// </summary>
        public Agenda(IServiceProvider services)
        {
            this.services = services;
            dateTimeEvent = new DateTimeEvent();
            dateTimeEvent.OnDateTimeReached += DateTimeEvent_OnDateTimeReached;
        }

        /// <summary>
        /// One should be able to plan an appointment
        /// </summary>
        /// <param name="appointment">The appointment to be planned</param>
        public async Task<MethodResult> PlanAsync(Appointment appointment)
        {
            ILogger logger = services.GetService<ILogger>();

            using (var database = services.GetRequiredService<BettyDB>())
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
                appointment.Notifications = appointment.Notifications?.Where(a => TimeZoneInfo.ConvertTimeToUtc(appointment.Date, appointment.Timezone) - a.Offset > DateTime.UtcNow.AddMinutes(2)).ToList();

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
            ILogger logger = services.GetService<ILogger>();
            using (var database = services.GetRequiredService<BettyDB>())
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
            // a date has to be valid.
            if (appointment.Timezone.IsInvalidTime(appointment.Date)) { return dateinvalid; }

            DateTime utcdate = TimeZoneInfo.ConvertTimeToUtc(appointment.Date, appointment.Timezone);

            // a date should be at least 2 minutes into the future.
            if (utcdate < DateTime.UtcNow.AddMinutes(2)) { return datepassed; }

            return MethodResult.success;
        }

        private void DateTimeEvent_OnDateTimeReached(object source, DateTime arg)
        {
            ILogger logger = services.GetService<ILogger>();
            if (logger != null) { logger.LogInfo("Agenda", "A notification date was reached. Subscribers will get notified."); }

            IList<Appointment> appointments;
            using (var database = services.GetRequiredService<BettyDB>())
            {
                // find all appointments which are due or have a notification which is due.
                appointments = (from app in database.Appointments.Include(a => a.Notifications)
                                let t = TimeZoneInfo.ConvertTimeToUtc(app.Date, app.Timezone)
                                where t == arg || app.Notifications.Any(n => t - n.Offset == arg)
                                select app).ToList();

                // notify all subscribers of each event
                foreach (Appointment a in appointments)
                {
                    //foreach (var s in subscribers)
                    //{
                    //    s(a);
                    //}

                    // remove either a notification or the appointment itself from the database
                    AppointmentNotification appnot = a.Notifications.OrderByDescending(n => n.Offset).FirstOrDefault();
                    if (appnot != null) { database.AppointmentNotifications.Remove(appnot); }
                    else { database.Appointments.Remove(a); }
                }

                database.SaveChanges();

                // find the next date to be notified of
            }
        }

        private DateTime GetNextNotificationDate()
        {
            throw new NotImplementedException();
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


    public class AppointmentEventArgs
    {
        public Appointment Appointment { get; set; }
        public AppointmentNotification Notification { get; set; }
    }
}
