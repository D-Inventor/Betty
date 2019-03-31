using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Betty.utilities;
using Discord.Commands;
using Discord;
using Betty.databases.guilds;
using Discord.WebSocket;

namespace Betty.commands
{
	public partial class DateConvert
	{
		private static Regex timerx = new Regex(@"(?<time>\d{1,2}(:\d{2})?\s?(am|pm))(\s(?<locale>(utc|local)))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        [RequireContext(ContextType.Guild)]
        [Command("time"), Summary("This command takes a time and displays a table which converts that time to all the timezones")]
		public async Task ConvertTime([Remainder]string input = null)
		{
			using(var database = new GuildDB())
			{
				// log command execution
				CommandMethods.LogExecution(logger, "time", Context);

				// indicate that the bot is working on the command
				await Context.Channel.TriggerTypingAsync();

				StringConverter language = statecollection.GetLanguage(Context.Guild, database);

				// make sure that the user has the right permissions
				if (!PermissionHelper.UserHasPermission(Context.User as SocketGuildUser, PermissionHelper.Member, database))
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.nopermission"));
					return;
				}

				TimeZoneInfo sourcetz;
				DateTime time;

				if(input != null)
				{
					// parse the input
					Match m = timerx.Match(input);
					if (!m.Success)
					{
						await Context.Channel.SendMessageAsync(language.GetString("command.time.error"));
						return;
					}

					// find out which timezone is desired
					GroupCollection g = m.Groups;
					TimeLocale tl;
					if (g["locale"].Success)
					{
						if(!Enum.TryParse<TimeLocale>(g["locale"].Value, true, out tl))
						{
							await Context.Channel.SendMessageAsync(language.GetString("command.time.error"));
							return;
						}
					}
					else
					{
						tl = TimeLocale.local;
					}

					// find the desired timezone info
					if(tl == TimeLocale.local)
					{
						sourcetz = DateTimeMethods.UserToTimezone(Context.User);
						if(sourcetz == null)
						{
							await Context.Channel.SendMessageAsync(language.GetString("command.time.notimezone"));
							return;
						}
					}
					else
					{
						sourcetz = TimeZoneInfo.Utc;
					}

					// find the time
					time = TimeZoneInfo.ConvertTime(DateTime.Now, sourcetz);
					TimeSpan? ts = DateTimeMethods.StringToTime(g["time"].Value, true);
					if (!ts.HasValue)
					{
						// signal error if not
						await Context.Channel.SendMessageAsync(language.GetString("command.time.error"));
						return;
					}
					time = new DateTime(time.Year, time.Month, time.Day, 0, 0, 0, time.Kind) + ts.Value;
				}
				else
				{
					sourcetz = TimeZoneInfo.Utc;
					time = DateTime.UtcNow;
				}

				// construct a timetable and present it to the user
				string result = DateTimeMethods.TimetableToString(DateTimeMethods.LocalTimeToTimetable(time, sourcetz, Context.Guild));
				await Context.Channel.SendMessageAsync(language.GetString("present"));

				await Context.Channel.TriggerTypingAsync();
				await Context.Channel.SendMessageAsync(result);
			}
		}
	}

	public enum TimeLocale
	{
		utc, local
	}
}
