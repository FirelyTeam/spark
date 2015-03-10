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
        // primary keys
        IEnumerable<string> List(string typename, DateTimeOffset? since = null);
        IEnumerable<string> History(string typename, DateTimeOffset? since = null);
        IEnumerable<string> History(IKey key, DateTimeOffset? since = null);
        IEnumerable<string> History(DateTimeOffset? since = null);

        // BundleEntries
        bool Exists(IKey key);

        Interaction Get(IKey key);
        Interaction Get(string primarykey);
        IEnumerable<Interaction> Get(IEnumerable<string> identifiers, string sortby);

        void Add(Interaction entry);
        void Add(IEnumerable<Interaction> entries);

        void Replace(Interaction entry);

        void Clean();
    }
}
