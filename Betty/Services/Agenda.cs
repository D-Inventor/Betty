using Betty.Database;
using Betty.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using TimeZoneConverter;

namespace Betty.Services
{
    /// <summary>
    /// An agenda is a combination of a database and a datetime event.
    /// </summary>
    public class Agenda
    {
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
        public MethodResult Plan(Appointment appointment)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// One should be able to cancel an appointment
        /// </summary>
        public MethodResult Cancel(ulong id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// One should be able to get notified when an appointment is due
        /// </summary>
        public event Action<Appointment> OnAppointmentDue
        {
            add { }
            remove { }
        }

        public static readonly MethodResult datepassed = new MethodResult(101, "The appointment has not been planned, because it is in the past.");
        public static readonly MethodResult dateinvalid = new MethodResult(102, "The appointment has not been planned, because the given date does not exist.");
        private readonly IServiceProvider services;
    }
}
