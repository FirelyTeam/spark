/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Store;
using Spark.Engine.Store.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Spark.Store.MongoDB;

public sealed class DatabaseMigrationService : IDatabaseMigrationService
{
    private const string NameField = "name";
    private const string CompletedAtField = "completedAt";

    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private IReadOnlyDictionary<int, string> _appliedMigrations = new Dictionary<int, string>();
    private int _currentVersion;

    public DatabaseMigrationService(string connectionString)
        : this(MongoDatabaseFactory.GetMongoDatabase(connectionString))
    {
    }

    internal DatabaseMigrationService(IMongoDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _collection = database.GetCollection<BsonDocument>(Collection.SchemaMigrations);
    }

    public int CurrentVersion => Volatile.Read(ref _currentVersion);

    public bool IsApplied(int version) => version > 0 && version <= CurrentVersion;

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            List<BsonDocument> documents = await _collection
                .Find(FilterDefinition<BsonDocument>.Empty)
                .Sort(Builders<BsonDocument>.Sort.Ascending(Field.PRIMARYKEY))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var appliedMigrations = new Dictionary<int, string>(documents.Count);
            var expectedVersion = 1;

            foreach (BsonDocument document in documents)
            {
                (int version, string name) = ReadMigration(document);
                if (version != expectedVersion)
                {
                    throw new DatabaseMigrationException(
                        $"Expected database migration version {expectedVersion}, but found version {version}."
                    );
                }

                appliedMigrations.Add(version, name);
                expectedVersion++;
            }

            PublishState(appliedMigrations, expectedVersion - 1);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task RecordCompletedAsync(
        DatabaseMigration migration,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(migration);

        ValidateMigration(migration);

        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (migration.Version <= _currentVersion)
            {
                if (_appliedMigrations.TryGetValue(migration.Version, out string cachedName) &&
                    string.Equals(cachedName, migration.Name, StringComparison.Ordinal))
                {
                    return;
                }

                throw new DatabaseMigrationException(
                    $"Database migration version {migration.Version} is already recorded with a different name."
                );
            }

            int expectedVersion = _currentVersion + 1;
            if (migration.Version != expectedVersion)
            {
                throw new DatabaseMigrationException(
                    $"Expected database migration version {expectedVersion}, but received version {migration.Version}."
                );
            }

            BsonDocument persistedMigration;
            try
            {
                persistedMigration = await UpsertMigrationAsync(migration, cancellationToken).ConfigureAwait(false);
            }
            catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000)
            {
                throw CreateConflictException(migration, exception);
            }
            catch (MongoCommandException exception) when (exception.Code == 11000)
            {
                throw CreateConflictException(migration, exception);
            }

            (_, string persistedName) = ReadMigration(persistedMigration);
            if (!string.Equals(persistedName, migration.Name, StringComparison.Ordinal))
            {
                throw new DatabaseMigrationException(
                    $"Database migration version {migration.Version} is already recorded as '{persistedName}', " +
                    $"not '{migration.Name}'."
                );
            }

            var appliedMigrations = new Dictionary<int, string>(_appliedMigrations)
            {
                [migration.Version] = migration.Name
            };
            PublishState(appliedMigrations, migration.Version);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private async Task<BsonDocument> UpsertMigrationAsync(
        DatabaseMigration migration,
        CancellationToken cancellationToken
    )
    {
        // If version 1 already exists with a different name the attempted upsert will collide, RecordCompletedAsync
        // will then convert that duplicate-key error into a DatabaseMigrationException.
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter
            .And(
                Builders<BsonDocument>.Filter.Eq(Field.PRIMARYKEY, migration.Version),
                Builders<BsonDocument>.Filter.Eq(NameField, migration.Name)
            );

        PipelineDefinition<BsonDocument, BsonDocument> pipeline = new[]
        {
            new BsonDocument(
                "$set",
                new BsonDocument(
                    CompletedAtField,
                    new BsonDocument("$ifNull", new BsonArray { $"${CompletedAtField}", "$$NOW" })
                )
            )
        };

        FindOneAndUpdateOptions<BsonDocument> options = new()
        {
            IsUpsert = true, ReturnDocument = ReturnDocument.After
        };

        return await _collection
            .FindOneAndUpdateAsync(filter, pipeline, options, cancellationToken)
            .ConfigureAwait(false);
    }

    private static (int Version, string Name) ReadMigration(BsonDocument document)
    {
        if (!document.TryGetValue(Field.PRIMARYKEY, out BsonValue versionValue) || !versionValue.IsInt32
            || versionValue.AsInt32 <= 0)
        {
            throw new DatabaseMigrationException("A persisted database migration has an invalid version.");
        }

        if (!document.TryGetValue(NameField, out BsonValue nameValue) || !nameValue.IsString
            || string.IsNullOrWhiteSpace(nameValue.AsString))
        {
            throw new DatabaseMigrationException(
                $"Persisted database migration version {versionValue.AsInt32} has an invalid name."
            );
        }

        if (!document.TryGetValue(CompletedAtField, out BsonValue completedAtValue) || !completedAtValue.IsBsonDateTime)
        {
            throw new DatabaseMigrationException(
                $"Persisted database migration version {versionValue.AsInt32} has an invalid completion timestamp."
            );
        }

        return (versionValue.AsInt32, nameValue.AsString);
    }

    private static void ValidateMigration(DatabaseMigration migration)
    {
        if (migration.Version <= 0)
            throw new DatabaseMigrationException("A database migration version must be greater than zero.");
        if (string.IsNullOrWhiteSpace(migration.Name))
            throw new DatabaseMigrationException("A database migration name must not be empty.");
    }

    private void PublishState(IReadOnlyDictionary<int, string> appliedMigrations, int currentVersion)
    {
        _appliedMigrations = appliedMigrations;
        Volatile.Write(ref _currentVersion, currentVersion);
    }

    private static DatabaseMigrationException CreateConflictException(
        DatabaseMigration migration,
        Exception innerException)
    {
        return new DatabaseMigrationException(
            $"Database migration version {migration.Version} is already recorded with a different name.",
            innerException
        );
    }
}
