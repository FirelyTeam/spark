/*
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System;
using System.Net;
using Spark.Engine.Core;
using System.Diagnostics;
using System.Linq;
using Spark.Engine.Utility;
using Microsoft.AspNetCore.Mvc;

namespace Spark.Engine.Extensions;

public static class OperationOutcomeExtensions
{
    public static OperationOutcome AddValidationProblems(this OperationOutcome outcome, Type resourceType, HttpStatusCode code, ValidationProblemDetails validationProblems)
    {
        ArgumentNullException.ThrowIfNull(resourceType);
        ArgumentNullException.ThrowIfNull(validationProblems);

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

    // FIXME: [next-major-release] Remove Init
    [Obsolete("This method is a no-op, no alternative needed.")]
    public static OperationOutcome Init(this OperationOutcome outcome)
    {
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

    private static OperationOutcome AddIssue(this OperationOutcome outcome, OperationOutcome.IssueSeverity severity, OperationOutcome.IssueType code, string message)
    {
        var item = new OperationOutcome.IssueComponent
        {
            Severity = severity,
            Code = code,
            Diagnostics = message
        };
        outcome.Issue.Add(item);
        return outcome;
    }
}
