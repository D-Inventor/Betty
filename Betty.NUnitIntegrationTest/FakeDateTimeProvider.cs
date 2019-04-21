using Betty.Utilities.DateTimeUtilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Betty.NUnitIntegrationTest
{
    class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; }
    }
}
