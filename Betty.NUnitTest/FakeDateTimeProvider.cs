using System;
using Betty.Utilities.DateTimeUtilities;

namespace Betty.NUnitTest
{
    public class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; }
    }
}