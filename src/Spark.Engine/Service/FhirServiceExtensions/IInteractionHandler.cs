/*
 * Copyright (c) 2021-2023, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
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