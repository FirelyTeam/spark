/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    public interface IFhirStore
    {
        void Add(Entry entry);
        Task AddAsync(Entry entry);

        Entry Get(IKey key);
        Task<Entry> GetAsync(IKey key);

        IList<Entry> Get(IEnumerable<IKey> localIdentifiers, IEnumerable<string> elements = null);
        Task<IList<Entry>> GetAsync(IEnumerable<IKey> localIdentifiers, IEnumerable<string> elements = null);
    }
}