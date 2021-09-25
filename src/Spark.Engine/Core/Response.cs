using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System.Collections.Generic;
using System.Net;
using System.Security.Permissions;

namespace Spark.Engine.Core
{
    // THe response class is an abstraction of the Fhir REST responses
    // This way, it's easier to implement multiple WebApi controllers
    // without having to implement functionality twice.
    // The FhirService always responds with a "Response"

    public class FhirResponse
    {
        private Resource _resource;

        public HttpStatusCode StatusCode;
        public IKey Key;
        public Prefer Prefer = Prefer.ReturnRepresentation;

        private static Dictionary<int, string> _diagnosticText = new Dictionary<int, string>
        {
            { 200, "Sucessfully updated resource \"{resource}\"" },
            { 201, "Sucessfully created resource \"{resource}\"" }
        };

        private static string BuildDiagnosticsText(int statusCode, IKey key)
        {
            if (key == null) return null;

            var relativeUrl = $"{key.TypeName}/{key.ResourceId}";
            if (key.HasVersionId())
            {
                relativeUrl += $"/_history/{key.VersionId}";
            }

            return _diagnosticText.ContainsKey(statusCode)
                ? _diagnosticText[statusCode].Replace("{resource}", relativeUrl)
                : null;
        }

        public FhirResponse(HttpStatusCode code, IKey key, Resource resource, Prefer prefer)
        {
            StatusCode = code;
            Key = key;
            _resource = resource;
            Prefer = prefer;
        }

        public FhirResponse(HttpStatusCode code, IKey key, Resource resource)
        {
            StatusCode = code;
            Key = key;
            _resource = resource;
        }

        public FhirResponse(HttpStatusCode code, Resource resource)
        {
            StatusCode = code;
            Key = null;
            _resource = resource;
        }

        public FhirResponse(HttpStatusCode code)
        {
            StatusCode = code;
            Key = null;
            _resource = null;
        }

        public Resource Resource
        {
            get
            {
                switch (Prefer)
                {
                    case Prefer.OperationOutcome:
                        return new OperationOutcome
                        {
                            Issue = new List<OperationOutcome.IssueComponent>
                            {
                                new OperationOutcome.IssueComponent
                                {
                                    Severity = OperationOutcome.IssueSeverity.Information,
                                    Code = OperationOutcome.IssueType.Informational,
                                    Diagnostics = BuildDiagnosticsText((int)StatusCode, Key)
                                }
                            }
                        };
                    case Prefer.ReturnMinimal:
                        return null;
                    default:
                        return _resource;
                }
            }
            set
            {
                _resource = value;
            }
        }

        public bool IsValid
        {
            get
            {
                int code = (int)StatusCode;
                return code <= 300;
            }
        }

        public bool HasBody
        {
            get
            {
                return Resource != null && (Prefer == Prefer.ReturnRepresentation || Prefer == Prefer.OperationOutcome);
            }
        }

        public override string ToString()
        {
            string details = (Resource != null) ? string.Format("({0})", Resource.TypeName) : null;
            string location = Key?.ToString();
            return string.Format("{0}: {1} {2} ({3})", (int)StatusCode, StatusCode.ToString(), details, location);
        }
    }
}