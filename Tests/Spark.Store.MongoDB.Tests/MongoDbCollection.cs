/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Xunit;

namespace Spark.Store.MongoDB.Tests;

[CollectionDefinition("MongoDB integration", DisableParallelization = true)]
public sealed class MongoDbCollection : ICollectionFixture<MongoDbFixture>
{
}
