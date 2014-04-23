/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Spark.Core;
using Hl7.Fhir.Model;

namespace Spark.Search
{
    public interface ISearcher
    {
        SearchResults Search(Parameters parameters);
        SearchResults Search(Query query);
    }
}