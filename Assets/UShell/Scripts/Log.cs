using UnityEngine;

namespace UShell
{
    public class Log
    {
        public LogType LogType { get; }
        public string Value { get; }
        public string StackTrace { get; }

        public Log(LogType logType, string value, string stackTrace)
        {
            LogType = logType;
            Value = value;
            StackTrace = stackTrace;
        }
    }
}
