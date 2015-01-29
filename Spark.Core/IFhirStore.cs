/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public interface IFhirStore
    {
        // Keys
        IEnumerable<string> List(string typename, DateTimeOffset? since = null);
        IEnumerable<string> History(string typename, DateTimeOffset? since = null);
        IEnumerable<string> History(Key key, DateTimeOffset? since = null);
        IEnumerable<string> History(DateTimeOffset? since = null);

        // BundleEntries
        bool Exists(Key key);

        Entry Get(Key key);
        Entry Get(string recordid);
        IEnumerable<Entry> Get(IEnumerable<string> identifiers, string sortby);

        void Add(Entry entry);
        void Add(IEnumerable<Entry> entries);

        void Replace(Entry entry);

        // Snapshots
        void AddSnapshot(Snapshot snapshot);
        Snapshot GetSnapshot(string snapshotid);

        void Clean();
    }
}
