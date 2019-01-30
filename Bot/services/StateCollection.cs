using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using Discord;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

using GuildCollection = System.Collections.Generic.Dictionary<ulong, Betty.GuildState>;
using Betty.utilities;
using Betty.databases.guilds;
using System.Collections.Generic;

namespace Betty
{
	public class StateCollection
	{
		GuildCollection guildCollection;
		GuildDB database;
		Logger logger;
		Constants constants;

		public StateCollection(IServiceProvider services)
		{
			guildCollection = new GuildCollection();
			database = services.GetService<GuildDB>();
			logger = services.GetService<Logger>();
			constants = services.GetService<Constants>();
		}

		public GuildTB GetGuildEntry(SocketGuild guild)
		{
			//query the database
			var dbresult = from g in database.Guilds
						   where g.GuildID == guild.Id
						   select g;

			GuildTB result;

			// if there was no entry for given guild, create a default new one
			if (!dbresult.Any())
			{
				logger.Log(new LogMessage(LogSeverity.Info, "State", $"There is no data available yet for {guild.Name}, create new data"));
				result = CreateDefaultGuild(guild);
			}

			// otherwise read the entry from the database
			else
			{
				result = dbresult.First();
			}

			return result;
		}

		public SocketTextChannel GetPublicChannel(SocketGuild guild)
		{
			// get the data from the database
			GuildTB dbresult = GetGuildEntry(guild);
			if (!dbresult.Public.HasValue) return null;

			try
			{
				// try to get the channel from discord
				return guild.GetTextChannel(dbresult.Public.Value);
			}
			catch(Exception e)
			{
				// log failure, set channel to null and return null
				logger.Log(new LogMessage(LogSeverity.Warning, "State", $"Attempted to get public text channel from '{guild.Name}', but failed: {e.Message}", e));
				dbresult.Public = null;
				database.Guilds.Update(dbresult);
				database.SaveChanges();
				return null;
			}
		}

		public void SetPublicChannel(SocketGuild guild, ulong? channel)
		{
			// get the data from the database
			GuildTB dbresult = GetGuildEntry(guild);

			// change and push back
			dbresult.Public = channel;
			database.Guilds.Update(dbresult);
			database.SaveChanges();
		}

		public SocketTextChannel GetNotificationChannel(SocketGuild guild)
		{
			// get the data from the database
			GuildTB dbresult = GetGuildEntry(guild);
			if (!dbresult.Notification.HasValue) return null;

			try
			{
				// try to get the channel from discord
				return guild.GetTextChannel(dbresult.Notification.Value);
			}
			catch (Exception e)
			{
				// log failure, set channel to null and return null
				logger.Log(new LogMessage(LogSeverity.Warning, "State", $"Attempted to get notification text channel from '{guild.Name}', but failed: {e.Message}", e));
				dbresult.Notification = null;
				database.Guilds.Update(dbresult);
				database.SaveChanges();
				return null;
			}
		}

		public void SetNotificationChannel(SocketGuild guild, ulong? channel)
		{
			// get the data from the database
			GuildTB dbresult = GetGuildEntry(guild);

			// change and push back
			dbresult.Notification = channel;
			database.Guilds.Update(dbresult);
			database.SaveChanges();
		}

		public SocketTextChannel GetNotificationElsePublic(SocketGuild guild)
		{
			// return notification channel if present, or public channel otherwise.
			return GetNotificationChannel(guild) ?? GetPublicChannel(guild);
		}

		public StringConverter GetLanguage(SocketGuild guild)
		{
			// make sure that the language is loaded.
			if (!guildCollection.ContainsKey(guild.Id))
			{
				GuildTB dbresult = GetGuildEntry(guild);
				StringConverter language = StringConverter.LoadFromFile(constants.PathToLanguage(dbresult.Language), logger);
				guildCollection.Add(guild.Id, new GuildState
				{
					Language = language,
				});
			}

			// return the language for given guild.
			return guildCollection[guild.Id].Language;
		}

		public bool GetApplicationActive(SocketGuild guild)
		{
			// return whether or not applications are active for given guild.
			GuildTB dbresult = GetGuildEntry(guild);

			return dbresult.Application == null;
		}

		public SocketTextChannel GetApplicationChannel(SocketGuild guild)
		{
			// find the application in the database
			var dbresult = from app in database.Applications
						   where app.GuildID == guild.Id
						   select app.Channel;

			// return null if there is no application currently
			if (!dbresult.Any()) return null;

			// attempt to get the text channel with given id
			ulong channelid = dbresult.First();

			try
			{
				return guild.GetTextChannel(channelid);
			}
			catch(Exception e)
			{
				logger.Log(new LogMessage(LogSeverity.Warning, "State", $"Attempted to get application text channel from '{guild.Name}', but failed: {e.Message}", e));
				return null;
			}
		}

