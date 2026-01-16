using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core
{
    public sealed class WorkspaceOneApiException : Exception
    {
        public int? HttpStatusCode { get; }
        public string Endpoint { get; }
        public string? ResponseBody { get; }
        public WorkspaceOneApiError? ApiError { get; }

        public WorkspaceOneApiException(
            string endpoint,
            int? httpStatusCode,
            string? responseBody,
            WorkspaceOneApiError? apiError,
            Exception? inner = null)
            : base(BuildMessage(endpoint, httpStatusCode, apiError), inner)
        {
            Endpoint = endpoint;
            HttpStatusCode = httpStatusCode;
            ResponseBody = responseBody;
            ApiError = apiError;
        }

        private static string BuildMessage(string endpoint, int? status, WorkspaceOneApiError? apiError)
        {
            var msg = apiError?.Message ?? "Workspace ONE request failed.";
            var act = apiError?.ActivityId;
            return $"WS1 error calling {endpoint} (HTTP {(status?.ToString() ?? "?")}): {msg}"
                 + (string.IsNullOrWhiteSpace(act) ? "" : $" (activityId {act})");
        }
    }
}
