/* 
 * Copyright (c) 2016-2018, Firely (info@fire.ly)
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;
using System.Threading.Tasks;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IInteractionHandler
    {
        Task<FhirResponse> HandleInteractionAsync(Entry interaction);
    }
}