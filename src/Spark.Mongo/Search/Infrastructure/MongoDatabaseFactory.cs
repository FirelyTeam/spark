/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using MongoDB.Driver;
using System.Collections.Concurrent;

namespace Spark.Store.Mongo;

public static class MongoDatabaseFactory
{
    private static readonly ConcurrentDictionary<string, IMongoDatabase> _instances = new();

    public static IMongoDatabase GetMongoDatabase(string url)
    {
        return _instances.GetOrAdd(url, CreateMongoDatabase);
    }

    private static IMongoDatabase CreateMongoDatabase(string url)
    {
        var mongourl = new MongoUrl(url);
        var client = new MongoClient(mongourl);
        return client.GetDatabase(mongourl.DatabaseName);
    }
}
