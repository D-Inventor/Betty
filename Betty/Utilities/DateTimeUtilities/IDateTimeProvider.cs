using System;
using System.Collections.Generic;
using System.Text;

namespace Betty.Utilities.DateTimeUtilities
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}
