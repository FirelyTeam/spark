## Migrate from 1.0 to 2.0

### New IGenerator implementation
MongoIdGenerator is replaced by GuidGenerator and is the new default identity generator for resource's.
See issue https://github.com/FirelyTeam/spark/issues/572 for more information.

### Rebuild indexes
Better resolve handling for search parameters of type reference has been added, for this to take effect on existing
resources the index has to be rebuilt. This can be done by by using the Admin UI that comes with Spark.Web or wire up
IndexRebuildService in your implementation.
