using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.IO;

using Microsoft.Extensions.DependencyInjection;

using Discord.WebSocket;
using Discord.Commands;
using Discord;

namespace Betty
{
	public class GuildData
	{
		private ulong id;
		private ulong? public_channel;
		private ulong? notification_channel;
		private StringConverter language;

		private bool appactive;
		private ulong? application_channel;
		private string invite;
		private DateTime? deadline;

		Constants constants;
		Logger logger;

		private GuildData(ulong id, ulong? public_channel, ulong? notification_channel, bool appactive, ulong? application_channel, string invite, DateTime? deadline, StringConverter language, IServiceProvider services)
		{
			this.id = id;
			this.public_channel = public_channel;
			this.notification_channel = notification_channel;
			this.language = language;

			this.appactive = appactive;
			this.application_channel = application_channel;
			this.invite = invite;
			this.deadline = deadline;

			appactive = false;
			application_channel = null;
			invite = null;

			this.constants = services.GetService<Constants>();
			this.logger = services.GetService<Logger>();
		}

		private static GuildData GetDefault(SocketGuild guild, IServiceProvider services)
		{
			var constants = services.GetService<Constants>();
			StringConverter sc = StringConverter.LoadFromFile(constants.PathToLanguage(constants.DefaultLanguage), services.GetService<Logger>());

			return new GuildData(guild.Id, null, null, false, null, null, null, sc, services);
		}

		public static GuildData FromFile(SocketGuild guild, IServiceProvider services)
		{
			var constants = services.GetService<Constants>();
			var logger = services.GetService<Logger>();
			string guildpath = constants.PathToGuild(guild.Id);

			// make sure that the file exists or create a new default
			if (!File.Exists(guildpath))
			{
				logger.Log(new LogMessage(LogSeverity.Info, "GuildDataLoader", $"{guild.Name} doesn't have a configuration yet. Creating a new one."));
				GuildData data = GetDefault(guild, services);
				data.Save();
				return data;
			}

			// read all the entries from the file
			Dictionary<string, string> entries = new Dictionary<string, string>();
			using(var file = new StreamReader(guildpath))
			{
				string line;
				while((line = file.ReadLine()) != null)
				{
					int separator = line.IndexOf(':');
					if (separator < 0)
					{
						logger.Log(new LogMessage(LogSeverity.Warning, "GuildDataLoader", $"Couldn't interpret option: {line}"));
						continue;
					}
					string key = line.Substring(0, separator);
					separator++;
					string value = line.Substring(separator, line.Length - separator);
					entries.Add(key, value);
				}
			}
			
			// parse entries into objects
			StringConverter sc = StringConverter.LoadFromFile(constants.PathToLanguage(entries.ContainsKey("LANG") ? entries["LANG"] : constants.DefaultLanguage), logger);
			ulong? public_channel = null;
			if (entries.ContainsKey("PUBCHANNEL") && entries["PUBCHANNEL"] != "null") public_channel = ulong.Parse(entries["PUBCHANNEL"]);
			ulong? notification_channel = null;
			if (entries.ContainsKey("NOTCHANNEL") && entries["NOTCHANNEL"] != "null") notification_channel = ulong.Parse(entries["NOTCHANNEL"]);

			bool appactive = false;
			if (entries.ContainsKey("APPACTIVE")) appactive = bool.Parse(entries["APPACTIVE"]);

			ulong? application_channel = null;
			string invite = null;
			DateTime? deadline = null;

			if (appactive)
			{
				if (entries.ContainsKey("APPCHANNEL") && entries["APPCHANNEL"] != "null") application_channel = ulong.Parse(entries["APPCHANNEL"]);
				if (entries.ContainsKey("APPINVITE") && entries["APPINVITE"] != "null") invite = entries["APPINVITE"];
				if (entries.ContainsKey("APPDEADLINE") && entries["APPDEADLINE"] != "null") deadline = DateTime.Parse(entries["APPDEADLINE"]);

				// if the application data is corrupt/ incorrect, pretend as if it doesn't exist
				if(application_channel == null || invite == null || deadline == null)
				{
					logger.Log(new LogMessage(LogSeverity.Warning, "GuildDataLoader", $"Application data for '{guild.Name}' is invalid, ignoring."));
					appactive = false;
				}
			}

			return new GuildData(guild.Id, public_channel, notification_channel, appactive, application_channel, invite, deadline, sc, services);
		}

		public void Save()
		{
			string path = constants.PathToGuild(id);
			using(var file = new StreamWriter(path))
			{
				file.WriteLine($"LANG:{language.Name}");
				file.WriteLine($"PUBCHANNEL:{(public_channel.HasValue ? public_channel.Value.ToString() : "null")}");
				file.WriteLine($"NOTCHANNEL:{(notification_channel.HasValue ? notification_channel.Value.ToString() : "null")}");
				file.WriteLine($"APPACTIVE:{appactive}");
				if (appactive)
				{
					file.WriteLine($"APPCHANNEL:{application_channel.Value}");
					file.WriteLine($"APPINVITE:{invite}");
					file.WriteLine($"APPDEADLINE:{(deadline.HasValue ? deadline.Value.ToString() : "null")}");
				}
			}
		}

		public StringConverter Language { get { return language; } }

		public ulong? PublicChannel
		{
			get { return public_channel; }
			set
			{
				this.public_channel = value;
				Save();
			}
		}

		public ulong? NotificationChannel
		{
			get { return notification_channel; }
			set
			{
				this.notification_channel = value;
				Save();
			}
		}

		public void SetApplication(bool active, ulong? application_channel, string invite, DateTime? deadline)
		{
			this.appactive = active;
			this.application_channel = application_channel;
			this.invite = invite;
			this.deadline = deadline;
			Save();
		}

		public ulong? ApplicationChannel { get { return application_channel; } }
		public string Invite { get { return invite; } }
		public bool AppActive { get { return appactive; } }
		public DateTime? Deadline { get { return deadline; } }
	}
}
