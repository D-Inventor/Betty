using System;
using System.IO;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Discord;

using Configurations = System.Collections.Generic.Dictionary<string, string>;

namespace Betty
{
	public partial class Settings
	{
		Constants constants;

		public Settings(IServiceProvider services)
		{
			// take reference to relevant services and create a new collection for guilds
			constants = services.GetRequiredService<Constants>();
			guildCollection = new Dictionary<ulong, GuildData>();
		}

		public void Init()
		{
			// initialise settings with configurations from the filesystem
			string configpath = constants.PathToConfig();
			var config = GetConfiguration(configpath);
			SetEnvironment(config);
		}

		private Configurations GetConfiguration(string path)
		{
			// read all global configurations from the filesystem
			Configurations configs = new Configurations();

			// make sure that config file exists
			if (!File.Exists(path))
			{
				Console.WriteLine($"Could not find config file.");
				return configs;
			}

			// open the file
			using (var file = new StreamReader(path))
			{
				string line;
				while ((line = file.ReadLine()) != null)
				{
					// for each line, extract the key and value and store in the dictionary
					int separator = line.IndexOf(':');
					if(separator < 0)
					{
						Console.WriteLine($"BAD OPTION: {line}");
						continue;
					}
					string key = line.Substring(0, separator);
					separator++;
					string value = line.Substring(separator, line.Length - separator);
					configs.Add(key, value);
				}
			}

			return configs;
		}

		private void SetEnvironment(Configurations config)
		{
			// take the configurations and assign variables accordingly

			// read loglevel
			string loglevelstring = config.ContainsKey("LOGLEVEL") ? config["LOGLEVEL"] : "Debug";
			switch (loglevelstring.ToUpper())
			{
				case "CRITICAL":
					LogLevel = LogSeverity.Critical;
					break;
				case "DEBUG":
					LogLevel = LogSeverity.Debug;
					break;
				case "ERROR":
					LogLevel = LogSeverity.Error;
					break;
				case "INFO":
					LogLevel = LogSeverity.Info;
					break;
				case "VERBOSE":
					LogLevel = LogSeverity.Verbose;
					break;
				case "WARNING":
					LogLevel = LogSeverity.Warning;
					break;
				default:
					Console.WriteLine($"Unknown loglevel: {loglevelstring}");
					LogLevel = LogSeverity.Debug;
					break;
			}

			// read token
			Token = config.ContainsKey("TOKEN") ? config["TOKEN"] : "";
		}

		public string Token { get; private set; }

		public LogSeverity LogLevel { get; private set; }
	}
}
