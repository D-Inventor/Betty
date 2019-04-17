using Betty.Database;
using Betty.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleSettings;
using System;
using System.IO;

using Betty.Utilities;

namespace Betty
{
    class Program
    {
        static void Main()
        {
            IServiceProvider services = DependencyUtility.LoadServices();
            if (services == null)
                return;
        }
    }
}
