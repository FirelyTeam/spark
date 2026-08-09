/* 
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Xunit;
using Spark.Engine.Extensions;
using System;

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
}
