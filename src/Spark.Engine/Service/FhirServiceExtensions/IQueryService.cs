﻿/* 
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using System.Collections.Generic;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    /// <summary>
    /// Use this interface on your own risk. This interface is highly likely to having breaking changes until
    /// version 2.0 of Spark.
    /// </summary>
    public interface IQueryService : IFhirServiceExtension
    {
        IAsyncEnumerable<Entry> GetAsync(string type, SearchParams searchParams);
    }
}