using System.IO;

using Microsoft.EntityFrameworkCore;

namespace Betty.databases.guilds
{
	public class GuildDB : DbContext
	{
		public DbSet<GuildTB> Guilds { get; set; }
		public DbSet<ApplicationTB> Applications { get; set; }
		public DbSet<EventTB> Events { get; set; }
		public DbSet<EventNotificationTB> EventNotifications { get; set; }
		public DbSet<PermissionTB> Permissions { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite($"Data Source={Path.Combine("data", "databases", "guild.db")}");
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// every guild has zero or one application
			modelBuilder.Entity<ApplicationTB>()
				.HasOne(p => p.Guild)
				.WithOne(i => i.Application)
				.HasForeignKey("ApplicationTB");

			// every guild has many events
			modelBuilder.Entity<GuildTB>()
				.HasMany(p => p.Events)
				.WithOne(i => i.Guild);

			// every event has many notifications
			modelBuilder.Entity<EventTB>()
				.HasMany(p => p.Notifications)
				.WithOne(i => i.Event);

			// every guild has many permissions
			modelBuilder.Entity<GuildTB>()
				.HasMany(p => p.Permissions)
				.WithOne(i => i.Guild);
		}
	}
}
