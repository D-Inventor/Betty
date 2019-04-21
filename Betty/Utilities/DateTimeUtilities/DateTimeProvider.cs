using System;
using System.Collections.Generic;
using System.Text;

namespace Betty.Utilities.DateTimeUtilities
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
