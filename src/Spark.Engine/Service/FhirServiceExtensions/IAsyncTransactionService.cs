/*
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using Hl7.Fhir.Model;
using Spark.Engine.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IAsyncTransactionService : IFhirServiceExtension
    {
        Task<FhirResponse> HandleTransactionAsync(ResourceManipulationOperation operation, IInteractionHandler interactionHandler);
        Task<IList<Tuple<Entry, FhirResponse>>> HandleTransactionAsync(Bundle bundle, IInteractionHandler interactionHandler);
        Task<IList<Tuple<Entry, FhirResponse>>> HandleTransactionAsync(IList<Entry> interactions, IInteractionHandler interactionHandler);
    }
}
