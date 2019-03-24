using System.Threading.Tasks;
using System.Globalization;

namespace Betty
{
	class Program
    {
        static void Main(string[] args)
        {
			// test if the database functions properly

			// entry point of the application
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			new Program().MainAsync().GetAwaiter().GetResult();
        }

		private async Task MainAsync()
		{
            // create and run the bot
            bool restart = true;
            while (restart)
            {
			    using (Bot bot = new Bot())
			    {
				    if (await bot.Init())
					    restart = await bot.Start();
			    }
            }
		}
	}
}
