using Betty.WebAPI;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.AspNetCore.Hosting;

namespace Betty
{
	class Program
    {
        static void Main(string[] args)
        {
			// entry point of the application
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // start both the bot and the web api
            var bottask = RunBotAsync();
            var webapitask = RunWebAPIAsync();

            // keep running until one of these tasks ends
            Task.WaitAny(bottask, webapitask);
        }

		private static async Task RunBotAsync()
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

        private static async Task RunWebAPIAsync()
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseUrls("http://*:4032", "https://*:4033")
                .Build();

            await host.RunAsync();
        }
	}
}
