/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spark.Search
{
    public interface IIndexer
    {
        void Put(Resource entry);
        void Put(IEnumerable<Resource> entries);
        void Delete(Key key);
        void Delete(IEnumerable<DeletedEntry> entries);
        void Clean();
    }
}