/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Collections.Generic;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Core
{

    public interface IFhirIndex
    {
        void Clean();
        void Process(IEnumerable<Entry> entries);
        void Process(Entry entry);
        SearchResults Search(string resource, SearchParams searchCommand);
        Key FindSingle(string resource, SearchParams searchCommand);
        SearchResults GetReverseIncludes(IList<IKey> keys, IList<string> revIncludes);

    }

}
