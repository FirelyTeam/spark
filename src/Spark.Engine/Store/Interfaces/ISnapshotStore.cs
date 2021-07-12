/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System.Threading.Tasks;
using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    public interface ISnapshotStore
    {
        void AddSnapshot(Snapshot snapshot);
        Task AddSnapshotAsync(Snapshot snapshot);

        Snapshot GetSnapshot(string snapshotId);
        Task<Snapshot> GetSnapshotAsync(string snapshotId);
    }
}
