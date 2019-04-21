using System;
using System.IO;
using Betty.Utilities.DateTimeUtilities;
using Microsoft.EntityFrameworkCore;

using TimeZoneConverter;

namespace Betty.Database
{
    /// <summary>
    /// Database for betty.
    /// </summary>
    public class BettyDB : DbContext
    {
        #region Tables
        public DbSet<DiscordServer> DiscordServers { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<DiscordAppointment> DiscordAppointments { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentNotification> AppointmentNotifications { get; set; }
        #endregion

        public BettyDB() : base() { }
        public BettyDB(DbContextOptions<BettyDB> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // the database uses a local file
                optionsBuilder.UseSqlite($"Data Source={Path.Combine("data", "Database.db")}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // the discord appointment has a key as the composite of two columns
            modelBuilder.Entity<DiscordAppointment>()
                .HasKey(p => new { p.DiscordServerId, p.AppointmentId });

            modelBuilder.Entity<Appointment>()
                .Property(t => t.Timezone)
                .HasConversion(
                    v => v.Id,
                    v => TZConvert.GetTimeZoneInfo(v));

            modelBuilder.Entity<Appointment>()
                .Property(t => t.Repetition)
                .HasConversion(
                    v => v.Id,
                    v => Repetition.FromId(v));
        }
    }
}
