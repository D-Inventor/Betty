using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using Discord;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

using GuildCollection = System.Collections.Concurrent.ConcurrentDictionary<ulong, Betty.GuildState>;
using Betty.utilities;
using Betty.databases.guilds;
using System.Collections.Generic;
using System.Threading;

namespace Betty
{
	public class StateCollection
	{
		#region fields
		GuildCollection guildCollection;
		Logger logger;
		Constants constants;
		Notifier notifier;
		#endregion

		public StateCollection(IServiceProvider services)
		{
			guildCollection = new GuildCollection();
			logger = services.GetService<Logger>();
			constants = services.GetService<Constants>();
			notifier = services.GetService<NotifierFactory>().Create(this);
		}

		public async Task RestoreApplication(SocketGuild guild, GuildDB database)
		{
			// check if this guild has applications
			ApplicationTB application = GetApplicationEntry(guild, database);
			if(application == null)
			{
				logger.Log(new LogMessage(LogSeverity.Info, "State", $"'{guild.Name}' currently has no application and will therefore not be restored"));
				return;
			}

			// make sure that there is not an already active notifier
			//  (This could happen, because GuildAvailable gets called when Betty loses connection with discord)
			CancellationTokenSource t = GetApplicationToken(guild);
			if(t != null)
			{
				if (!t.IsCancellationRequested)
				{
					// if it's still active, there is no need to restore the application
					logger.Log(new LogMessage(LogSeverity.Warning, "State", $"There is already a notifier for this application. Skipping the application"));
					return;
				}
				else
				{
					// if it's not active, then there is something wrong with the code, but then a new notifier can be created still
					logger.Log(new LogMessage(LogSeverity.Error, "State", $"Found a notifier, but cancellation was requested. This should never happen!!"));
				}
			}

			// create notifier
			SocketTextChannel channel = GetApplicationChannel(guild, database, application);
			IInviteMetadata invite = await GetApplicationInvite(guild, database, application, channel);
			var token = notifier.CreateWaiterTask(guild, channel, messages: DateTimeMethods.BuildMessageList(constants.ApplicationNotifications, application.Deadline, "Application selection"), action: async (db) =>
			{
				await ExpireAppLink(invite);
			});

			SetApplicationToken(guild, database, token);
		}

		#region getters
		public GuildTB GetGuildEntry(SocketGuild guild, GuildDB database)
		{
			// try to read the database for a guild entry
			GuildTB dbresult = (from g in database.Guilds
								where g.GuildId == guild.Id
								select g).FirstOrDefault();

			// if there was no entry, create a default entry
			if (dbresult == null)
			{
				// push to log and create new entry
				logger.Log(new LogMessage(LogSeverity.Info, "State", $"No database entry was found for '{guild.Name}'. Creating a new one"));
				dbresult = CreateDefaultGuild(guild, database);
			}

			return dbresult;
		}

		public SocketTextChannel GetPublicChannel(SocketGuild guild, GuildDB database, GuildTB dbentry = null)
		{
			// try to use the database entry if present
			if (dbentry == null) dbentry = GetGuildEntry(guild, database);

			// get the channel
			return GetChannel(guild, dbentry.Public);
		}

		public SocketTextChannel GetNotificationChannel(SocketGuild guild, GuildDB database, GuildTB dbentry = null)
		{
			// try to use the database entry if present
			if (dbentry == null) dbentry = GetGuildEntry(guild, database);

			// get the channel
			return GetChannel(guild, dbentry.Notification);
		}

		public SocketTextChannel GetNotificationElsePublicChannel(SocketGuild guild, GuildDB database, GuildTB dbentry = null)
		{
			if (dbentry == null) dbentry = GetGuildEntry(guild, database);
			return GetNotificationChannel(guild, database, dbentry) ?? GetPublicChannel(guild, database, dbentry);
		}

		public StringConverter GetLanguage(SocketGuild guild, GuildDB database, GuildTB dbentry = null)
		{
			// make sure that the dbentry is not null
			if (dbentry == null) dbentry = GetGuildEntry(guild, database);

			// make sure that there is a state for this guild
			if (!guildCollection.ContainsKey(guild.Id))
			{
				logger.Log(new LogMessage(LogSeverity.Info, "State", $"No state was found for '{guild.Name}'. Creating a new one with the database"));
				return CreateDefaultState(guild, dbentry).Language;
			}

			// return the language
			return guildCollection[guild.Id].Language;
		}

		private SocketTextChannel GetChannel(SocketGuild guild, ulong? channelid)
		{
			// check if a public channel was assigned
			if (channelid == null) return null;

			try
			{
				// try to get the channel from discord
				return guild.GetTextChannel(channelid.Value);
			}
			catch (Exception e)
			{
				// log failure
				logger.Log(new LogMessage(LogSeverity.Warning, "State", $"Attempted to get text channel from '{guild.Name}', but failed: {e.Message}\n{e.StackTrace}"));
				return null;
			}
		}

		public ApplicationTB GetApplicationEntry(SocketGuild guild, GuildDB database)
		{
			return (from app in database.Applications
					where app.Guild.GuildId == guild.Id
					select app).FirstOrDefault();
		}

