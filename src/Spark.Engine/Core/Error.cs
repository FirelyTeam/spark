/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2019-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Collections.Generic;
using System.Net;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Core;

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

    public static SparkException NotFound(IKey key)
    {
        if (key.VersionId == null)
        {
            return NotFound("No {0} resource with id {1} was found.", key.TypeName, key.ResourceId);
        }
        else
        {
            return NotFound("There is no {0} resource with id {1}, or there is no version {2}", key.TypeName, key.ResourceId, key.VersionId);
        }
    }

    public static SparkException NotAllowed(string message)
    {
        return new SparkException(HttpStatusCode.Forbidden, message);
    }

    public static SparkException Internal(string message, params object[] values)
    {
        return new SparkException(HttpStatusCode.InternalServerError, message, values);
    }

    public static SparkException NotSupported(string message, params object[] values)
    {
        return new SparkException(HttpStatusCode.NotImplemented, message, values);
    }

    public static SparkException NotAcceptable(string message = null)
    {
        return new SparkException(HttpStatusCode.NotAcceptable, message ?? "Not Acceptable");
    }

    public static SparkException UnsupportedMediaType(string message = null)
    {
        return new SparkException(HttpStatusCode.UnsupportedMediaType, message ?? "Unsupported Media Type");
    }

    private static OperationOutcome.IssueComponent CreateValidationResult(string details, IEnumerable<string> location)
    {
        return new OperationOutcome.IssueComponent()
        {
            Severity = OperationOutcome.IssueSeverity.Error,
            Code = OperationOutcome.IssueType.Invalid,
            Details = new CodeableConcept("http://hl7.org/fhir/issue-type", "invalid"),
            Diagnostics = details,
            Location = location
        };
    }
}
