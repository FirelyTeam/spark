/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Spark.Engine.Core;
using System.Diagnostics;
using System.Linq;
using Spark.Engine.Utility;
#if NETSTANDARD2_0 || NET6_0
using Microsoft.AspNetCore.Mvc;
#endif

namespace Spark.Engine.Extensions;

public static class OperationOutcomeExtensions
{
    internal static Func<string, string> pascalToCamelCase = (pascalCase) => $"{char.ToLower(pascalCase[0])}{pascalCase.Substring(1)}";

#if NETSTANDARD2_0 || NET6_0
        public static OperationOutcome AddValidationProblems(this OperationOutcome outcome, Type resourceType, HttpStatusCode code, ValidationProblemDetails validationProblems)
        {
            if (resourceType == null) throw new ArgumentNullException(nameof(resourceType));
            if (validationProblems == null) throw new ArgumentNullException(nameof(ValidationProblemDetails));

            OperationOutcome.IssueSeverity severity = IssueSeverityOf(code);
            foreach (var error in validationProblems.Errors)
            {
                var expression = FhirPathUtil.ResolveToFhirPathExpression(resourceType, error.Key);
                outcome.Issue.Add(new OperationOutcome.IssueComponent
                {
                    Severity = severity,
                    Code = OperationOutcome.IssueType.Required,
                    Diagnostics = error.Value.FirstOrDefault(),
                    Expression = new[] { expression },
                    Location = new[] { FhirPathUtil.ConvertToXPathExpression(expression) }
                });
            }

            return outcome;
        }
#endif

    internal static OperationOutcome.IssueSeverity IssueSeverityOf(HttpStatusCode code)
    {
        int range = ((int)code / 100);
        switch(range)
        {
            case 1:
            case 2: return OperationOutcome.IssueSeverity.Information;
            case 3: return OperationOutcome.IssueSeverity.Warning;
            case 4: return OperationOutcome.IssueSeverity.Error;
            case 5: return OperationOutcome.IssueSeverity.Fatal;
            default: return OperationOutcome.IssueSeverity.Information;
        }
    }

    internal static OperationOutcome.IssueType IssueTypeOf(HttpStatusCode code)
    {
        switch (code)
        {
            case HttpStatusCode.BadRequest:
                return OperationOutcome.IssueType.Invalid;
            case HttpStatusCode.Forbidden:
                return OperationOutcome.IssueType.Forbidden;
            case HttpStatusCode.NotFound:
                return OperationOutcome.IssueType.NotFound;
            case HttpStatusCode.MethodNotAllowed:
            case HttpStatusCode.NotAcceptable:
            case HttpStatusCode.UnsupportedMediaType:
            case HttpStatusCode.NotImplemented:
                return OperationOutcome.IssueType.NotSupported;
            case HttpStatusCode.Conflict:
            case HttpStatusCode.PreconditionFailed:
                return OperationOutcome.IssueType.Conflict;
            case HttpStatusCode.ServiceUnavailable:
                return OperationOutcome.IssueType.Transient;
        }

        return (int)code >= 500 ? OperationOutcome.IssueType.Exception : OperationOutcome.IssueType.Processing;
    }

    private static void SetContentHeaders(HttpResponseMessage response, ResourceFormat format)
    {
        response.Content.Headers.ContentType = FhirMediaType.GetMediaTypeHeaderValue(typeof(Resource), format);
    }

    public static OperationOutcome Init(this OperationOutcome outcome)
    {
        if (outcome.Issue == null)
        {
            outcome.Issue = new List<OperationOutcome.IssueComponent>();
        }
        return outcome;
    }

    public static OperationOutcome AddError(this OperationOutcome outcome, Exception exception)
    {
        string message;
        OperationOutcome.IssueType code;

        if (exception is SparkException sparkException)
        {
            message = sparkException.Message;
            code = IssueTypeOf(sparkException.StatusCode);
        }
        else
        {
            message = $"{exception.GetType().Name}: {exception.Message}";
            code = OperationOutcome.IssueType.Exception;
        }

        outcome.AddIssue(OperationOutcome.IssueSeverity.Error, code, message);

        // Don't add a stacktrace if this is an acceptable logical-level error
        if (Debugger.IsAttached && !(exception is SparkException))
        {
            // FIXME: Log the stack trace.
            outcome.AddIssue(OperationOutcome.IssueSeverity.Information, OperationOutcome.IssueType.Informational, exception.StackTrace);
        }

        return outcome;
    }

    public static OperationOutcome AddAllInnerErrors(this OperationOutcome outcome, Exception exception)
    {
        AddError(outcome, exception);
        while (exception.InnerException != null)
        {
            exception = exception.InnerException;
            AddError(outcome, exception);
        }

        return outcome;
    }

    public static OperationOutcome AddError(this OperationOutcome outcome, string message)
    {
        return outcome.AddError(message, OperationOutcome.IssueType.Processing);
    }

    public static OperationOutcome AddError(this OperationOutcome outcome, string message, OperationOutcome.IssueType code)
    {
        return outcome.AddIssue(OperationOutcome.IssueSeverity.Error, code, message);
    }

    public static OperationOutcome AddMessage(this OperationOutcome outcome, string message)
    {
        return outcome.AddIssue(OperationOutcome.IssueSeverity.Information, OperationOutcome.IssueType.Informational, message);
    }

    public static OperationOutcome AddMessage(this OperationOutcome outcome, HttpStatusCode code, string message)
    {
        OperationOutcome.IssueSeverity severity = IssueSeverityOf(code);
        OperationOutcome.IssueType issueType = severity is OperationOutcome.IssueSeverity.Information or OperationOutcome.IssueSeverity.Warning
            ? OperationOutcome.IssueType.Informational
            : IssueTypeOf(code);
        return outcome.AddIssue(severity, issueType, message);
    }

    private static OperationOutcome AddIssue(this OperationOutcome outcome, OperationOutcome.IssueSeverity severity, OperationOutcome.IssueType code, string message)
    {
        if (outcome.Issue == null) outcome.Init();

        var item = new OperationOutcome.IssueComponent
        {
            Severity = severity,
            Code = code,
            Diagnostics = message
        };
        outcome.Issue.Add(item);
        return outcome;
    }

    [Obsolete("Use method with signature HttpResponseMessage ToHttpResponseMessage(this OperationOutcome, ResourceFormat) instead.")]
    public static HttpResponseMessage ToHttpResponseMessage(this OperationOutcome outcome, ResourceFormat target, HttpRequestMessage request)
    {
        return ToHttpResponseMessage(outcome, target);
    }

    public static HttpResponseMessage ToHttpResponseMessage(this OperationOutcome outcome, ResourceFormat target)
    {
        // TODO: Remove this method is seems to not be in use.
        byte[] data = null;
        if (target == ResourceFormat.Xml)
        {
            FhirXmlSerializer serializer = new FhirXmlSerializer();
            data = serializer.SerializeToBytes(outcome);
        }
        else if (target == ResourceFormat.Json)
        {
            FhirJsonSerializer serializer = new FhirJsonSerializer();
            data = serializer.SerializeToBytes(outcome);
        }
        HttpResponseMessage response = new HttpResponseMessage
        {
            Content = new ByteArrayContent(data)
        };
        SetContentHeaders(response, target);

        return response;
    }
}
