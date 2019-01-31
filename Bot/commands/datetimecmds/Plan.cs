using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;

using Betty.utilities;

namespace Betty.commands
{
	public partial class DateConvert
	{
		
		[Command("plan"), Summary("This command takes a time and displays a table which converts that time to all the timezones")]
		public async Task Plan([Remainder]string input = null)
		{
			// log command execution
			CommandMethods.LogExecution(logger, "plan", Context);

			// indicate that the bot is working on the command
			await Context.Channel.TriggerTypingAsync();

			var language = statecollection.GetLanguage(Context.Guild);

			//make sure that user has a timezone assigned
			TimeZoneInfo timezone = DateTimeMethods.UserToTimezone(Context.User);
			if (timezone == null)
			{
				await Context.Channel.SendMessageAsync(language.GetString("command.time.notimezone"));
				return;
			}

			// make sure that the command is not empty
			if(input == null)
			{
				await Context.Channel.SendMessageAsync(language.GetString("command.plan.empty"));
				return;
			}

			// make sure that a valid input has been provided
			DateTimeMethods.StringToAppointment(input, out DateTime? date, out string title);
			if (!date.HasValue)
			{
				await Context.Channel.SendMessageAsync(language.GetString("command.plan.error"));
				return;
			}

			// make sure that the provided date is in the future
			var dateutc = TimeZoneInfo.ConvertTimeToUtc(date.Value, timezone);
			if(dateutc < DateTime.UtcNow)
			{
				await Context.Channel.SendMessageAsync(language.GetString("command.plan.past"));
				return;
			}

			// put the plan in the agenda
			await agenda.Plan(Context.Guild, title, dateutc, notifications: constants.EventNotifications);

			// return success to the user
			await Context.Channel.SendMessageAsync(language.GetString("command.plan.success", new SentenceContext()
																								.Add("date", $"{dateutc:dd MMMM} at {dateutc:hh:mm tt} UTC")
																								.Add("title", title)));
		}
	}
}
