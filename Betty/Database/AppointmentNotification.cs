using System;
using System.ComponentModel.DataAnnotations;

namespace Betty.Database
{
    public class AppointmentNotification
    {
        [Key]
        public ulong Id { get; set; }

        [Required]
        public ulong AppointmentId { get; set; }
        public Appointment Appointment { get; set; }

        [Required]
        public TimeSpan Offset { get; set; }
    }
}
