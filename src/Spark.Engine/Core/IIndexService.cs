/* 
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using Hl7.Fhir.Model;
using Spark.Engine.Model;

namespace Spark.Engine.Core
{
    public interface IIndexService
    {
        void Process(Entry entry);

        IndexValue IndexResource(Resource resource, IKey key);
    }
}
