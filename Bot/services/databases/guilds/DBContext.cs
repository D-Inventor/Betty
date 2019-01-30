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
	}
}
