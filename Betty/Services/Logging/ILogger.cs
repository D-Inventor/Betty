using System;
using System.Collections.Generic;
using System.Text;

namespace Betty.Services
{
    public interface ILogger
    {
        void LogDebug(string source, object message);
        void LogInfo(string source, object message);
        void LogWarning(string source, object message);
        void LogError(string source, object message);
    }
}
