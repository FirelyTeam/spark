using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Spark.Core.Exceptions;
using Hl7.Fhir.Model;

namespace Spark.Core
{
    public static class Error
    {

        public static SparkException Create(HttpStatusCode code, string message, params object[] values)
        {
            return new SparkException(code, message, values);
        }

        public static SparkException BadRequest(string message, params object[] values)
        {
            return new SparkException(HttpStatusCode.BadRequest, message, values);
        }

        public static SparkException NotFound(string message, params object[] values)
        {
            return new SparkException(HttpStatusCode.NotFound, message, values);
        }

        public static SparkException Internal(string message, params object[] values)
        {
            return new SparkException(HttpStatusCode.InternalServerError, message, values);
        }

        public static SparkException NotSupported(string message, params object[] values)
        {
            return new SparkException(HttpStatusCode.NotImplemented, message, values);
        }

        private static OperationOutcome.OperationOutcomeIssueComponent CreateValidationResult(string details, IEnumerable<string> location)
        {
            return new OperationOutcome.OperationOutcomeIssueComponent()
            {
                Severity = OperationOutcome.IssueSeverity.Error,
                Code = new CodeableConcept("http://hl7.org/fhir/issue-type", "invalid"),
                Details = details,
                Location = location
            };
        }
    }
}
