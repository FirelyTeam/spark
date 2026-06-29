/*
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;

namespace Spark.Store.MongoDB;

public class MongoSnapshotStore : ISnapshotStore2
{
    public const int SNAPSHOT_KEY_LIMIT = 1000;

    private readonly IMongoDatabase _database;

    public MongoSnapshotStore(string mongoUrl)
    {
        _database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
    }

    public async Task AddSnapshotAsync(Snapshot snapshot)
    {
        var collection = _database.GetCollection<Snapshot>(Collection.SNAPSHOT);
        var snapshots = snapshot.Split(SNAPSHOT_KEY_LIMIT);
        if (snapshots.Count == 1)
        {
            await collection.InsertOneAsync(snapshots[0]).ConfigureAwait(false);
            return;
        }

        await collection.InsertManyAsync(snapshots).ConfigureAwait(false);
    }

    public async Task<Snapshot> GetSnapshotAsync(string snapshotId)
    {
        var collection = _database.GetCollection<Snapshot>(Collection.SNAPSHOT);
        var snapshot = await collection.Find(s => s.Id == snapshotId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
        if (snapshot != null)
            return snapshot;

        var chunks = await collection.Find(s => s.GroupId == snapshotId)
            .SortBy(s => s.StartIndex)
            .ToListAsync()
            .ConfigureAwait(false);

        return Snapshot.CreateWindow(snapshotId, chunks);
    }

    public async Task<Snapshot> GetSnapshotAsync(string snapshotId, int offset)
    {
        var collection = _database.GetCollection<Snapshot>(Collection.SNAPSHOT);
        var snapshot = await collection.Find(s => s.Id == snapshotId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
        if (snapshot != null)
            return snapshot;

        var firstChunk = await collection.Find(s => s.GroupId == snapshotId)
            .SortBy(s => s.StartIndex)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
        if (firstChunk == null)
            return null;

        if (offset >= firstChunk.Count)
            return Snapshot.CreateWindow(snapshotId, [firstChunk]);

        var chunkAtOffset = await collection.Find(s => s.GroupId == snapshotId && s.StartIndex <= offset)
            .SortByDescending(s => s.StartIndex)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        var startIndex = chunkAtOffset?.StartIndex ?? firstChunk.StartIndex;
        var endIndex = offset + GetChunkWindowSize(firstChunk);
        var chunks = await collection.Find(s =>
                s.GroupId == snapshotId &&
                s.StartIndex >= startIndex &&
                s.StartIndex < endIndex)
            .SortBy(s => s.StartIndex)
            .ToListAsync()
            .ConfigureAwait(false);

        return Snapshot.CreateWindow(snapshotId, FilterOverlappingChunks(chunks, offset, endIndex));
    }

    private static int GetChunkWindowSize(Snapshot snapshot)
    {
        return snapshot.GetPageSize() == 0 ? 1 : snapshot.GetPageSize();
    }

    private static IReadOnlyList<Snapshot> FilterOverlappingChunks(IEnumerable<Snapshot> chunks, int offset, int endIndex)
    {
        return chunks
            .Where(chunk => chunk.StartIndex < endIndex && chunk.StartIndex + chunk.KeyCount > offset)
            .ToList();
    }
}
