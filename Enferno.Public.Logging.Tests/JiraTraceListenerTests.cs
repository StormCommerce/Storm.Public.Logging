using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Enferno.Public.Logging.Tests
{
    [TestClass]
    public class JiraTraceListenerTests
    {
        [TestMethod, TestCategory("UnitTest")]
        public void CanCreateJuraIssueThruJiraTraceListener()
        {
            Log.LogEntry
                .Message("This is a test from Enterprise Library logger")
                .Property("ClientId", 2)
                .Property("ApplicationId", 21)
                .Property("QuotationId", 123456)
                .Categories(CategoryFlags.ClientNotification)
                .WriteError();
        }
    }
}
