/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly)
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
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
