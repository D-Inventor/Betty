using System.IO;

using Microsoft.EntityFrameworkCore;

namespace Betty.Database
{
    /// <summary>
    /// Database for betty.
    /// </summary>
    public class BettyDB : DbContext
    {
        public DbSet<DiscordServer> DiscordServers { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<DiscordAppointment> DiscordAppointments { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentNotification> AppointmentNotifications { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={Path.Combine("data", "Database.db")}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // the discord appointment has a key as the composite of two columns
            modelBuilder.Entity<DiscordAppointment>()
                .HasKey(p => new { p.DiscordServerId, p.AppointmentId });
        }
    }
}
