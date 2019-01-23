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
		private static readonly string languagepath = @"/lang/{0}.lang";
		private static readonly string configpath = @"/config/Config.conf";
		private static readonly string guildspath = @"/guilds/{0}.conf";

		public string PathToAppointments()		  => PathToData() + "/appointments.txt";
		public string PathToConfig()			  => PathToRoot() + configpath;
		public string PathToData()				  => PathToRoot() + "/data";
		public string PathToGuild(ulong id)		  => PathToData() + string.Format(guildspath, id);
		public string PathToGuilds()			  => PathToData() + "/guilds";
		public string PathToLanguage(string name) => PathToRoot() + string.Format(languagepath, name);
		public string PathToRoot()                => Directory.GetCurrentDirectory();

		public readonly string DefaultLanguage = "Assistant";

		public readonly TimeSpan[] EventNotifications = new TimeSpan[] { new TimeSpan(2, 0, 0), new TimeSpan(0, 30, 0) };
		public readonly TimeSpan[] ApplicationNotifications = new TimeSpan[] { new TimeSpan(5, 0, 0), new TimeSpan(0, 30, 0) };

		public GuildPermissions RolePermissions { get; } = new GuildPermissions(changeNickname: true, useVoiceActivation: true);
	}
}
