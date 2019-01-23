using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Discord.WebSocket;

using TimeZoneConverter;

namespace Betty
{
	class DateTimeUtils
	{
		// create a timetable string based on given timetable
		public string TimetableToString(IEnumerable<KeyValuePair<string, DateTime>> entries)
		{
			// find the largest key value
			int largest = Math.Max(entries.Select(kv => kv.Key.Length).Max(), 8);

			// build the header
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```");
			sb.AppendLine($"+{new string('-', largest + 2)}+{new string('-', 10)}+");
			sb.AppendLine($"| Timezone{new string(' ', largest - 8)} | Time{new string(' ', 4)} |");
			sb.AppendLine($"+{new string('-', largest + 2)}+{new string('-', 10)}+");

			// build the content
			foreach (var t in entries)
			{
				sb.AppendLine($"| {t.Key + new string(' ', largest - t.Key.Length)} | {t.Value:hh\\:mm\\ tt} |");
			}

			// build the footer
			sb.AppendLine($"+{new string('-', largest + 2)}+{new string('-', 10)}+");
			sb.AppendLine("```");

			return sb.ToString();
		}

		public string TimeSpanToString(TimeSpan ts)
		{
			return (ts.Hours > 0 ? $"{ts.Hours} hours" : "") + (ts.Minutes > 0 && ts.Hours > 0 ? " and " : "") + (ts.Minutes > 0 || ts.Hours == 0 ? $"{ts.Minutes} minutes" : "");
		}

		// find the timezone for this user. return null if fail
		public TimeZoneInfo UserToTimezone(SocketUser user)
		{
			var userguild = user as SocketGuildUser;

			string tz = userguild.Roles.Select(r => r.Name).Where(r => IsTimezone(r)).FirstOrDefault();
			if (tz == null) return null;
			return IDToTimezone(tz);
		}

		// find the timezone info for given id string
		public TimeZoneInfo IDToTimezone(string id)
		{
			TZConvert.TryGetTimeZoneInfo(id, out TimeZoneInfo t);
			return t;
		}

		// using this guild, find the timezones that are used in this guild
		public IEnumerable<string> GuildToTimezones(SocketGuild guild)
		{
			HashSet<string> timezones = new HashSet<string>();
			foreach (var user in guild.Users)
			{
				string tz = user.Roles.Select(r => r.Name).Where(r => IsTimezone(r)).FirstOrDefault();
				if (tz != null) timezones.Add(tz);
			}
			return timezones;
		}

		public IEnumerable<KeyValuePair<string, DateTime>> LocalTimeToTimetable(DateTime localtime, TimeZoneInfo source, SocketGuild guild)
		{
			foreach(string s in GuildToTimezones(guild).OrderBy(x => x))
			{
				yield return new KeyValuePair<string, DateTime>(s, TimeZoneInfo.ConvertTime(localtime, source, IDToTimezone(s)));
			}
		}

		// regular expressions for string analysis
		private static readonly Regex timeRX = new Regex(@"\$(?<hours>\d{1,2})(:(?<minutes>\d{2}))?\s*(?<ampm>(am|pm))($|\s)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex timeonlyRX = new Regex(@"^(?<hours>\d{1,2})(:(?<minutes>\d{2}))?\s*(?<ampm>(am|pm))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static Regex appointment = new Regex(@"^(?<day>\d{2}):(?<month>\d{2}):(?<year>\d{4})\s+(?<hour>\d{1,2})(:(?<minute>\d{2}))?\s*(?<ampm>am|pm)\s+(?<title>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static Regex date = new Regex(@"(?<day>\d{2}):(?<month>\d{2}):(?<year>\d{4})\s+(?<hour>\d{1,2})(:(?<minute>\d{2}))?\s*(?<ampm>am|pm)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		// find a time indication in a string and return it as a timespan
		public TimeSpan? StringToTime(string input, bool onlytime = false)
		{
			// try to find a match
			Regex rx = onlytime ? timeonlyRX : timeRX;
			Match match = rx.Match(input);
			if (!match.Success) return null;

			// get all the time components
			GroupCollection groups = match.Groups;
			int hours = int.Parse(groups["hours"].Value);
			int minutes = groups["minutes"].Success ? int.Parse(groups["minutes"].Value) : 0;

			if (hours > 12 || minutes > 59) return null;

			hours = (hours % 12) + (groups["ampm"].Value == "pm" ? 12 : 0);

			return new TimeSpan(hours, minutes, 0);
		}

		public DateTime? StringToDatetime(string input)
		{
			Match match = date.Match(input);
			if (!match.Success) return null;

			GroupCollection groups = match.Groups;
			int day = int.Parse(groups["day"].Value);
			int month = int.Parse(groups["month"].Value);
			int year = int.Parse(groups["year"].Value);
			int hour = int.Parse(groups["hour"].Value);
			int minute = groups["minute"].Success ? int.Parse(groups["minute"].Value) : 0;

			if (hour > 12 || minute > 59 || month > 12 || month < 1 || day > DateTime.DaysInMonth(year, month)) return null;

			bool past = groups["ampm"].Value == "pm";
			hour = (hour % 12) + (past ? 12 : 0);

			return new DateTime(year, month, day, hour, minute, 0);
		}

		public async Task WaitForDate(DateTime date, CancellationToken token)
		{
			while (!token.IsCancellationRequested && (date - DateTime.UtcNow).TotalMilliseconds > int.MaxValue)
			{
				await Task.Delay(int.MaxValue >> 1, token);
			}

			await Task.Delay(date - DateTime.UtcNow, token);
		}

		// takes a string and returns the date and the title
		public void StringToAppointment(string input, out DateTime? date, out string title)
		{
			Match match = appointment.Match(input);
			if (!match.Success)
			{
				date = null;
				title = null;
				return;
			}

			GroupCollection groups = match.Groups;
			int day = int.Parse(groups["day"].Value);
			int month = int.Parse(groups["month"].Value);
			int year = int.Parse(groups["year"].Value);
			int hour = int.Parse(groups["hour"].Value);
			int minute = groups["minute"].Success ? int.Parse(groups["minute"].Value) : 0;

			if (hour > 12 || minute > 59 || month > 12 || month < 1 || day > DateTime.DaysInMonth(year, month))
			{
				date = null;
				title = null;
				return;
			}

			bool past = groups["ampm"].Value == "pm";
			hour = (hour % 12) + (past ? 12 : 0);

			date = new DateTime(year, month, day, hour, minute, 0);
			title = groups["title"].Value;
		}

		private static HashSet<string> timezones = new HashSet<string>(TZConvert.KnownWindowsTimeZoneIds.Where(s => !s.StartsWith("UTC")));
		
		public bool IsTimezone(string input) => timezones.Contains(input);
		public IEnumerable<string> Timezones() => timezones;
	}
}
