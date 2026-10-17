/* 
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Xunit;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using System;
using System.Net;

namespace Spark.Engine.Tests.Extensions;

public class OperationOutcomeExtensionsTests
{
    [Fact]
    public void Three_Level_InnerErrors_Test()
    {
        OperationOutcome outcome;

        try
        {
            try
            {
                try
                {
                    throw new Exception("Third error level");
                }
                catch (Exception e3)
                {
                    throw new Exception("Second error level", e3);
                }
            }
            catch (Exception e2)
            {
                throw new Exception("First error level", e2);
            }
        }
        catch (Exception e1)
        {
            outcome = new OperationOutcome().AddAllInnerErrors(e1);
        }

        Assert.Equal(0, outcome.Issue.FindIndex(i => i.Diagnostics.Equals("Exception: First error level")));
        Assert.Equal(1, outcome.Issue.FindIndex(i => i.Diagnostics.Equals("Exception: Second error level")));
        Assert.Equal(2, outcome.Issue.FindIndex(i => i.Diagnostics.Equals("Exception: Third error level")));
    }

    [Fact]
    public void IssueSeverity_Is_Information_When_HttpStatusCode_Is_Continue_Test()
    {
        Assert.Equal(OperationOutcome.IssueSeverity.Information, OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.Continue));
    }

    [Fact]
    public void IssueSeverity_Is_Information_When_HttpStatusCode_Is_Created_Test()
    {
        Assert.Equal(OperationOutcome.IssueSeverity.Information, OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.Created));
    }

    [Fact]
    public void IssueSeverity_Is_Warning_When_HttpStatusCode_Is_MovedPermanently_Test()
    {
        Assert.Equal(OperationOutcome.IssueSeverity.Warning, OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.MovedPermanently));
    }

    [Fact]
    public void IssueSeverity_Is_Error_When_HttpStatusCode_Is_BadRequest_Test()
    {
        Assert.Equal(OperationOutcome.IssueSeverity.Error, OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.BadRequest));
    }

    [Fact]
    public void IssueSeverity_Is_Fatal_When_HttpStatusCode_Is_InternalServerError_Test()
    {
        Assert.Equal(OperationOutcome.IssueSeverity.Fatal, OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.InternalServerError));
    }

    [Fact]
    public void AddAllInnerErrors_Sets_IssueType_From_StatusCode_For_SparkException()
    {
        var outcome = new OperationOutcome().AddAllInnerErrors(Error.BadRequest("Body parsing failed"));

        var issue = Assert.Single(outcome.Issue);
        Assert.Equal(OperationOutcome.IssueType.Invalid, issue.Code);
    }

    [Fact]
    public void AddAllInnerErrors_Sets_IssueType_Exception_For_Other_Exceptions()
    {
        var outcome = new OperationOutcome().AddAllInnerErrors(new InvalidOperationException("Something failed"));

        var issue = Assert.Single(outcome.Issue);
        Assert.Equal(OperationOutcome.IssueType.Exception, issue.Code);
    }

    [Fact]
    public void AddError_With_Message_Sets_IssueType_Processing()
    {
        var outcome = new OperationOutcome().AddError("Something failed");

        var issue = Assert.Single(outcome.Issue);
        Assert.Equal(OperationOutcome.IssueType.Processing, issue.Code);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, OperationOutcome.IssueType.Invalid)]
    [InlineData(HttpStatusCode.Forbidden, OperationOutcome.IssueType.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, OperationOutcome.IssueType.NotFound)]
    [InlineData(HttpStatusCode.MethodNotAllowed, OperationOutcome.IssueType.NotSupported)]
    [InlineData(HttpStatusCode.NotAcceptable, OperationOutcome.IssueType.NotSupported)]
    [InlineData(HttpStatusCode.UnsupportedMediaType, OperationOutcome.IssueType.NotSupported)]
    [InlineData(HttpStatusCode.NotImplemented, OperationOutcome.IssueType.NotSupported)]
    [InlineData(HttpStatusCode.Conflict, OperationOutcome.IssueType.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed, OperationOutcome.IssueType.Conflict)]
    [InlineData(HttpStatusCode.Gone, OperationOutcome.IssueType.Processing)]
    [InlineData(HttpStatusCode.ServiceUnavailable, OperationOutcome.IssueType.Transient)]
    [InlineData(HttpStatusCode.InternalServerError, OperationOutcome.IssueType.Exception)]
    public void IssueType_Is_Mapped_From_HttpStatusCode(HttpStatusCode code, OperationOutcome.IssueType expected)
    {
        Assert.Equal(expected, OperationOutcomeExtensions.IssueTypeOf(code));
    }
}
