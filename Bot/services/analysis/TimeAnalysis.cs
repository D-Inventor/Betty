using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord.Commands;

namespace Betty
{
	class Analysis
	{
		DateTimeUtils datetimeutils;
		Settings settings;

		public Analysis(IServiceProvider services)
		{
			datetimeutils = services.GetRequiredService<DateTimeUtils>();
			settings = services.GetRequiredService<Settings>();
		}

		public async Task AnalyseTime(SocketCommandContext Context)
		{
			DateTime now = DateTime.UtcNow;

			// check if the text contains a time indication
			TimeSpan? FoundTime = datetimeutils.StringToTime(Context.Message.Content, false);
			if (!FoundTime.HasValue) return;

			// check if the user has a timezone applied
			TimeZoneInfo tz = datetimeutils.UserToTimezone(Context.User);
			if (tz == null) return;

			// indicate that the bot is working on the answer
			await Context.Channel.TriggerTypingAsync();

			var language = settings.GetLanguage(Context.Guild);

			// find the desired date time in the local time
			DateTime dt = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0) + FoundTime.Value;

			// print the table to the chat
			string result = datetimeutils.TimetableToString(datetimeutils.LocalTimeToTimetable(dt, tz, Context.Guild));
			await Context.Channel.SendMessageAsync(language.GetString("scan.time.found"));

			await Context.Channel.TriggerTypingAsync();
			await Context.Channel.SendMessageAsync(result);
			return;
		}
	}
}
