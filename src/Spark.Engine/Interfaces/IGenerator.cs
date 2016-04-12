/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */


using Spark.Engine.Core;

namespace Spark.Core
{

    public interface IGenerator
    {
        string NextResourceId(string resource);
        string NextVersionId(string resourceIdentifier);
        bool CustomResourceIdAllowed(string value);
        string NextVersionId(string resourceType, string resourceIdentifier);
    }

    

}
