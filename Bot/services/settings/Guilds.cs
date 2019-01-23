using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Microsoft.Extensions.DependencyInjection;

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

			GuildData guilddata = GuildData.FromFile(guild, constants);

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

		public ulong? GetPublicChannel(SocketGuild guild)
		{
			// return public channel of given guild.
			return GetGuildData(guild).PublicChannel;
		}

		public void SetPublicChannel(SocketGuild guild, ulong? channel)
		{
			// store given channel ID for given guild.
			GetGuildData(guild).PublicChannel = channel;
		}

		public ulong? GetNotificationChannel(SocketGuild guild)
		{
			// return notification channel for given guild.
			return GetGuildData(guild).NotificationChannel;
		}

		public void SetNotificationChannel(SocketGuild guild, ulong? channel)
		{
			// store given channel ID for given guild.
			GetGuildData(guild).NotificationChannel = channel;
		}

		public ulong? GetNotificationElsePublic(SocketGuild guild)
		{
			// return notification channel if present, or public channel otherwise.
			GuildData guildData = GetGuildData(guild);
			return guildData.NotificationChannel.HasValue ? guildData.NotificationChannel : guildData.PublicChannel;
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

		public ulong? GetApplicationChannel(SocketGuild guild)
		{
			var guilddata = GetGuildData(guild);
			return guilddata.AppActive ? guilddata.ApplicationChannel : null;
		}

		public async Task<IInviteMetadata> GetApplicationInvite(SocketGuild guild)
		{
			string invite = GetGuildData(guild).Invite;
			ulong? channelid = GetApplicationChannel(guild);

			if (!channelid.HasValue) return null;

			var channel = guild.GetTextChannel(channelid.Value);
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

			// create an application channel under given category
			ITextChannel appchannel = await guild.CreateTextChannelAsync("applications", x =>
			{
				if(category != null) x.CategoryId = category.Id;
				x.Topic = "If you're interested in our community: Write an application here!";
			});

			ulong appchannelid = appchannel.Id;
			
			// create an invite link to the public channel or the application channel
			IInviteMetadata invite = await appchannel.CreateInviteAsync(maxAge: null);

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
				await appchannel.DeleteAsync();
			}

			// update guild data
			guilddata.SetApplication(false, null, null, null);
		}
	}
}
