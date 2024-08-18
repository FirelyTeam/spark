/* 
 * Copyright (c) 2016-2018, Firely (info@fire.ly)
 * Copyright (c) 2020-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Threading.Tasks;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store
{
    public class MongoSnapshotStore : ISnapshotStore
    {
        private readonly IMongoDatabase _database;

        public MongoSnapshotStore(string mongoUrl)
        {
            _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        }

        public async Task AddSnapshotAsync(Snapshot snapshot)
        {
            var collection = _database.GetCollection<Snapshot>(Collection.SNAPSHOT);
            await collection.InsertOneAsync(snapshot).ConfigureAwait(false);
        }

        public async Task<Snapshot> GetSnapshotAsync(string snapshotId)
        {
            var collection = _database.GetCollection<Snapshot>(Collection.SNAPSHOT);
            return await collection.Find(s => s.Id == snapshotId)
                .FirstOrDefaultAsync();
        }
    }
}