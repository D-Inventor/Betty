using Betty.Database;
using Betty.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleSettings;
using System;
using System.IO;

namespace Betty
{
    class Program
    {
        static void Main()
        {
            if (!CheckFileIntegrity())
                return;
            IServiceProvider services = LoadServices();
        }

        private static bool CheckFileIntegrity()
        {
            // make sure that settings file exists
            string data = Path.Combine(Directory.GetCurrentDirectory(), "data");
            string file = Path.Combine(data, "Configurations.conf");
            if (!Directory.Exists(data) || !File.Exists(file))
            {
                // create required directory and file
                Directory.CreateDirectory(data);
                Settings.ToFile<Configurations>(null, file);

                // notify user that file needs to be filled in with desired settings
                Console.WriteLine("Configuration file was not found. Created a new template. Please fill in and restart the bot.");

                // return failure
                return false;
            }

            // migrate the database to the latest version
            using(var database = new BettyDB())
            {
                database.Database.Migrate();
            }

            // return success
            return true;
        }

        static IServiceProvider LoadServices()
        {
            // create all services
            string file = Path.Combine(Directory.GetCurrentDirectory(), "data", "Configurations.conf");
            Configurations configurations = Settings.FromFile<Configurations>(file);
            Logger logger = new Logger
            {
                LogSeverity = configurations.LogSeverity,
                StreamProvider = configurations.LogfileProvider
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddSingleton(configurations)
                .AddSingleton(logger);
            

            // return as service provider
            return serviceCollection.BuildServiceProvider();
        }
    }
}
