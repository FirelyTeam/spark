/* 
 * Copyright (c) 2020-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Core;
using Spark.Engine.Extensions;
using System;
using System.Net;

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

    [TestMethod]
    public void AddAllInnerErrors_Sets_IssueType_From_StatusCode_For_SparkException()
    {
        var outcome = new OperationOutcome().AddAllInnerErrors(Error.BadRequest("Body parsing failed"));

        Assert.AreEqual(1, outcome.Issue.Count);
        Assert.AreEqual(OperationOutcome.IssueType.Invalid, outcome.Issue[0].Code);
    }

    [TestMethod]
    public void AddAllInnerErrors_Sets_IssueType_Exception_For_Other_Exceptions()
    {
        var outcome = new OperationOutcome().AddAllInnerErrors(new InvalidOperationException("Something failed"));

        Assert.AreEqual(1, outcome.Issue.Count);
        Assert.AreEqual(OperationOutcome.IssueType.Exception, outcome.Issue[0].Code);
    }

    [TestMethod]
    public void AddError_With_Message_Sets_IssueType_Processing()
    {
        var outcome = new OperationOutcome().AddError("Something failed");

        Assert.AreEqual(1, outcome.Issue.Count);
        Assert.AreEqual(OperationOutcome.IssueType.Processing, outcome.Issue[0].Code);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest, OperationOutcome.IssueType.Invalid)]
    [DataRow(HttpStatusCode.Forbidden, OperationOutcome.IssueType.Forbidden)]
    [DataRow(HttpStatusCode.NotFound, OperationOutcome.IssueType.NotFound)]
    [DataRow(HttpStatusCode.MethodNotAllowed, OperationOutcome.IssueType.NotSupported)]
    [DataRow(HttpStatusCode.NotAcceptable, OperationOutcome.IssueType.NotSupported)]
    [DataRow(HttpStatusCode.UnsupportedMediaType, OperationOutcome.IssueType.NotSupported)]
    [DataRow(HttpStatusCode.NotImplemented, OperationOutcome.IssueType.NotSupported)]
    [DataRow(HttpStatusCode.Conflict, OperationOutcome.IssueType.Conflict)]
    [DataRow(HttpStatusCode.PreconditionFailed, OperationOutcome.IssueType.Conflict)]
    [DataRow(HttpStatusCode.Gone, OperationOutcome.IssueType.Processing)]
    [DataRow(HttpStatusCode.ServiceUnavailable, OperationOutcome.IssueType.Transient)]
    [DataRow(HttpStatusCode.InternalServerError, OperationOutcome.IssueType.Exception)]
    public void IssueType_Is_Mapped_From_HttpStatusCode(HttpStatusCode code, OperationOutcome.IssueType expected)
    {
        Assert.AreEqual(expected, OperationOutcomeExtensions.IssueTypeOf(code));
    }
}
