# Database Migrations

Spark records database schema and index migrations in MongoDB. Migration state is stored in the `schema_migrations`
collection and is refreshed when the application starts and every 30 seconds afterward.

## Migration State

Search-index query behavior is derived from the highest consecutive migration version recorded in
`schema_migrations`.

| Database version | Migration state | Query behavior |
| --- | --- | --- |
| `0` | `None` | Supports legacy plain-string token values and scalar token/quantity/reference values. |
| `1` | `StructuredStringTokenIndex` | Removes the legacy plain-string token branch but retains scalar compatibility branches. |
| `2` | `StructuredStringTokenIndex` + `TokenQuantityAndReferenceArrayIndex` | Uses array-based `$elemMatch` queries for token, quantity, and reference-identifier values. |

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

Migration 1 is recorded as version `1` with the name `structured-string-token-index`.

## Token, Quantity, and Reference Array Index

Migration 2, `token-quantity-and-reference-array-index`, normalizes parameter-level values to BSON arrays. This
includes token and quantity values, even when there is only one value, and reference values because
`reference:identifier` searches use the token query implementation.

For example, a token and quantity value are stored in these shapes:

```json
{
  "identifier": [
    {
      "code": "12345",
      "system": "urn:oid:1.2.36.146.595.217.0.1"
    }
  ],
  "value-quantity": [
    {
      "system": "http://unitsofmeasure.org",
      "value": 2.0,
      "decimals": "2",
      "unit": "mmol"
    }
  ]
}
```

Migration 2 is recorded as version `2` with the name `token-quantity-and-reference-array-index`.

Before version 2 is recorded, MongoDB queries include compatibility branches for scalar index values. After version 2
is recorded, token, quantity, and reference-identifier queries use only array-based `$elemMatch` branches. The scalar
compatibility code remains in the library for existing major-version compatibility and is disabled through migration
state rather than removed.

## Existing Databases

Use the following procedure when migrating an existing database:

1. Deploy the migration-aware Spark version to every Spark instance using the database.
2. Pause writes externally across the entire cluster.
3. Set `ClearIndexOnRebuild=true`.
4. Run one clean index rebuild using the Admin UI or `IndexRebuildService`.
5. Monitor the rebuild and confirm that no ordinary resource-indexing failures are reported. Resources that raise
   `CodedValidationException` are skipped with a warning and do not block migration recording.
6. If the database is at version `0`, verify that migrations `1` and `2` are recorded in order. If the database is at
   version `1`, verify that migration `2` is recorded.
7. Verify the migration names in `schema_migrations`:
   - version `1`: `structured-string-token-index`;
   - version `2`: `token-quantity-and-reference-array-index`.
8. Resume writes.

The maintenance lock is process-local and does not coordinate writes across multiple Spark instances. Writes must
therefore be paused by the deployment before the rebuild starts.

Pending migrations are recorded only after the clean rebuild completes without ordinary indexing failures. If the
rebuild fails, the pending migration remains unapplied. Resolve the failure and run the clean rebuild again before
resuming normal operation.

## Fresh Databases

When both the resource and search-index collections are empty, Spark records all current migrations automatically during
startup, so a fresh database reaches migration version `2`. An unversioned database containing resources or search-index
documents remains at migration version `0` and requires the existing-database procedure above.

## Later Re-indexing

After migration 2 has been recorded, ordinary re-indexing may use `ClearIndexOnRebuild=false`. Spark can replace the
search-index document for the same resource version safely. This does not replace the clean-rebuild requirement when
transitioning an existing database to a new migration.
