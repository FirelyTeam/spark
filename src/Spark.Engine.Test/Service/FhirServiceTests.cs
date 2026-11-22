/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Moq;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Service;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Service;
using System;
using System.Collections.Generic;
using Xunit;

namespace Spark.Engine.Test.Service;

public class FhirServiceTests
{
    [Theory]
    [InlineData(Bundle.BundleType.Batch, Bundle.BundleType.BatchResponse)]
    [InlineData(Bundle.BundleType.Transaction, Bundle.BundleType.TransactionResponse)]
    public async System.Threading.Tasks.Task TransactionAsync_Returns_Response_Bundle_Type_Matching_Request(Bundle.BundleType requestType, Bundle.BundleType expectedResponseType)
    {
        var bundle = new Bundle { Type = requestType };
        var responses = new List<Tuple<Entry, FhirResponse>>();

        var transactionService = new Mock<ITransactionService>();
        transactionService
            .Setup(s => s.HandleTransactionAsync(bundle, It.IsAny<IInteractionHandler>()))
            .ReturnsAsync(responses);
        var responseFactory = new Mock<IFhirResponseFactory>();

        var fhirService = new FhirService(new IFhirServiceExtension[] { transactionService.Object }, responseFactory.Object);

        await fhirService.TransactionAsync(bundle);

        responseFactory.Verify(f => f.GetFhirResponse(responses, expectedResponseType), Times.Once);
    }
}
