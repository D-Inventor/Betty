using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using Discord;
using Discord.WebSocket;

using GuildCollection = System.Collections.Generic.Dictionary<ulong, Betty.GuildData>;

namespace Betty
{
	public partial class Settings
	{
		GuildCollection guildCollection;

		private void LoadGuild(SocketGuild guild)
		{
			// check if the directory is defined
			string guilddir = constants.PathToGuilds();
			if (!Directory.Exists(guilddir)) Directory.CreateDirectory(guilddir);

			GuildData guilddata = GuildData.FromFile(guild, services);

			// load the guild from a file
			guildCollection.Add(guild.Id, guilddata);
		}

		public GuildData GetGuildData(SocketGuild guild)
		{
			// load guild data from filesystem if not already loaded.
			GuildData result;
			lock (guildCollection)
			{
				if (!guildCollection.ContainsKey(guild.Id))
				{
					LoadGuild(guild);
				}
				result = guildCollection[guild.Id];
			}

			// return appropriate guild.
			return result;
		}

		public SocketTextChannel GetPublicChannel(SocketGuild guild)
		{
			// return public channel of given guild.
			ulong? pc = GetGuildData(guild).PublicChannel;
			return pc == null ? null : guild.GetTextChannel(pc.Value);
		}

		public void SetPublicChannel(SocketGuild guild, ulong? channel)
		{
			// store given channel ID for given guild.
			GetGuildData(guild).PublicChannel = channel;
		}

		public SocketTextChannel GetNotificationChannel(SocketGuild guild)
		{
			// return notification channel for given guild.
			ulong? nc = GetGuildData(guild).NotificationChannel;
			return nc == null ? null : guild.GetTextChannel(nc.Value);
		}

		public void SetNotificationChannel(SocketGuild guild, ulong? channel)
		{
			// store given channel ID for given guild.
			GetGuildData(guild).NotificationChannel = channel;
		}

		public SocketTextChannel GetNotificationElsePublic(SocketGuild guild)
		{
			// return notification channel if present, or public channel otherwise.
			return GetNotificationChannel(guild) ?? GetPublicChannel(guild);
		}

		public StringConverter GetLanguage(SocketGuild guild)
		{
			// return the language for given guild.
			return GetGuildData(guild).Language;
		}

		public bool GetApplicationActive(SocketGuild guild)
		{
			// return whether or not applications are active for given guild.
			return GetGuildData(guild).AppActive;
		}

		public SocketTextChannel GetApplicationChannel(SocketGuild guild)
		{
			var guilddata = GetGuildData(guild);
			if (!guilddata.AppActive) return null;

			ulong? ac = guilddata.ApplicationChannel;
			return ac.HasValue ? guild.GetTextChannel(ac.Value) : null;
		}

		public async Task<IInviteMetadata> GetApplicationInvite(SocketGuild guild)
		{
			string invite = GetGuildData(guild).Invite;

			var channel = GetApplicationChannel(guild);
			if(channel == null)
			{
				logger.Log(new LogMessage(LogSeverity.Warning, "Settings", $"Attempted to get invite on {guild.Name}, but failed: channel was null"));
				return null;
			}
			return (await channel.GetInvitesAsync()).FirstOrDefault(x => x.Id == invite);
		}

		public DateTime? GetApplicationDeadline(SocketGuild guild)
		{
			var guilddata = GetGuildData(guild);
			return guilddata.AppActive ? guilddata.Deadline : null;
		}

		public async Task<IInviteMetadata> StartApplication(SocketGuild guild, DateTime deadline)
		{
			var guilddata = GetGuildData(guild);
			if (guilddata.AppActive)
			{
				return null;
			}
			
			// find the category in which the public channel resides
			ulong? publicid = guilddata.PublicChannel;
			SocketCategoryChannel category = null;

			if (publicid.HasValue)
			{
				foreach (var c in guild.CategoryChannels)
				{
					if (c.Channels.Any(x => x.Id == publicid.Value))
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

			ulong appchannelid = appchannel.Id;

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

			guilddata.SetApplication(true, appchannelid, invite.Id, deadline);
			return invite;
		}
		
		public async Task StopApplication(SocketGuild guild)
		{
			var guilddata = GetGuildData(guild);

			// destroy applications channel
			ulong? appchannelid = guilddata.ApplicationChannel;
			if (appchannelid.HasValue)
			{
				var appchannel = guild.GetTextChannel(appchannelid.Value);
				try
				{
					await appchannel.DeleteAsync();
				}
				catch(Exception e)
				{
					logger.Log(new LogMessage(LogSeverity.Warning, "Settings", $"Attempted to delete application channel in '{guild.Name}', but failed: {e.Message}", e));
				}
			}

			// update guild data
			guilddata.SetApplication(false, null, null, null);
		}
	}
}
