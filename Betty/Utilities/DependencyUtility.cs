using Betty.Database;
using Betty.Services;
using Betty.Utilities.DateTimeUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleSettings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Betty.Utilities
{
    public static class DependencyUtility
    {
        public static IServiceProvider LoadServices()
        {
            string datapath = Path.Combine(Directory.GetCurrentDirectory(), "data");
            if (!Directory.Exists(datapath))
            {
                // make sure that the data folder exists
                Directory.CreateDirectory(datapath);
            }

            using(var database = new BettyDB())
            {
                // make sure that the database is migrated to the latest version
                database.Database.Migrate();
            }

            Configurations configurations = new Configurations();
            try
            {
                // try to load the configuration
                Settings.FromFile(configurations, Path.Combine(datapath, "Configurations.conf"));
            }
            catch (FileNotFoundException)
            {
                // communicate file absence to the user
                Settings.ToFile<Configurations>(null, Path.Combine(datapath, "Configurations.conf"));
                Console.WriteLine($"Could not read configurations.\nA new template was created\nPlease fill in the configuration file and try again.");
                return null;
            }
            catch (Exception e)
            {
                // communicate failure to the user
                Console.WriteLine($"An error occured while loading the configurations.\nPlease fix the error and try again:\n{e.Message}");
                return null;
            }

            // create a logger
            Logger logger = new Logger()
            {
                LogSeverity = configurations.LogSeverity,
                StreamProvider = configurations
            };

            // create a service provider from services
            IServiceProvider services = new ServiceCollection()
                .AddSingleton(configurations)
                .AddSingleton<ILogger>(logger)
                .AddSingleton<DateTimeProvider>()
                .AddTransient<BettyDB>()
                .AddSingleton(s => new Agenda(s))
                .AddSingleton(s => new Bot(s))
                .BuildServiceProvider();

            // bind service provider to these services
            configurations.Services = services;
            logger.Services = services;

            return services;
        }
    }
}
