﻿/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * Copyright (c) 2021-2023, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
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