﻿using System;
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

namespace Betty
{
	public class Bot
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
		GuildDB database;
		Notifier notifier;

		public Bot()
		{
			// set up services and store reference to services that are relevant for this class
			services = BuildServiceProvider();
			settings = services.GetService<Settings>();
			agenda = services.GetService<Agenda>();
			constants = services.GetService<Constants>();
			logger = services.GetService<Logger>();
			statecollection = services.GetService<StateCollection>();
			database = services.GetService<GuildDB>();
			notifier = services.GetService<Notifier>();
			analysis = new Analysis(services);
		}

		public async Task<bool> Init()
		{
			// initiate logger process
			logger.Init();

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

		private void SetupEventSubscriptions()
		{
			client.Log += Client_Log;
			client.Ready += Client_Ready;
			client.GuildAvailable += Client_GuildAvailable;
			client.MessageReceived += Client_MessageReceived;
			client.UserJoined += Client_UserJoined;
		}

		public async Task Start()
		{
			// try to login
			try
			{
				await client.LoginAsync(TokenType.Bot, settings.Token);
			}
			catch(Discord.Net.HttpException e)
			{
				logger.Log(new LogMessage(LogSeverity.Error, "Bot", $"Attempted to log in, but failed: {e.Reason}", e));
				return;
			}

			// start
			await client.StartAsync();

			// keep the bot running forever
			await Task.Delay(-1);
		}

		public void Dispose()
		{
			logger.Dispose();
		}

		public IServiceProvider BuildServiceProvider() => new ServiceCollection()
			.AddSingleton<Constants>()
			.AddDbContext<GuildDB>()
			.AddSingleton(x => new Logger(x))
			.AddSingleton(x => new StateCollection(x))
			.AddSingleton(x => new Notifier(x))
			.AddSingleton(x => new Settings(x))
			.AddSingleton(x => new Agenda(x))
			.BuildServiceProvider();

		private async Task Client_UserJoined(SocketGuildUser user)
		{
			var sc = statecollection.GetPublicChannel(user.Guild);
			if (sc == null) return;

			await Task.Delay(10000);
			await sc.TriggerTypingAsync();
			await Task.Delay(3000);
			var language = statecollection.GetLanguage(user.Guild);
			await sc.SendMessageAsync(language.GetString("event.join", new SentenceContext()
																			.Add("mention", user.Mention)));
		}

		private async Task Client_MessageReceived(SocketMessage arg)
		{
			// get context variables
			var message = arg as SocketUserMessage;
			var context = new SocketCommandContext(client, message);

			// make sure that the message is valid and from a valid source
			if (context.Message == null || context.Message.Content == "") return;
			if (context.User.IsBot) return;
			
			// make sure that only users who are member can use the bot
			if (!(context.User as SocketGuildUser).Roles.Select(r => r.Name).Contains("Member")) return;

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
			await client.SetGameAsync(@"https://github.com/D-Inventor/Betty");
		}
		
		private async Task Client_GuildAvailable(SocketGuild guild)
		{
			// check if this guild was having applications and create the notifications if necessary
			if (statecollection.GetApplicationActive(guild))
			{
				logger.Log(new LogMessage(LogSeverity.Info, "Bot", $"Reading application data from database for {guild.Name}"));

				// create notifications and store cancellation token
				var ctoken = notifier.CreateWaiterTask(guild, statecollection.GetApplicationChannel(guild), messages: DateTimeMethods.BuildMessageList(constants.ApplicationNotifications, statecollection.GetApplicationDeadline(guild).Value, "Application selection")
								, action: async () =>
								{
									IInviteMetadata invite = await statecollection.GetApplicationInvite(guild);
									try
									{
										await invite?.DeleteAsync();
									}
									catch(Exception e)
									{
										logger.Log(new LogMessage(LogSeverity.Warning, "Commands", $"Attempted to delete invitation in {guild.Name}, but failed: {e.Message}"));
									}
								});

				statecollection.SetApplicationToken(guild, ctoken);
			}
		}

		private Task Client_Log(LogMessage msg)
		{
			// writes log messages to the console
			logger.Log(msg);
			return Task.CompletedTask;
		}
	}
}
