using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Betty.Database
{
    public class DiscordAppointment
    {
        [Required]
        public ulong DiscordServerId { get; set; }
        public DiscordServer DiscordServer { get; set; }


        [Required]
        public ulong AppointmentId { get; set; }
        public Appointment Appointment { get; set; }


        public ulong? NotificationChannel { get; set; }
    }
}
