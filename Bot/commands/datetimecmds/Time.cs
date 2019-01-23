using System;
using System.Threading.Tasks;

using Discord.Commands;

namespace Betty.commands
{
	public partial class DateConvert
	{
		[Command("time"), Summary("This command takes a time and displays a table which converts that time to all the timezones")]
		public async Task ConvertTime([Remainder]string input = null)
		{
			// indicate that the bot is working on the command
			await Context.Channel.TriggerTypingAsync();
			StringConverter language = settings.GetLanguage(Context.Guild);

			// get the desired timezone
			TimeZoneInfo sourcetz = null;
			foreach (var tz in Context.Message.MentionedRoles)
			{
				if (datetimeutils.IsTimezone(tz.Name))
				{
					sourcetz = datetimeutils.IDToTimezone(tz.Name);
					break;
				}
			}

			if (sourcetz == null) sourcetz = datetimeutils.UserToTimezone(Context.User);
			if (sourcetz == null)
			{
				await Context.Channel.SendMessageAsync(language.GetString("command.time.notimezone"));
				return;
			}

			// get the desired time
			DateTime time = TimeZoneInfo.ConvertTime(DateTime.Now, sourcetz);
			if (input != null)
			{
				TimeSpan? ts = datetimeutils.StringToTime(input, true);
				if (!ts.HasValue)
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.time.error"));
					return;
				}
				time = new DateTime(time.Year, time.Month, time.Day, 0, 0, 0, time.Kind) + ts.Value;
			}

			string result = datetimeutils.TimetableToString(datetimeutils.LocalTimeToTimetable(time, sourcetz, Context.Guild));
			await Context.Channel.SendMessageAsync(language.GetString("present"));

			await Context.Channel.TriggerTypingAsync();
			await Context.Channel.SendMessageAsync(result);
		}
	}
}
