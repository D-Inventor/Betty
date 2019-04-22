using Betty.Database;
using Betty.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleSettings;
using System;
using System.IO;

using Betty.Utilities;
using System.Threading.Tasks;

namespace Betty
{
    class Program
    {
        static void Main()
        {
            MainAsync().Wait();
        }

        static async Task MainAsync()
        {
            IServiceProvider services = DependencyUtility.LoadServices();
            if (services == null) { return; }

            await services.GetRequiredService<Bot>().Run();
        }
    }
}
