using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using Atlassian.Jira;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners;

namespace Enferno.Public.Logging
{
    [ConfigurationElementType(typeof(CustomTraceListenerData))]
    public class JiraTraceListener : CustomTraceListener
    {
        private readonly string jiraEndpoint;
        private readonly string jiraUserName;
        private readonly string jiraPassword;
        private readonly string jiraComponent;

        public JiraTraceListener()
        {
            jiraEndpoint = ConfigurationManager.AppSettings["JiraEndpoint"] ?? string.Empty;
            jiraUserName = ConfigurationManager.AppSettings["JiraUserName"] ?? string.Empty;
            jiraPassword = ConfigurationManager.AppSettings["JiraPassword"] ?? string.Empty;
            jiraComponent = ConfigurationManager.AppSettings["JiraComponent"] ?? string.Empty;
        }
        
        internal struct JiraPriority
        {
            public const string Highest = "Highest";
            public const string High = "High";
            public const string Medium = "Medium";
            public const string Low = "Low";
            public const string None = "None";
        }
        
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            try
            {
                var priority = ConvertSeverityToJiraPriority(eventType);

                string clientId = null;
                string applicationId = null;
                string quotationId = null;
                string logHeader;
                string logMessage;
                if (data is LogEntry entry)
                {
                    clientId = GetExtendedProperty(entry, "ClientId");
                    applicationId = GetExtendedProperty(entry, "ApplicationId");
                    quotationId = GetExtendedProperty(entry, "QuotationId");
                    logHeader = !string.IsNullOrWhiteSpace(quotationId) ? $"Orderflow error for Basket ID: {quotationId}" : entry.Message;
                    logMessage = Formatter != null ? Formatter.Format(entry) : data.ToString();
                }
                else
                {
                    logHeader = "System Alert";
                    logMessage = data.ToString();
                }

                CreateOrAppendIssue(logHeader, logMessage, priority, jiraComponent, clientId, applicationId, quotationId);
            }
            catch (Exception ex)
            {
                var method = new StackTrace().GetFrame(1).GetMethod().Name;
                var message = $"Error in {GetType().Name}.{method}";
                Log.LogEntry.Categories(CategoryFlags.Alert)
                    .Message(message)
                    .ErrorMessages(ex.Message)
                    .Exceptions(ex)
                    .WriteError();
            }
        }

        private void CreateOrAppendIssue(string header, string message, string priority, string component, string clientId, string applicationId, string quotationId)
        {
            const string project = "SA";
            const string issueType = "Incident";

            var jira = Jira.CreateRestClient(jiraEndpoint, jiraUserName, jiraPassword);

            var existingIssueKey = GetExistingActiveIssue(jira, project, clientId, quotationId);

            if (!string.IsNullOrWhiteSpace(existingIssueKey))
            {
                AppendExistingIssue(jira, existingIssueKey, message, component);
                return;
            }

            CreateIssue(jira, header, message, project, issueType, priority, component, clientId, applicationId, quotationId);
        }

        private static string GetExistingActiveIssue(Jira jira, string project, string clientId, string quotationId)
        {
            if (string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(quotationId))
            {
                return string.Empty;
            }

            try
            {
                return jira.Issues.Queryable
                    .FirstOrDefault(i =>
                        i.Project == project &&
                        (i.Status == "Open" || i.Status == "Pending" || i.Status == "Work in progress") &&
                        i["Client ID"] == clientId &&
                        i["Quotation ID"] == quotationId)
                    ?.Key.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void AppendExistingIssue(Jira jira, string issueKey, string message, string component)
        {
            var existingIssue = jira.Issues.GetIssueAsync(issueKey).Result;

            existingIssue.AddCommentAsync(message).Wait();

            if (string.IsNullOrWhiteSpace(component))
            {
                return;
            }

            try
            {
                existingIssue.Components.Add(component);
                existingIssue.SaveChanges();
            }
            catch 
            {
                // Ignoring errors on component update
            }
        }

        private static void CreateIssue(Jira jira, string header, string message, string project, string issueType, string priority, string component, string clientId, string applicationId, string quotationId)
        {
            var newIssue = jira.CreateIssue(project);
            newIssue.Type = issueType;
            newIssue.Summary = header;
            newIssue.Description = message;
            newIssue.Priority = priority;

            newIssue.SaveChanges();

            try
            {
                if (!string.IsNullOrWhiteSpace(component))
                {
                    newIssue.Components.Add(component);
                }

                if (!string.IsNullOrWhiteSpace(clientId))
                {
                    newIssue["Client ID"] = clientId;
                }

                if (!string.IsNullOrWhiteSpace(applicationId))
                {
                    newIssue["Application ID"] = applicationId;
                }

                if (!string.IsNullOrWhiteSpace(quotationId))
                {
                    newIssue["Quotation ID"] = quotationId;
                }

                newIssue.SaveChanges();
            }
            catch
            {
                // Ignoring errors on custom field creation.
            }
        }

        private static string GetExtendedProperty(LogEntry entry, string property)
        {
            return entry.ExtendedProperties.TryGetValue(property, out var o) ? o.ToString() : null;
        }

        private static string ConvertSeverityToJiraPriority(TraceEventType eventType)
        {
            switch (eventType)
            {
                case TraceEventType.Critical:
                    return JiraPriority.Highest;
                case TraceEventType.Error:
                    return JiraPriority.High;
                case TraceEventType.Warning:
                    return JiraPriority.Medium;
                case TraceEventType.Information:
                    return JiraPriority.Low;
            }
            return JiraPriority.None;
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
