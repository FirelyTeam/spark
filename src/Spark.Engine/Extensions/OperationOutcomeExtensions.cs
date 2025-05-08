/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
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

        if (exception is SparkException)
            message = exception.Message;
        else
            message = string.Format("{0}: {1}", exception.GetType().Name, exception.Message);

        outcome.AddError(message);

        // Don't add a stacktrace if this is an acceptable logical-level error
        if (Debugger.IsAttached && !(exception is SparkException))
        {
            var stackTrace = new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Information,
                Diagnostics = exception.StackTrace
            };
            outcome.Issue.Add(stackTrace);
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
        return outcome.AddIssue(OperationOutcome.IssueSeverity.Error, message);
    }

    private static OperationOutcome AddIssue(this OperationOutcome outcome, OperationOutcome.IssueSeverity severity, string message)
    {
        if (outcome.Issue == null) outcome.Init();

        var item = new OperationOutcome.IssueComponent
        {
            Severity = severity,
            Diagnostics = message
        };
        outcome.Issue.Add(item);
        return outcome;
    }
}
