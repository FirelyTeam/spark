/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using Spark.Engine.Core;

namespace Spark.Core
{
    public interface IFhirStore
    {
        // primary keys
        IList<string> List(string typename, DateTimeOffset? since = null);
        IList<string> History(string typename, DateTimeOffset? since = null);
        IList<string> History(IKey key, DateTimeOffset? since = null);
        IList<string> History(DateTimeOffset? since = null);

        // BundleEntries
        bool Exists(IKey key);

        Entry Get(IKey key);
        IList<Entry> Get(IEnumerable<string> identifiers, string sortby);
        IList<Entry> GetCurrent(IEnumerable<string> identifiers, string sortby);

        void Add(Entry entry);
        void Add(IEnumerable<Entry> entries);

        void Replace(Entry entry);

        void Clean();
    }

    

}

