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
using Hl7.Fhir.Model;
using Spark.Core;

namespace Spark.Search
{
    public interface IIndexer
    {
        void Process(Entry entry);
        void Process(IEnumerable<Entry> entries);
        void Clean();
    }

}