		private bool GetApplicationEntry(SocketGuild guild, GuildDB database, ref ApplicationTB application)
		{
			// make sure that there are indeed applications going
			if (application == null)
			{
				application = GetApplicationEntry(guild, database);
				if (application == null) return false;
			}

			return true;
		}

		public SocketTextChannel GetApplicationChannel(SocketGuild guild, GuildDB database, ApplicationTB application = null)
		{
			// make sure that there are indeed applications
			if (!GetApplicationEntry(guild, database, ref application)) return null;

			// return the channel
			return GetChannel(guild, application.Channel);
		}

		public async Task<IInviteMetadata> GetApplicationInvite(SocketGuild guild, GuildDB database, ApplicationTB application = null, SocketTextChannel channel = null)
		{
			// make sure that there are indeed applications
			if (!GetApplicationEntry(guild, database, ref application)) return null;

			// get the channel where the invite comes from
			if(channel == null)
			{
				channel = GetChannel(guild, application.Channel);
				if (channel == null) return null;
			}

			IEnumerable<IInviteMetadata> invites;
			try
			{
				// try to get the invites from discord
				invites = await channel.GetInvitesAsync();
			}
			catch(Exception e)
			{
				// log failure and return
				logger.Log(new LogMessage(LogSeverity.Warning, "State", $"Attempted to read invites from '{guild.Name}', but failed: {e.Message}\n{e.StackTrace}"));
				return null;
			}

			// return the invite
			return invites.FirstOrDefault(x => x.Id == application.InviteID);
		}

		public CancellationTokenSource GetApplicationToken(SocketGuild guild)
		{
			// if there's not state, then there's no application token
			if (!guildCollection.ContainsKey(guild.Id)) return null;

			// return the token
			return guildCollection[guild.Id].ApplicationToken;
		}
		#endregion

		#region setters
		public void SetGuildEntry(GuildTB dbentry, GuildDB database)
		{
			// check if the language is still up to date
			if (guildCollection.ContainsKey(dbentry.GuildId) && guildCollection[dbentry.GuildId].Language.Name != dbentry.Language)
			{
				guildCollection[dbentry.GuildId].Language = StringConverter.LoadFromFile(constants.PathToLanguage(dbentry.Language), logger);
			}

			// send changes to database
			database.Guilds.Update(dbentry);

			try
			{
				database.SaveChanges();
			}
			catch(Exception e)
			{
				// log failure
				logger.Log(new LogMessage(LogSeverity.Error, "State", $"Attempted to save guild changes to database, but failed: {e.Message}\n{e.StackTrace}"));
			}
		}

		public void SetApplicationToken(SocketGuild guild, GuildDB database, CancellationTokenSource token, GuildTB dbentry = null)
		{
			// make sure that dbentry is not null
			if (dbentry == null) dbentry = GetGuildEntry(guild, database);

			// make sure that there is a guild state
			if (!guildCollection.ContainsKey(guild.Id)) CreateDefaultState(guild, dbentry);

			// set the token
			guildCollection[guild.Id].ApplicationToken = token;
		}
		#endregion

		#region application
		public async Task<IInviteMetadata> StartApplication(SocketGuild guild, GuildDB database, DateTime deadline, GuildTB dbentry = null)
		{

			if(dbentry == null) dbentry = GetGuildEntry(guild, database);

			// create a channel
			var channel = await CreateApplicationChannel(guild, dbentry);
			if (channel == null) return null;

			// create an invite
			var invite = await CreateApplicationInvite(guild, channel, dbentry);
			if (invite == null) return null;

			// save to database
			if (!SaveApplicationToDatabase(dbentry, deadline, channel, invite, database)) return null;

			// set a notifier
			var token = notifier.CreateWaiterTask(guild, GetApplicationChannel(guild, database), messages: DateTimeMethods.BuildMessageList(constants.ApplicationNotifications, deadline, "Application selection"), action: async(db) =>
			{
				await ExpireAppLink(invite);
			});
			SetApplicationToken(guild, database, token, dbentry);

			return invite;
		}

		private async Task ExpireAppLink(IInviteMetadata invite)
		{
			try
			{
				// try to delete the invite
				await invite.DeleteAsync();
			}
			catch(Exception e)
			{
				// log failure
				logger.Log(new LogMessage(LogSeverity.Warning, "State", $"Attempted to delete invite, but failed: {e.Message}\n{e.StackTrace}"));
			}
		}

		private bool SaveApplicationToDatabase(GuildTB dbentry, DateTime deadline, ITextChannel channel, IInviteMetadata invite, GuildDB database)
		{
			// create entry
			ApplicationTB appdbentry = new ApplicationTB
			{
				Deadline = deadline,
				InviteID = invite.Id,
				Channel = channel.Id,
				Guild = dbentry,
			};
			database.Applications.Add(appdbentry);

			try
			{
				// try to save to database
				database.SaveChanges();
				return true;
			}
			catch (Exception e)
			{
				// log failure
				logger.Log(new LogMessage(LogSeverity.Error, "State", $"Attempted to save application to database, but failed: {e.Message}\n{e.StackTrace}"));
				return false;
			}
		}

