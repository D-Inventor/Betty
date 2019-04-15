using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord.Commands;

using Betty.utilities;
using Betty.databases.guilds;
using Discord.WebSocket;

namespace Betty
{
	class Analysis
	{
		StateCollection statecollection;

		public Analysis(IServiceProvider services)
		{
			statecollection = services.GetRequiredService<StateCollection>();
		}

		public async Task AnalyseTime(SocketCommandContext Context)
		{
			using (var database = new GuildDB())
			{
				// first make sure that the user has the correct permissions
				if (!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Member, database)) return;

				DateTime now = DateTime.UtcNow;

				// check if the text contains a time indication
				TimeSpan? FoundTime = DateTimeMethods.StringToTime(Context.Message.Content);
				if (!FoundTime.HasValue) return;

				// check if the user has a timezone applied
				TimeZoneInfo tz = DateTimeMethods.UserToTimezone(Context.User);
				if (tz == null) return;

				// indicate that the bot is working on the answer
				await Context.Channel.TriggerTypingAsync();

				var language = statecollection.GetLanguage(Context.Guild, database);

				// find the desired date time in the local time
				DateTime dt = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0) + FoundTime.Value;

				// print the table to the chat
				string result = DateTimeMethods.TimetableToString(DateTimeMethods.LocalTimeToTimetable(dt, tz, Context.Guild));
				await Context.Channel.SendMessageAsync(language.GetString("scan.time.found"));

				await Context.Channel.TriggerTypingAsync();
				await Context.Channel.SendMessageAsync(result);
				return;
			}
		}
	}
}
