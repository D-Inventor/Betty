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

		// concurrent logging objects
		ConcurrentQueue<string> logQueue;
		ManualResetEventSlim loggingFlag;
		bool isLogging = false;

		public Bot()
		{
			// set up services and store reference to services that are relevant for this class
			services = BuildServiceProvider();
			settings = services.GetService<Settings>();
			analysis = services.GetService<Analysis>();
			agenda = services.GetService<Agenda>();
			constants = services.GetService<Constants>();

			//setup logging objects
			logQueue = new ConcurrentQueue<string>();
			loggingFlag = new ManualResetEventSlim(false);
		}

		public async Task Init()
		{
			// read settings from the file system
			settings.Init();

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
			// initiate logger process
			Task.Run((Action)LoggerProcess);

			// login and start
			await client.LoginAsync(TokenType.Bot, settings.Token);
			await client.StartAsync();

			// keep the bot running forever
			await Task.Delay(-1);
		}

		public IServiceProvider BuildServiceProvider() => new ServiceCollection()
			.AddSingleton<Constants>()
			.AddSingleton<DateTimeUtils>()
			.AddSingleton(x => new Settings(x))
			.AddSingleton(x => new Agenda(x))
			.AddSingleton(x => new Analysis(x))
			.BuildServiceProvider();

		private async Task Client_UserJoined(SocketGuildUser user)
		{
			// get configurations for given guild.
			GuildData gd = settings.GetGuildData(user.Guild);
			ulong? channel = gd.PublicChannel;
			if (!channel.HasValue) return;

			// if a public channel has been configured, send a welcome message to that channel
			var sc = user.Guild.GetTextChannel(channel.Value);

			await Task.Delay(10000);
			await sc.TriggerTypingAsync();
			await Task.Delay(3000);
			await sc.SendMessageAsync(gd.Language.GetString("event.join", new SentenceContext()
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

#warning TODO: Create security layers per guild.
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
					Console.WriteLine($"[{DateTime.Now}] Commands\t\t{result.ErrorReason}");
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
		
		private Task Client_GuildAvailable(SocketGuild guild)
		{
			// check if this guild has started applications and plan an event accordingly.
			if (settings.GetApplicationActive(guild) && settings.GetApplicationDeadline(guild).Value > DateTime.UtcNow)
			{
				agenda.Plan(guild, "Application selection", settings.GetApplicationDeadline(guild).Value, channelid: settings.GetApplicationChannel(guild), notifications: constants.ApplicationNotifications, savetoharddrive: false, action: async () =>
				{
					await (await settings.GetApplicationInvite(guild))?.DeleteAsync();
				});
			}

			return Task.CompletedTask;
		}

		private Task Client_Log(LogMessage msg)
		{
			// writes log messages to the console
			logQueue.Enqueue($"[{DateTime.UtcNow}] {msg.Source}: {msg.Message}");
			loggingFlag.Set();
			return Task.CompletedTask;
		}

		private void LoggerProcess()
		{
			// keep logging forever
			while (true)
			{
				// wait until a signal for flushing has been given, but only if the queue is currently empty.
				if (logQueue.Count == 0)
					loggingFlag.Wait();

				// make sure that the log directory exists
				string logpath = constants.PathToLogs();
				if (!Directory.Exists(logpath))
				{
					Directory.CreateDirectory(logpath);
				}

				// find the most recent log file in this directory
				string path = Directory.GetFiles(constants.PathToLogs()).Max(x => File.GetCreationTimeUtc(x));

				// open log file at given path if present and smaller than 20MB or create a new log file
				using(StreamWriter sw = new StreamWriter((path == null || new FileInfo(path).Length > 20 * 1024) ? Path.Combine(logpath, $"{DateTime.UtcNow:yyyyMMdd_HHmmss}.log") : path, true))
				{
					// write all entries to the log file
					while (logQueue.TryDequeue(out string msg))
					{
						sw.WriteLine(msg);
						Console.WriteLine(msg);
					}
				}

				// make sure that there are not more than 2 logfiles in the folder
				string[] files = Directory.GetFiles(logpath);
				if (files.Length > 2)
				{
					File.Delete(files.Min(x => File.GetCreationTimeUtc(x)));
				}

				// make sure that the logging flag is no longer set to prevent unnecessary work
				loggingFlag.Reset();
			}
		}
	}
}
