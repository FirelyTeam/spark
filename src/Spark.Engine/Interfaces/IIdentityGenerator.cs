/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;

namespace Spark.Core
{
    public interface IIdentityGenerator
    {
        string NextResourceId(Resource resource);
        string NextVersionId(string resourceIdentifier);
        string NextVersionId(string resourceType, string resourceIdentifier);
    }
}
