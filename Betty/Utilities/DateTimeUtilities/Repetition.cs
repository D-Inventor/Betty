using System;
using System.Collections.Generic;
using System.Text;

namespace Betty.Utilities.DateTimeUtilities
{
    public struct Repetition
    {
        public RepetitionUnit Unit { get; }
        public int Amount { get; }

        public Repetition(RepetitionUnit unit, int amount)
        {
            if(unit != RepetitionUnit.Once && amount <= 0) { throw new ArgumentException("Amount has to be a value larger than 0.", nameof(amount)); }

            Unit = unit;
            Amount = amount;
        }

        public string Id
        {
            get
            {
                if (Unit == RepetitionUnit.Once) { return "o"; }
                switch (Unit)
                {
                    case RepetitionUnit.Day:
                        return $"d{Amount}";
                    case RepetitionUnit.Week:
                        return $"w{Amount}";
                    case RepetitionUnit.Month:
                        return $"m{Amount}";
                    case RepetitionUnit.Year:
                        return $"y{Amount}";
                    default:
                        throw new Exception("This should never happen!");
                }
            }
        }

        public static Repetition FromId(string input)
        {
            // make sure that input is not empty
            if (string.IsNullOrEmpty(input))
            {
                throw new FormatException("input does not look like a repetition string.");
            }

            if(input == "o") { return new Repetition(RepetitionUnit.Once, 0); }

            RepetitionUnit unit;
            switch (input[0])
            {
                case 'd':
                    unit = RepetitionUnit.Day;
                    break;
                case 'w':
                    unit = RepetitionUnit.Week;
                    break;
                case 'm':
                    unit = RepetitionUnit.Month;
                    break;
                case 'y':
                    unit = RepetitionUnit.Year;
                    break;
                default:
                    throw new FormatException("input does not look like a repetition string.");
            }

            int amount = int.Parse(input.Substring(1));
            return new Repetition(unit, amount);
        }

        public override string ToString()
        {
            return Unit == RepetitionUnit.Once ? "once" : $"every {Amount} {Unit.ToString().ToLower()}{(Amount > 1 ? "s" : "")}";
        }

        public DateTime GetNext(DateTime date, TimeZoneInfo timezone)
        {
            switch (Unit)
            {
                case RepetitionUnit.Day:
                    return GetNextDay(date, timezone);
                case RepetitionUnit.Week:
                    return GetNextWeek(date, timezone);
                case RepetitionUnit.Month:
                    return GetNextMonth(date, timezone);
                case RepetitionUnit.Year:
                    return GetNextYear(date, timezone);
                default:
                    throw new Exception("The given repetition unit is unknown.");
            }
        }

        private DateTime GetNextDay(DateTime date, TimeZoneInfo timezone)
        {
            int x = 1;
            DateTime output;
            do
            {
                output = date.AddDays(Amount * x++);
            } while (timezone.IsInvalidTime(output));

            return output;
        }

        private DateTime GetNextWeek(DateTime date, TimeZoneInfo timezone)
        {
            int x = 1;
            DateTime output;
            do
            {
                output = date.AddDays(Amount * 7 * x++);
            } while (timezone.IsInvalidTime(output));

            return output;
        }

        private DateTime GetNextMonth(DateTime date, TimeZoneInfo timezone)
        {
            int x = 1;
            DateTime output;
            do
            {
                // skip months until you find a month where you have the same day and the time is valid
                output = date.AddMonths(Amount * x++);
            } while (output.Day != date.Day || timezone.IsInvalidTime(output));

            return output;
        }

        private DateTime GetNextYear(DateTime date, TimeZoneInfo timezone)
        {
            int x = 1;
            DateTime output;
            do
            {
                output = date.AddYears(Amount * x++);
            } while (output.Day != date.Day || timezone.IsInvalidTime(output));

            return output;
        }
    }

    public enum RepetitionUnit
    {
        Once,
        Day,
        Week,
        Month,
        Year
    }
}
