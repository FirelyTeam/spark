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
    public interface IGenerator
    {
        string NextResourceId(Resource resource);
        string NextVersionId(string resourceIdentifier);
        string NextVersionId(string resourceType, string resourceIdentifier);
    }
}
