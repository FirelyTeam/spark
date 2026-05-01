/* 
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Extensions;
using System;

namespace Spark.Engine.Test.Extensions;

[TestClass]
public class OperationOutcomeExtensionsTests
{
    [TestMethod]
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

        Assert.AreEqual(0, outcome.Issue.FindIndex(i => i.Diagnostics.Equals("Exception: First error level")), "First error level should be at index 0");
        Assert.AreEqual(1, outcome.Issue.FindIndex(i => i.Diagnostics.Equals("Exception: Second error level")), "Second error level should be at index 1");
        Assert.AreEqual(2, outcome.Issue.FindIndex(i => i.Diagnostics.Equals("Exception: Third error level")), "Third error level should be at index 2");
    }

    [TestMethod]
    public void IssueSeverity_Is_Information_When_HttpStatusCode_Is_Continue_Test()
    {
        Assert.AreEqual(OperationOutcome.IssueSeverity.Information, OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.Continue));
    }

    [TestMethod]
    public void IssueSeverity_Is_Information_When_HttpStatusCode_Is_Created_Test()
    {
        Assert.AreEqual(OperationOutcome.IssueSeverity.Information, OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.Created));
    }

    [TestMethod]
    public void IssueSeverity_Is_Warning_When_HttpStatusCode_Is_MovedPermanently_Test()
    {
        Assert.AreEqual(OperationOutcome.IssueSeverity.Warning, OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.MovedPermanently));
    }

    [TestMethod]
    public void IssueSeverity_Is_Error_When_HttpStatusCode_Is_BadRequest_Test()
    {
        Assert.AreEqual(OperationOutcome.IssueSeverity.Error, OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.BadRequest));
    }

    [TestMethod]
    public void IssueSeverity_Is_Fatal_When_HttpStatusCode_Is_InternalServerError_Test()
    {
        Assert.AreEqual(OperationOutcome.IssueSeverity.Fatal, OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.InternalServerError));
    }
}
