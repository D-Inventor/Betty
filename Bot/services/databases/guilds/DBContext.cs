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

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite($"Data Source={Path.Combine("data", "databases", "guild.db")}");
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<GuildTB>()
				.HasOne(p => p.Application)
				.WithOne(i => i.Guild)
				.HasForeignKey<ApplicationTB>(b => b.GuildId);

			modelBuilder.Entity<GuildTB>()
				.HasMany(p => p.Events)
				.WithOne(i => i.Guild);

			modelBuilder.Entity<EventTB>()
				.HasMany(p => p.Notifications)
				.WithOne(i => i.Event);
		}
	}
}
