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
		Logger logger;

		public Settings(IServiceProvider services)
		{
			// take reference to relevant services and create a new collection for guilds
			constants = services.GetRequiredService<Constants>();
			logger = services.GetRequiredService<Logger>();
		}

		public bool Init()
		{
			// initialise settings with configurations from the filesystem
			string configpath = constants.PathToConfig();
			if (!GetConfiguration(configpath, out Configurations config))
				return false;

			return SetEnvironment(config);
		}

		private bool GetConfiguration(string path, out Configurations configs)
		{
			// read all global configurations from the filesystem
			configs = new Configurations();

			// make sure that config file exists
			if (!File.Exists(path))
			{
				// write to log if no settings exist and create a template settings file
				logger.Log(new LogMessage(LogSeverity.Error, "Settings", $"Looked for configuration file at '{path}', but couldn't find it."));

				try
				{
					using (StreamWriter sw = new StreamWriter(path))
					{
						sw.WriteLine("TOKEN:your_private_token");
						sw.WriteLine("LOGLEVEL:WARNING");
					}
				}
				catch (Exception e)
				{
					logger.Log(new LogMessage(LogSeverity.Error, "Settings", $"Attempted to make template settings at '{path}', but failed: {e.Message}", e));
				}

				return false;
			}

			// open the file
			try
			{
				using (var file = new StreamReader(path))
				{
					string line;
					while ((line = file.ReadLine()) != null)
					{
						// for each line, extract the key and value and store in the dictionary
						int separator = line.IndexOf(':');
						if (separator < 0)
						{
							logger.Log(new LogMessage(LogSeverity.Warning, "Settings", $"Read line '{line}', but could not interpret."));
							continue;
						}
						string key = line.Substring(0, separator);
						separator++;
						string value = line.Substring(separator, line.Length - separator);
						configs.Add(key, value);
					}
				}
			}
			catch(Exception e)
			{
				logger.Log(new LogMessage(LogSeverity.Error, "Settings", $"Attempted to read configuration file, but failed: {e.Message}", e));
				return false;
			}

			return true;
		}

		private bool SetEnvironment(Configurations config)
		{
			// take the configurations and assign variables accordingly

			// read loglevel
			if (!config.ContainsKey("LOGLEVEL"))
			{
				logger.Log(new LogMessage(LogSeverity.Warning, "Settings", $"Missing configuration for 'LOGLEVEL'."));
				LogLevel = LogSeverity.Warning;
			}
			else
			{
				string loglevelstring = config["LOGLEVEL"];
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
						logger.Log(new LogMessage(LogSeverity.Warning, "Settings", $"The value for 'LOGLEVEL' is missing or incorrect: '{loglevelstring}'"));
						LogLevel = LogSeverity.Warning;
						break;
				}
			}

			// read token
			if (!config.ContainsKey("TOKEN"))
			{
				logger.Log(new LogMessage(LogSeverity.Error, "Settings", $"Missing configuration for 'TOKEN'."));
				return false;
			}
			Token = config["TOKEN"];
			return true;
		}

		public string Token { get; private set; }
		public LogSeverity LogLevel { get; private set; }
	}
}
