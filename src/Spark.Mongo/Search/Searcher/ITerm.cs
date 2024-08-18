/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Spark.Mongo.Search.Common;

namespace Spark.Search.Mongo
{
    public interface ITerm
    {
        string Resource { get; set; }
        string Field { get; set; }
        string Operator { get; set; }
        string Value { get; set; }
        Argument Argument { get; set; }
    }
}