		public async Task<IInviteMetadata> GetApplicationInvite(SocketGuild guild)
		{
			// find the invite id in the database
			var dbresult = from app in database.Applications
						   where app.GuildID == guild.Id
						   select app;

			// if there's no application, return null
			if (!dbresult.Any()) return null;

			ApplicationTB entry = dbresult.First();
			string invite = entry.InviteID;

			// try to find the invite in the application channel
			try
			{
				SocketTextChannel channel = guild.GetTextChannel(entry.Channel);
				return (await channel.GetInvitesAsync()).FirstOrDefault(x => x.Id == invite);
			}
			catch(Exception e)
			{
				logger.Log(new LogMessage(LogSeverity.Warning, "State", $"Attempted to get invite on {guild.Name}, but failed: Couldn't get text channel: {e.Message}", e));
				return null;
			}
		}

		public DateTime? GetApplicationDeadline(SocketGuild guild)
		{
			// query the database for the deadline
			var dbresult = from app in database.Applications
						   where app.GuildID == guild.Id
						   select app.Deadline;

			// return the date if present or null otherwise
			if (!dbresult.Any()) return null;
			return dbresult.First();
		}

		public async Task<IInviteMetadata> StartApplication(SocketGuild guild, DateTime deadline)
		{
			// check if there are already applications going
			var dbresultapp = from app in database.Applications
						   where app.GuildID == guild.Id
						   select app;

			if (dbresultapp.Any()) return null;

			// find the category in which the public channel resides
			GuildTB gtb = GetGuildEntry(guild);
			SocketCategoryChannel category = null;

			if (gtb.Public.HasValue)
			{
				foreach (var c in guild.CategoryChannels)
				{
					if (c.Channels.Any(x => x.Id == gtb.Public.Value))
					{
						category = c;
						break;
					}
				}
			}

			ITextChannel appchannel;
			try
			{
				// create an application channel under given category
				appchannel = await guild.CreateTextChannelAsync("applications", x =>
				{
					if (category != null) x.CategoryId = category.Id;
					x.Topic = "If you're interested in our community: Write an application here!";
				});
			}
			catch(Exception e)
			{
				logger.Log(new LogMessage(LogSeverity.Warning, "Settings", $"Attempted to create application channel in '{guild.Name}', but failed: {e.Message}", e));
				return null;
			}

			IInviteMetadata invite;
			try
			{
				// create an invite link to the public channel or the application channel
				invite = await appchannel.CreateInviteAsync(maxAge: null);
			}
			catch(Exception e)
			{
				logger.Log(new LogMessage(LogSeverity.Warning, "Settings", $"Attempted to create invite in '{guild.Name}', but failed: {e.Message}", e));
				return null;
			}

			// add new entry to the database
			await database.Applications.AddAsync(new ApplicationTB
			{
				Channel = appchannel.Id,
				Deadline = deadline,
				Guild = gtb,
				GuildID = guild.Id
			});
			await database.SaveChangesAsync();

			return invite;
		}
		
		public async Task StopApplication(SocketGuild guild)
		{
			// check if there were applications in the first place
			var dbresult = from app in database.Applications
						   where app.GuildID == guild.Id
						   select app;

			if (!dbresult.Any()) return;
			ApplicationTB atb = dbresult.First();

			// destroy applications channel
			try
			{
				var appchannel = guild.GetTextChannel(atb.Channel);
				await appchannel.DeleteAsync();
			}
			catch(Exception e)
			{
				logger.Log(new LogMessage(LogSeverity.Warning, "Settings", $"Attempted to delete application channel in '{guild.Name}', but failed: {e.Message}", e));
			}

			database.Applications.Remove(atb);
			await database.SaveChangesAsync();
		}

		public async Task<IInviteMetadata> GetInviteByID(SocketTextChannel channel, string inviteID)
		{
			IReadOnlyCollection<IInviteMetadata> invites;
			try
			{
				// try to get invites for given channel
				invites = await channel.GetInvitesAsync();
			}
			catch(Exception e)
			{
				// log failure and return nothing
				logger.Log(new LogMessage(LogSeverity.Warning, "State", $"Attempted to load invites from {channel.Name}, but failed: {e.Message}"));
				return null;
			}

			// try to find given invite in list
			return invites.FirstOrDefault(x => x.Id == inviteID);
		}

		private GuildTB CreateDefaultGuild(SocketGuild guild)
		{
			GuildTB result = new GuildTB
			{
				GuildID = guild.Id,
				Name = guild.Name,
				Language = "Assistant",
			};

			// add default entry to database
			database.Guilds.Add(result);
			database.SaveChanges();

			return result;
		}
	}
}
