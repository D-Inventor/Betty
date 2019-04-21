using Betty.Utilities.DateTimeUtilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Betty.Database
{
    public class Appointment
    {
        [Key]
        public ulong Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeZoneInfo Timezone { get; set; }

        [Required]
        public Repetition Repetition { get; set; }

        public ICollection<AppointmentNotification> Notifications { get; set; }
    }
}
