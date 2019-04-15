using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Betty.Database
{
    public class DiscordServer
    {
        [Key]
        public ulong Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Language { get; set; }

        public ulong? PublicChannel { get; set; }

        public ulong? NotificationChannel { get; set; }

        public ulong? ClockChannel { get; set; }

        public Application Application { get; set; }

        public ICollection<DiscordAppointment> DiscordAppointments { get; set; }

        public ICollection<Permission> Permissions { get; set; }
    }
}
