using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord.Commands;

namespace Betty.commands
{
	public partial class DateConvert
	{
		
		[Command("plan"), Summary("This command takes a time and displays a table which converts that time to all the timezones")]
		public async Task Plan([Remainder]string input = null)
		{
			await Context.Channel.TriggerTypingAsync();

			var language = settings.GetLanguage(Context.Guild);

			//make sure that user has a timezone assigned
			TimeZoneInfo timezone = services.GetService<DateTimeUtils>().UserToTimezone(Context.User);
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

			// make sure that a valid datetime has been provided
			DateTime? date;
			string title;
			services.GetService<DateTimeUtils>().StringToAppointment(input, out date, out title);
			if (!date.HasValue)
			{
				await Context.Channel.SendMessageAsync(language.GetString("command.plan.error"));
				return;
			}

			var dateutc = TimeZoneInfo.ConvertTimeToUtc(date.Value, timezone);
			TimeSpan ts = dateutc - DateTime.UtcNow;

			if(ts < TimeSpan.Zero)
			{
				await Context.Channel.SendMessageAsync(language.GetString("command.plan.past"));
				return;
			}

			services.GetService<Agenda>().Plan(Context.Guild, title, dateutc);

			await Context.Channel.SendMessageAsync(language.GetString("command.plan.success", new SentenceContext()
																								.Add("date", $"{dateutc:dd MMMM} at {dateutc:hh:mm tt} UTC")
																								.Add("title", title)));
		}
	}
}
