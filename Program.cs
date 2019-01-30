using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using System;

using Betty.databases.guilds;

namespace Betty
{
	class Program
    {
        static void Main(string[] args)
        {
			// test if the database functions properly
			//TestDatabase();

			// entry point of the application
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			new Program().MainAsync().GetAwaiter().GetResult();
        }

		private async Task MainAsync()
		{
			// create and run the bot
			Bot bot = new Bot();
			if(await bot.Init())
				await bot.Start();
			bot.Dispose();
		}

		private static void TestDatabase()
		{
			var database = new GuildDB();

			// create a guild
			database.Guilds.Add(new GuildTB
			{
				Name = "Awesome guild!",
			});
			database.SaveChanges();


			//get the guild
			var guilds = from g in database.Guilds
						 select g;

			// create an event
			database.Events.Add(new EventTB
			{
				Guild = guilds.First(),
				Name = "Amazing Event!",
				Date = new DateTime(2019, 11, 23),
			});
			database.SaveChanges();

			foreach(var g in from x in database.Guilds
							  select x.Events)
			{
				foreach(var e in g)
				{
					Console.WriteLine(e.Name);
				}
			}
		}
	}
}
