/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;

namespace Spark.Store.Mongo
{
    public class MongoFhirStore : IFhirStore
    {
        private readonly IAsyncFhirStore _asyncFhirStore;

        public MongoFhirStore(IAsyncFhirStore asyncFhirStore)
        {
            _asyncFhirStore = asyncFhirStore ?? throw new ArgumentNullException(nameof(asyncFhirStore));
        }

        [Obsolete("Use AddAsync(Entry) instead")]
        public void Add(Entry entry)
        {
            Task.Run(() => _asyncFhirStore.AddAsync(entry)).GetAwaiter().GetResult();
        }

        [Obsolete("Use GetAsync(IKey) instead")]
        public Entry Get(IKey key)
        {
            return Task.Run(() => _asyncFhirStore.GetAsync(key)).GetAwaiter().GetResult();
        }

        [Obsolete("Use GetAsync(IEnumerable<IKey>) instead")]
        public IList<Entry> Get(IEnumerable<IKey> localIdentifiers)
        {
            return Task.Run(() => _asyncFhirStore.GetAsync(localIdentifiers)).GetAwaiter().GetResult();
        }
    }
}
