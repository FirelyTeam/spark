/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;

namespace Spark.Engine.Interfaces
{
    public interface IFhirResponseInterceptor
    {
        FhirResponse GetFhirResponse(Entry entry, object input);

        bool CanHandle(object input);
    }
}