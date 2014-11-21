/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Spark.Core
{
    public interface IFhirIndex
    {
        void Clean();
        void Delete(DeletedEntry entry);
        void Process(Bundle bundle);
        void Process(BundleEntry entry);
        void Process(IEnumerable<BundleEntry> bundle);
        SearchResults Search(string resource, IEnumerable<Tuple<string, string>> collection);
        SearchResults Search(string resourcename, string query = "");
        SearchResults Search(Query query);
    }
}
