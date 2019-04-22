using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Betty.Services;
using Betty.Utilities.DiscordUtilities;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Betty
{
    public class Bot
    {
        private ManualResetEvent stopEvent;

        public IServiceProvider Services { get; set; }
        public DiscordSocketClient Client { get; private set; }
        public CommandService CommandService { get; private set; }

        public Bot(IServiceProvider services)
        {
            Services = services;
            Configurations configurations = Services.GetRequiredService<Configurations>();

            stopEvent = new ManualResetEvent(false);

            // configure state
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = TypeTranslations.LogSeverity(configurations.LogSeverity),
            });

            CommandService = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
                LogLevel = TypeTranslations.LogSeverity(configurations.LogSeverity)
            });

            // subscribe to events
            Client.Log += Client_Log;
            Client.Ready += Client_Ready;
        }

        private async Task Client_Ready()
        {
            Configurations configurations = Services.GetRequiredService<Configurations>();

            await Client.SetGameAsync("you", null, ActivityType.Watching);
            SocketUser owner = Client.GetUser(configurations.OwnerId);
            await owner.SendMessageAsync("Ready");
        }

        private Task Client_Log(LogMessage arg)
        {
            Services.GetService<ILogger>()?.Log(arg);
            return Task.CompletedTask;
        }

        public async Task Run()
        {
            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), Services);

            Configurations configurations = Services.GetRequiredService<Configurations>();

            try
            {
                await Client.LoginAsync(TokenType.Bot, configurations.Token);
            }
            catch(Exception e)
            {
                Services.GetService<ILogger>()?.LogError("Bot", $"Attempted to log in, but failed: {e.Message}\n{e.StackTrace}");
                return;
            }

            await Client.StartAsync();

            stopEvent.WaitOne();

            await Client.StopAsync();
            await Client.LogoutAsync();
        }
    }
}
