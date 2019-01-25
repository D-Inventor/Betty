using System.Threading.Tasks;
using System.Globalization;
using System;

namespace Betty
{
	class Program
    {
        static void Main(string[] args)
        {
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
	}
}
