using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using log4net;

namespace Enferno.Public.Logging.Tests
{
    [TestClass]
    public class Log4NetTraceListenerTests
    {
        private readonly ILog logger;

        public Log4NetTraceListenerTests()
        {
            log4net.Config.XmlConfigurator.Configure();

            logger = LogManager.GetLogger(GetType());
        }

        [TestMethod, TestCategory("UnitTest")]
        public void CanLogToLogEntriesThruLog4NetTraceListener()
        {
            Log.LogEntry
                .Message("This is a test from Enterprise Library logger")
                .Property("TestProperty", 1)
                .Categories(CategoryFlags.Alert)
                .WriteError();

            //make sure we have written everything to LogEntries.com
            FlushToLogEntries();
        }

        private static void FlushToLogEntries()
        {
            // This will give LE background thread some time to finish sending messages to Logentries.
            var numWaits = 3;
            while (!LogentriesCore.Net.AsyncLogger.AreAllQueuesEmpty(TimeSpan.FromSeconds(5)) && numWaits > 0)
                numWaits--;
        }

    }
}