		private async Task<ITextChannel> CreateApplicationChannel(SocketGuild guild, GuildTB dbentry)
		{
			logger.Log(new LogMessage(LogSeverity.Info, "State", $"Creating an application channel for {guild.Name}."));

			// find the correct category
			SocketCategoryChannel category = null;
			if(dbentry.Public != null)
			{
				foreach(var c in guild.CategoryChannels)
				{
					if(c.Channels.Any(x => x.Id == dbentry.Public))
					{
						category = c;
						break;
					}
				}
			}

			try
			{
				// try to create a text channel
				return await guild.CreateTextChannelAsync("Applications", (x) =>
				{
					x.Topic = "Are you interested in our community? Write your application here!";
					x.CategoryId = category?.Id;
				});
			}
			catch(Exception e)
			{
				// report failure
				logger.Log(new LogMessage(LogSeverity.Error, "State", $"Attempted to create a channel for '{guild.Name}', but failed: {e.Message}\n{e.StackTrace}"));
				return null;
			}
		}

		private async Task<IInviteMetadata> CreateApplicationInvite(SocketGuild guild, ITextChannel channel, GuildTB dbentry)
		{
			logger.Log(new LogMessage(LogSeverity.Info, "State", $"Creating an invite for {guild.Name}"));
			try
			{
				return await channel.CreateInviteAsync(null, null, false, false);
			}
			catch(Exception e)
			{
				logger.Log(new LogMessage(LogSeverity.Error, "State", $"Attempted to create invite for '{guild.Name}', but failed: {e.Message}\n{e.StackTrace}"));
				return null;
			}
		}

		public async Task<bool> StopApplication(SocketGuild guild, GuildDB database, ApplicationTB appentry)
		{
			// make sure that there is indeed a state with a token
			if (!guildCollection.ContainsKey(guild.Id)) return false;

			// cancel notifier
			logger.Log(new LogMessage(LogSeverity.Info, "State", $"Cancelling application notifier for '{guild.Name}'"));
			if (!CancelApplicationNotifier(guild)) return false;

			// delete the invite
			logger.Log(new LogMessage(LogSeverity.Info, "State", $"Deleting invite for '{guild.Name}'."));
			await ExpireAppLink(await GetApplicationInvite(guild, database, appentry));

			// delete the channel
			logger.Log(new LogMessage(LogSeverity.Info, "State", $"Deleting application channel for '{guild.Name}'"));
			await DeleteApplicationChannel(guild, appentry);

			// delete from the database
			RemoveApplicationFromDatabase(appentry, database);

			return true;
		}

		private bool RemoveApplicationFromDatabase(ApplicationTB appentry, GuildDB database)
		{
			database.Applications.Remove(appentry);

			try
			{
				//try to apply removal to database
				database.SaveChanges();
				return true;
			}
			catch(Exception e)
			{
				//log failure
				logger.Log(new LogMessage(LogSeverity.Warning, "State", $"Attempted to remove application from database, but failed: {e.Message}\n{e.StackTrace}"));
				return false;
			}
		}

		private bool CancelApplicationNotifier(SocketGuild guild)
		{
			// cancel the token
			var token = guildCollection[guild.Id].ApplicationToken;
			if (token == null)
			{
				logger.Log(new LogMessage(LogSeverity.Error, "State", $"Attempted to cancel notification, but failed: token was null."));
				return false;
			}
			token.Cancel();
			guildCollection[guild.Id].ApplicationToken = null;

			return true;
		}

		private async Task<bool> DeleteApplicationChannel(SocketGuild guild, ApplicationTB appentry)
		{
			var channel = GetChannel(guild, appentry.Channel);

			try
			{
				// try to delete the channel
				await channel.DeleteAsync();
				return true;
			}
			catch(Exception e)
			{
				// log failure
				logger.Log(new LogMessage(LogSeverity.Warning, "State", $"Attempted to delete application channel for '{guild.Name}', but failed: {e.Message}\n{e.StackTrace}"));
				return false;
			}
		}
		#endregion

		#region default creators
		private GuildTB CreateDefaultGuild(SocketGuild guild, GuildDB database)
		{
			GuildTB result = new GuildTB
			{
				GuildId = guild.Id,
				Name = guild.Name,
				Language = "Assistant",
			};

			// add default entry to database
			database.Guilds.Add(result);
			database.SaveChanges();

			return result;
		}

		private GuildState CreateDefaultState(SocketGuild guild, GuildTB dbentry)
		{
			StringConverter language = StringConverter.LoadFromFile(constants.PathToLanguage(dbentry.Language), logger);
			GuildState state = new GuildState
			{
				Language = language,
			};

			if(!guildCollection.TryAdd(guild.Id, state))
			{
				logger.Log(new LogMessage(LogSeverity.Error, "State", $"Attempted to add new state to state collection, but failed."));
			}

			return state;
		}
		#endregion
	}
}
