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
		IServiceProvider services;
		Constants constants;
		Logger logger;

		public Settings(IServiceProvider services)
		{
			// take reference to relevant services and create a new collection for guilds
			this.services = services;
			constants = services.GetRequiredService<Constants>();
			logger = services.GetRequiredService<Logger>();
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
				logger.Log(new LogMessage(LogSeverity.Error, "Settings", "Couldn't find configuration file"));
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
						logger.Log(new LogMessage(LogSeverity.Warning, "Settings", $"Couldn't interpret option: {line}"));
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
					logger.Log(new LogMessage(LogSeverity.Warning, "Settings", $"Unknown log level: {loglevelstring}"));
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
