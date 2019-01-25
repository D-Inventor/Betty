using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.IO;

using Discord;

using TimeZoneConverter;

namespace Betty
{
	public class Constants
	{
		private static readonly string languagepath = Path.Combine("lang", "{0}.lang");
		private static readonly string configpath = Path.Combine("config", "Config.conf");
		private static readonly string guildspath = Path.Combine("guilds", "{0}.conf");

		public string PathToAppointments()		  => Path.Combine(PathToRoot(), "appointments.txt");
		public string PathToConfig()			  => Path.Combine(PathToRoot(), configpath);
		public string PathToData()				  => Path.Combine(PathToRoot(), "data");
		public string PathToGuild(ulong id)		  => Path.Combine(PathToRoot(), string.Format(guildspath, id));
		public string PathToGuilds()			  => Path.Combine(PathToRoot(), "guilds");
		public string PathToLanguage(string name) => Path.Combine(PathToRoot(), string.Format(languagepath, name));
		public string PathToLogs()				  => Path.Combine(PathToRoot(), "logs");
		public string PathToRoot()                => Directory.GetCurrentDirectory();

		public readonly string DefaultLanguage = "Assistant";

		public readonly TimeSpan[] EventNotifications = new TimeSpan[] { new TimeSpan(2, 0, 0), new TimeSpan(0, 30, 0) };
		public readonly TimeSpan[] ApplicationNotifications = new TimeSpan[] { new TimeSpan(5, 0, 0), new TimeSpan(0, 30, 0) };

		public GuildPermissions RolePermissions { get; } = new GuildPermissions(changeNickname: true, useVoiceActivation: true);

		public int MaxLogSize { get; } = 20 * 1024;
		public int MaxLogs { get; } = 2;
	}
}
