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

		private GuildData(ulong id, ulong? public_channel, ulong? notification_channel, bool appactive, ulong? application_channel, string invite, DateTime? deadline, StringConverter language, Constants constants)
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

			this.constants = constants;
		}

		private static GuildData GetDefault(SocketGuild guild, Constants constants)
		{
			StringConverter sc = StringConverter.LoadFromFile(constants.PathToLanguage(constants.DefaultLanguage));

			return new GuildData(guild.Id, null, null, false, null, null, null, sc, constants);
		}

		public static GuildData FromFile(SocketGuild guild, Constants constants)
		{
			string guildpath = constants.PathToGuild(guild.Id);

			// make sure that the file exists or create a new default
			if (!File.Exists(guildpath))
			{
				Console.WriteLine($"{guild.Name} doesn't have a configuration yet. Creating a new one.");
				GuildData data = GetDefault(guild, constants);
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
						Console.WriteLine($"BAD OPTION: {line}");
						continue;
					}
					string key = line.Substring(0, separator);
					separator++;
					string value = line.Substring(separator, line.Length - separator);
					entries.Add(key, value);
				}
			}
			
			// parse entries into objects
			StringConverter sc = StringConverter.LoadFromFile(constants.PathToLanguage(entries.ContainsKey("LANG") ? entries["LANG"] : constants.DefaultLanguage));
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
					Console.WriteLine("Application data appears to be invalid and gets discarded");
					appactive = false;
				}
			}

			return new GuildData(guild.Id, public_channel, notification_channel, appactive, application_channel, invite, deadline, sc, constants);
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
