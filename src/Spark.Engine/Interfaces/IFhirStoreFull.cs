/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Engine.Interfaces
{
    public interface IFhirStoreFull
    {
        void Add(Entry entry);
        Entry Get(IKey key);
        IList<Entry> Get(IEnumerable<string> identifiers, string sortby = null);

        // primary keys
        IList<string> List(string typename, DateTimeOffset? since = null);
        IList<string> History(string typename, DateTimeOffset? since = null);
        IList<string> History(IKey key, DateTimeOffset? since = null);
        IList<string> History(DateTimeOffset? since = null);

        // BundleEntries
        bool Exists(IKey key);

        IList<Entry> GetCurrent(IEnumerable<string> identifiers, string sortby = null);

        void Add(IEnumerable<Entry> entries);

        void Replace(Entry entry);

     
    }

    public interface IFhirStoreAdministration
    {
        void Clean();
    }




}

