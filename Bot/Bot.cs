using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Collections.Concurrent;
using System.IO;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

using Betty.databases.guilds;
using Betty.utilities;
using Microsoft.EntityFrameworkCore;

namespace Betty
{
	public class Bot : IDisposable
	{
		// discord environment
		DiscordSocketClient client;
		CommandService commands;

		// services
		IServiceProvider services;
		Settings settings;
		Analysis analysis;
		Agenda agenda;
		Constants constants;
		Logger logger;
		StateCollection statecollection;
		Notifier notifier;
        ManualResetEventSlim exitevent;

		public Bot()
		{
			// set up services and store reference to services that are relevant for this class
			services = BuildServiceProvider();
			settings = services.GetService<Settings>();
			agenda = services.GetService<Agenda>();
			constants = services.GetService<Constants>();
			logger = services.GetService<Logger>();
			statecollection = services.GetService<StateCollection>();
			notifier = services.GetService<Notifier>();
            exitevent = services.GetService<ManualResetEventSlim>();
            exitevent.Reset();
			analysis = new Analysis(services);
		}

		#region public interface
		public async Task<bool> Init()
		{
			// initiate logger process
			logger.Init();

			// migrate the database to the latest version
			string dbpath = Path.Combine(constants.PathToData(), "databases");
			if (!Directory.Exists(dbpath))
				Directory.CreateDirectory(dbpath);

			using(var database = new GuildDB())
			{
				database.Database.Migrate();
			}

			// read settings from the file system
			if (!settings.Init())
			{
				logger.Log(new LogMessage(LogSeverity.Error, "Bot", $"Initialisation of configurations failed"));
				return false;
			}

			// create discord objects
			client = new DiscordSocketClient(new DiscordSocketConfig
			{
				LogLevel = settings.LogLevel
			});

			commands = new CommandService(new CommandServiceConfig
			{
				CaseSensitiveCommands = false,
				DefaultRunMode = RunMode.Async,
				LogLevel = settings.LogLevel
			});
			await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);

			// subscribe to all relevant events
			SetupEventSubscriptions();

			return true;
		}

		public async Task Start()
		{
			// try to login
			try
			{
				await client.LoginAsync(TokenType.Bot, settings.Token);
			}
			catch (Exception e)
			{
				logger.Log(new LogMessage(LogSeverity.Error, "Bot", $"Attempted to log in, but failed: {e.Message}", e));
				return;
			}

			// start
			await client.StartAsync();

            // keep the bot running until an exit signal is given
            exitevent.Wait();

            await client.StopAsync();
            await client.LogoutAsync();

			//await Task.Delay(-1);
		}

		public void Dispose()
		{
			logger.Dispose();
		}
		#endregion

		#region private methods
		private void SetupEventSubscriptions()
		{
			client.Log += Client_Log;
			client.Ready += Client_Ready;
			client.GuildAvailable += Client_GuildAvailable_RestoreApplication;
			client.GuildAvailable += Client_GuildAvailable_RestoreEvents;
			client.MessageReceived += Client_MessageReceived;
			client.UserJoined += Client_UserJoined;
		}

		private IServiceProvider BuildServiceProvider() => new ServiceCollection()
			.AddSingleton<Constants>()
			.AddSingleton(x => new Logger(x))
			.AddSingleton(x => new StateCollection(x))
			.AddSingleton(x => new NotifierFactory(x))
			.AddSingleton(x => new Settings(x))
			.AddSingleton(x => new Agenda(x))
            .AddSingleton<ManualResetEventSlim>()
			.BuildServiceProvider();
		#endregion

		#region event listeners
		private async Task Client_UserJoined(SocketGuildUser user)
		{
			using (var database = new GuildDB())
			{
				// find the public channel of given guild
				var sc = statecollection.GetPublicChannel(user.Guild, database);
				if (sc == null) return;

				// add some delays to make Betty's response seem more natural
				await Task.Delay(10000);
				await sc.TriggerTypingAsync();
				await Task.Delay(3000);
				var language = statecollection.GetLanguage(user.Guild, database);
				await sc.SendMessageAsync(language.GetString("event.join", new SentenceContext()
																				.Add("mention", user.Mention)));
			}
		}

		private async Task Client_MessageReceived(SocketMessage arg)
		{
			// get context variables
			var message = arg as SocketUserMessage;
			var context = new SocketCommandContext(client, message);

			// make sure that the message is valid and from a valid source
			if (context.Message == null || context.Message.Content == "") return;
			if (context.User.IsBot) return;

			// check if message is a command
			int argPos = 0;
			if (message.HasStringPrefix("$", ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))
			{
				// execute command
				var result = await commands.ExecuteAsync(context, argPos, services);
				if (!result.IsSuccess)
				{
					logger.Log(new LogMessage(LogSeverity.Warning, "Commands", $"Attempted to execute command '{message.Content}', but failed: {result.ErrorReason}"));
				}
			}
			else
			{
				// do scans on message
				await analysis.AnalyseTime(context);
			}
		}

		private async Task Client_Ready()
		{
			// set playing game to respository url
			await client.SetGameAsync(@"https://github.com/D-Inventor/Betty");
		}

		private async Task Client_GuildAvailable_RestoreApplication(SocketGuild guild)
		{
			// ask state collection to restore application and log start and finish
			logger.Log(new LogMessage(LogSeverity.Info, "Bot", $"Restoring application for '{guild.Name}'"));
			using (var database = new GuildDB())
				await statecollection.RestoreApplication(guild, database);
			logger.Log(new LogMessage(LogSeverity.Info, "Bot", $"Successfully restored application for '{guild.Name}'"));
		}

		private Task Client_GuildAvailable_RestoreEvents(SocketGuild guild)
		{
			// ask agenda to restore events and log start and finish
			logger.Log(new LogMessage(LogSeverity.Info, "Bot", $"Restoring events for '{guild.Name}'"));
			using (var database = new GuildDB())
				agenda.RestoreGuildEvents(guild, database);
			logger.Log(new LogMessage(LogSeverity.Info, "Bot", $"Successfully restored events for '{guild.Name}'"));
			return Task.CompletedTask;
		}

		private Task Client_Log(LogMessage msg)
		{
			// writes log messages to the console
			logger.Log(msg);
			return Task.CompletedTask;
		}
		#endregion
	}
}
