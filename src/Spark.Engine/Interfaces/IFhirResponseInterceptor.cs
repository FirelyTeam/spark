/* 
 * Copyright (c) 2015-2018, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
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