/*
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */


using Hl7.Fhir.Model;

namespace Spark.Core
{
    using System.Threading.Tasks;

    public interface IGenerator
    {
        Task<string> NextResourceId(Resource resource);
        Task<string> NextVersionId(string resourceIdentifier);
        Task<string> NextVersionId(string resourceType, string resourceIdentifier);
    }
}
