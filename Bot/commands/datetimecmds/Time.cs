using System;
using System.Threading.Tasks;
using Betty.utilities;
using Discord.Commands;
using Discord;
using Betty.databases.guilds;
using Discord.WebSocket;

namespace Betty.commands
{
	public partial class DateConvert
	{
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

				// check if the user has specified a timezone and use it
				TimeZoneInfo sourcetz = null;
				foreach (var tz in Context.Message.MentionedRoles)
				{
					if (DateTimeMethods.IsTimezone(tz.Name))
					{
						sourcetz = DateTimeMethods.IDToTimezone(tz.Name);
						break;
					}
				}

				// if no timezone was specified, use the user's own timezone
				if (sourcetz == null) sourcetz = DateTimeMethods.UserToTimezone(Context.User);

				// make sure that the user does indeed have a timezone
				if (sourcetz == null)
				{
					await Context.Channel.SendMessageAsync(language.GetString("command.time.notimezone"));
					return;
				}

				// get the desired time
				DateTime time = TimeZoneInfo.ConvertTime(DateTime.Now, sourcetz);
				if (input != null)
				{
					// check if the provided time is in the correct format
					TimeSpan? ts = DateTimeMethods.StringToTime(input, true);
					if (!ts.HasValue)
					{
						// signal error if not
						await Context.Channel.SendMessageAsync(language.GetString("command.time.error"));
						return;
					}
					time = new DateTime(time.Year, time.Month, time.Day, 0, 0, 0, time.Kind) + ts.Value;
				}

				// construct a timetable and present it to the user
				string result = DateTimeMethods.TimetableToString(DateTimeMethods.LocalTimeToTimetable(time, sourcetz, Context.Guild));
				await Context.Channel.SendMessageAsync(language.GetString("present"));

				await Context.Channel.TriggerTypingAsync();
				await Context.Channel.SendMessageAsync(result);
			}
		}
	}
}
