using System;
using System.Threading.Tasks;
using Betty.utilities;
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
			StringConverter language = statecollection.GetLanguage(Context.Guild);

			// get the desired timezone
			TimeZoneInfo sourcetz = null;
			foreach (var tz in Context.Message.MentionedRoles)
			{
				if (DateTimeMethods.IsTimezone(tz.Name))
				{
					sourcetz = DateTimeMethods.IDToTimezone(tz.Name);
					break;
				}
			}

			if (sourcetz == null) sourcetz = DateTimeMethods.UserToTimezone(Context.User);
			if (sourcetz == null)
			{
				await Context.Channel.SendMessageAsync(language.GetString("command.time.notimezone"));
				return;
			}

			// get the desired time
			DateTime time = TimeZoneInfo.ConvertTime(DateTime.Now, sourcetz);
			if (input != null)
			{
				TimeSpan? ts = DateTimeMethods.StringToTime(input, true);
				if (!ts.HasValue)
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.time.error"));
					return;
				}
				time = new DateTime(time.Year, time.Month, time.Day, 0, 0, 0, time.Kind) + ts.Value;
			}

			string result = DateTimeMethods.TimetableToString(DateTimeMethods.LocalTimeToTimetable(time, sourcetz, Context.Guild));
			await Context.Channel.SendMessageAsync(language.GetString("present"));

			await Context.Channel.TriggerTypingAsync();
			await Context.Channel.SendMessageAsync(result);
		}
	}
}
