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
        IEnumerable<Uri> List(string resource, DateTimeOffset? since = null);
        IEnumerable<Uri> History(string resource, DateTimeOffset? since = null);
        IEnumerable<Uri> History(Uri key, DateTimeOffset? since = null);
        IEnumerable<Uri> History(DateTimeOffset? since = null);

        // BundleEntries
        bool Exists(Uri key);

        BundleEntry Get(Uri key);
        IEnumerable<BundleEntry> Get(IEnumerable<Uri> keys, string sortby);

        void Add(BundleEntry entry);
        void Add(IEnumerable<BundleEntry> entries);

        void Replace(BundleEntry entry);

        // Snapshots
        void AddSnapshot(Snapshot snapshot);
        Snapshot GetSnapshot(string key);

        void Clean();
    }
}
