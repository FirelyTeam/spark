/* 
 * Copyright (c) 2015-2018, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Collections.Generic;
using Spark.Engine.Core;
using Spark.Engine.Service;

namespace Spark.Engine.Interfaces
{
    public interface IFhirResponseInterceptorRunner
    {
        void AddInterceptor(IFhirResponseInterceptor interceptor);
        void ClearInterceptors();
        FhirResponse RunInterceptors(Entry entry, IEnumerable<object> parameters);
    }
}