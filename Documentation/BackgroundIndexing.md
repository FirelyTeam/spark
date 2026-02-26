# Background Indexing

> [!IMPORTANT]
> **Experimental feature — use with caution.**
> Background indexing is not yet recommended for production use. It introduces eventual
> consistency between writes and search results, which may cause unexpected behaviour in
> deployments that rely on immediate read-your-writes consistency on the search index.

## Overview

By default Spark processes search index updates synchronously in the HTTP request path:
every write waits for indexing to complete before returning a response. Background indexing
decouples this work by enqueuing index updates in a durable MongoDB `indexqueue` collection
and processing them in a `BackgroundService` worker (`IndexWorker`) that runs on each Spark
node.

The main benefit is reduced write latency. The trade-off is that search results are
eventually consistent — a resource may not appear in search results immediately after a
successful write.

## Configuration

Background indexing is controlled by the `IndexingMode` property of the `Experimental`
settings group in `SparkSettings`:

```json
{
  "SparkSettings": {
    "Experimental": {
      "IndexingMode": "Background"
    }
  }
}
```

Or via environment variable (useful for Docker / Kubernetes deployments):

```
SparkSettings__Experimental__IndexingMode=Background
```

The default value is `Synchronous`. Set it to `Background` to enable background indexing.

The index queue behaviour can be tuned via `StoreSettings.IndexQueue`:

```json
{
  "StoreSettings": {
    "IndexQueue": {
      "PollInterval": "00:00:00.020",
      "LeaseTimeout": "00:05:00",
      "MaxAttempts": 5
    }
  }
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `PollInterval` | 20 ms | How often `IndexWorker` polls for new queue entries. |
| `LeaseTimeout` | 5 min | How long a claimed entry may be held before it is reclaimed by another worker (crash recovery). |
| `MaxAttempts` | 5 | Maximum number of processing attempts before an entry is marked `failed`. |

## Eventual Consistency Trade-offs

Operators must evaluate the following before enabling background indexing:

- **Conditional writes** — `conditional create`, `conditional update`, and `conditional delete`
  resolve their match criteria via the search index. If a conflicting resource was written
  within the last index-poll cycle, the condition may resolve against a stale index.
- **Search immediately after write** — clients that write a resource and immediately search
  for it may transiently receive zero or incomplete results.
- **Automated test suites** — any test that asserts search result counts or presence
  immediately after a write must either add retry/polling logic or be run against a
  `Synchronous`-mode instance.

## Multi-node Deployments

Each Spark node runs its own `IndexWorker`. Claims are atomic (`FindOneAndUpdate`) so only
one node processes any given queue entry. If a node crashes mid-processing, the entry is
automatically reclaimed after `LeaseTimeout` by another node.

## Monitoring

- Entries that exhaust `MaxAttempts` are marked `status=failed` in the `indexqueue`
  collection and logged by `IndexWorker`. Operator intervention is required to re-process
  or discard them.
- Monitor the depth of the `indexqueue` collection. A growing queue indicates that the
  worker cannot keep up with the write rate, or that the worker is stopped.

## See Also

- [ADR 0004 — Background IndexService via Durable Outbox Queue](ADR/0004-BackgroundIndexService.md)
