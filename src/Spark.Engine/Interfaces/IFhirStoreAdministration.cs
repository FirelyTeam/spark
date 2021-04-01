/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Threading.Tasks;

namespace Spark.Engine.Interfaces
{
    public interface IFhirStoreAdministration
    {
        [Obsolete("Use Async method version instead")]
        void Clean();
        Task CleanAsync();
    }
}

