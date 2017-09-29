using System;
using System.Diagnostics;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners;
using log4net;

namespace Enferno.Public.Logging
{
    [ConfigurationElementType(typeof(CustomTraceListenerData))]
    public class Log4NetTraceListener : CustomTraceListener
    {
        internal struct Log4NetSeverity
        {
            public const string Debug = "Debug";
            public const string Warn = "Warn";
            public const string Error = "Error";
            public const string Fatal = "Fatal";
            public const string Info = "Info";
        }

        public ILog Log4NetLogger
        {
            get
            {
                return LogManager.GetLogger(Log4NetLoggerName);
            }
        }

        public string Log4NetLoggerName
        {
            get
            {
                return Attributes.ContainsKey("loggerName") ? Attributes["loggerName"] : Name;
            }
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            string logMessage;
            if (data is LogEntry && Formatter != null)
            {
                logMessage = Formatter.Format(data as LogEntry);
            }
            else
            {
                logMessage = data.ToString();
            }


            var severity = ConvertToLog4NetSeverity(eventType);
            Log(logMessage, severity);
        }

        private void Log(string message, string severity)
        {

            switch (severity)
            {
                case Log4NetSeverity.Debug:
                    Log4NetLogger.Debug(message);
                    break;
                case Log4NetSeverity.Error:
                    Log4NetLogger.Error(message);
                    break;
                case Log4NetSeverity.Fatal:
                    Log4NetLogger.Fatal(message);
                    break;
                case Log4NetSeverity.Info:
                    Log4NetLogger.Info(message);
                    break;
                case Log4NetSeverity.Warn:
                    Log4NetLogger.Warn(message);
                    break;
            }   
        }

        private static string ConvertToLog4NetSeverity(TraceEventType eventType)
        {
            switch (eventType)
            {
                case TraceEventType.Critical:
                    return Log4NetSeverity.Fatal;
                case TraceEventType.Error:
                    return Log4NetSeverity.Error;
                case TraceEventType.Information:
                    return Log4NetSeverity.Info;
                case TraceEventType.Warning:
                    return Log4NetSeverity.Warn;
            }
            return Log4NetSeverity.Debug;
        }

        public override void Write(string message)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(string message)
        {
            throw new NotImplementedException();
        }
    }
}
