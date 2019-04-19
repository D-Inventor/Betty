using Betty.Database;
using Betty.Utilities;
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

        /// <summary>
        /// One should be able to make an agenda
        /// </summary>
        public Agenda(IServiceProvider services)
        {
            this.services = services;
        }

        /// <summary>
        /// One should be able to plan an appointment
        /// </summary>
        /// <param name="appointment">The appointment to be planned</param>
        public async Task<MethodResult> PlanAsync(Appointment appointment)
        {
            ILogger logger = services.GetService<ILogger>();

            using(var database = services.GetRequiredService<BettyDB>())
            {
                // check that appointment has all required fields
                var validationContext = new ValidationContext(appointment);
                try
                {
                    // attempt to validate
                    Validator.ValidateObject(appointment, validationContext);
                }
                catch(ValidationException e)
                {
                    // try log failure
                    if (logger != null)
                        logger.LogError("Agenda", "Attempted to plan appointment, but failed: The appointment was invalid.");
                    throw new ArgumentException("The given appointment is not valid.", e);
                }

                // check that the appointment is valid
                MethodResult result = ValidateAppointment(appointment);
                if (result != MethodResult.success) { return result; }

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

                if(appointment == null)
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
        /// One should be able to get notified when an appointment is due
        /// </summary>
        public event Action<Appointment> OnAppointmentDue
        {
            add { }
            remove { }
        }

        private MethodResult ValidateAppointment(Appointment appointment)
        {
            throw new NotImplementedException();
        }

        public static readonly MethodResult datepassed = new MethodResult(101, "The appointment has not been planned, because it is in the past.");
        public static readonly MethodResult dateinvalid = new MethodResult(102, "The appointment has not been planned, because the given date does not exist.");
    }
}
