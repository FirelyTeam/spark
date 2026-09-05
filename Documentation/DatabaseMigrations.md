# Database Migrations

Spark records database schema and index migrations in MongoDB. Migration state is stored in the `schema_migrations`
collection and is refreshed when the application starts and every 30 seconds afterward.

## Structured String Token Index

Migration 1, `structured-string-token-index`, changes string-valued token indexes from a scalar value:

```json
{
  "contenttype": "application/hl7-v3+xml"
}
```

to the structured token representation:

```json
{
  "contenttype": {
    "code": "application/hl7-v3+xml"
  }
}
```

## Existing Databases

Use the following procedure when migrating an existing database:

1. Deploy the migration-aware Spark version to every Spark instance using the database.
2. Pause writes externally across the entire cluster.
3. Set `ClearIndexOnRebuild=true`.
4. Run one clean index rebuild using the Admin UI or `IIndexRebuildService`.
5. Monitor the rebuild and confirm that no resource-indexing failures are reported.
6. Verify that the `schema_migrations` collection contains migration version `1` with the name
   `structured-string-token-index`.
7. Resume writes.

The maintenance lock is process-local and does not coordinate writes across multiple Spark instances. Writes must
therefore be paused by the deployment before the rebuild starts.

Migration 1 is recorded only after the clean rebuild completes without indexing failures. If the rebuild fails, resolve
the failure and run the clean rebuild again before resuming normal operation.

## Fresh Databases

When both the resource and search-index collections are empty, Spark records the current migration automatically during
startup. An unversioned database containing resources or search-index documents remains at migration version 0 and
requires the existing-database procedure above.

## Later Re-indexing

After migration 1 has been recorded, ordinary re-indexing may use `ClearIndexOnRebuild=false`. Spark can replace the
search-index document for the same resource version safely. This does not replace the clean-rebuild requirement when
transitioning an existing database to a new migration.
