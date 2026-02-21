# ADR-0004: Background IndexService via Durable Outbox Queue

## Status
Accepted

## Context
`IndexService.ProcessAsync()` is currently called synchronously inside the HTTP request
path, triggered via `ServiceListener.InformAsync()` after a FHIR resource has been stored.
This means every write request must wait for search indexing to complete before returning
a response to the client.

ADR 0001 identified the need for transactional atomicity across the `resources`, `counters`,
and `searchindex` MongoDB collections. It also noted that running the `IndexService`
independently in a background service would be architecturally desirable.

Two problems motivate decoupling `IndexService` from the request path:

1. **Atomicity:** To achieve the transactional guarantees described in ADR 0001, the
   `searchindex` write must either be inside the same MongoDB transaction as `resources` and
   `counters`, or be deferred to a background worker that is guaranteed not to lose work.
   Including `searchindex` inside the MongoDB transaction is possible but undesirable: it
   increases transaction duration, holds locks longer, and ties search availability to the
   success of the resource write.

2. **Latency:** Indexing can be computationally expensive for large resources. Removing it
   from the synchronous path reduces write latency for clients.

The search index is used only for read operations (search queries). A brief period of
eventual consistency between a write and its appearance in search results is acceptable.

## Discussion

### Alternatives Considered

1. **Keep IndexService synchronous, inside the MongoDB transaction**
   - Pros: Strongly consistent — search index is always up to date at commit time.
   - Cons: Increases transaction duration and lock contention; couples search availability
     to the write transaction; degrades write latency; contradicts the ADR 0001 intent of
     keeping the transaction focused on the two critical collections.

2. **Keep IndexService synchronous, outside the transaction (current state)**
   - Pros: No new infrastructure needed.
   - Cons: Index write is not atomic with the resource write. A crash between commit and
     indexing leaves the search index permanently out of sync with no recovery path.

3. **In-memory queue (`System.Threading.Channels`)**
   - Pros: Simple; no extra MongoDB collection; very low overhead.
   - Cons: Not durable — pending index work is lost on application restart or crash.
     A node crash after committing a resource but before processing the channel entry
     leaves the search index permanently stale.

4. **MongoDB Change Streams on the `resources` collection**
   - Pros: No explicit enqueue step; automatically triggered by any write to `resources`.
   - Cons: Requires a MongoDB replica set with an oplog; resume token management adds
     complexity; does not integrate naturally with the transactional outbox pattern needed
     for ADR 0001; harder to test.

5. **Durable outbox queue in MongoDB (`indexqueue` collection)** *(chosen)*
   - Pros: Durable across restarts; integrates cleanly with the ADR 0001 transaction (the
     outbox entry is written inside the same transaction as `resources` + `counters`);
     supports multiple Spark nodes safely via atomic claim; straightforward to test and
     monitor.
   - Cons: Introduces a new MongoDB collection and a background service; search index
     becomes eventually consistent (accepted trade-off).

## Decision

We will decouple `IndexService` from the synchronous request path by introducing a durable
outbox queue backed by a MongoDB `indexqueue` collection and a background worker.

- A new `IIndexQueue` interface provides `EnqueueAsync`, `ClaimNextAsync`,
  `AcknowledgeAsync`, and `NackAsync` operations.
- `MongoIndexQueue` implements `IIndexQueue` using the `indexqueue` collection.
- `EnqueueAsync` is called from a new `IndexQueueEnqueueListener : IServiceListener`,
  which participates in the existing `ServiceListener` chain. This preserves the
  `IServiceListener` pattern and allows operators to switch between synchronous indexing
  (`SearchService`) and background indexing (`IndexQueueEnqueueListener`) by swapping a
  single DI registration, without modifying any storage code.
- The existing `ServiceListener.InformAsync()` call and the `IServiceListener` /
  `SearchService` wiring are fully preserved. `IndexQueueEnqueueListener` is a drop-in
  replacement for `SearchService` in the listener registration. The DI swap to activate
  background indexing will be opt-in (controlled by a configuration flag) so operators can
  choose synchronous vs. background indexing per deployment.
- `IndexWorker : BackgroundService` runs on every Spark node and polls `ClaimNextAsync()`
  in a loop. `ClaimNextAsync()` is a single atomic `FindOneAndUpdate` that transitions an
  entry from `pending` to `processing` and stamps it with the claiming node's `WorkerId`.
  This guarantees that only one node across the cluster processes any given entry.
- Entries that remain in `processing` past a configurable lease timeout are automatically
  reclaimed by the next available worker (crash recovery).
- Failed entries are retried up to a configurable maximum; after that they transition to
  `status=failed` and are logged for operator attention.
- Once ADR 0001 is implemented, both `IndexQueueEnqueueListener` and `SearchService` will
  participate in the same MongoDB transaction as `resources` and `counters`, completing
  the transactional outbox pattern regardless of which indexing mode is active.

## Consequences

### Positive
- Search indexing is removed from the synchronous write path, reducing write latency.
- No index work is lost across application restarts or crashes.
- Lays the foundation for ADR 0001's transactional outbox pattern — `EnqueueAsync` can
  be called inside the MongoDB transaction without expanding the transaction scope.
- Naturally supports horizontal scaling: multiple Spark nodes share the same queue and
  each claims work atomically.

### Negative
- Search results become eventually consistent: a resource may not appear in search
  results immediately after a successful write.
- A new `indexqueue` MongoDB collection and a background service must be operated
  and monitored.
- Failed entries that exhaust retries require manual operator intervention.

### Risks
- If the poll interval is too long, search results lag noticeably behind writes. This
  must be tunable via configuration.
- The `indexqueue` collection can grow unboundedly if the worker is stopped for an
  extended period. Monitoring queue depth is recommended.

## References
- [ADR 0001 — Transactional behavior across resources](0001-TransactionalBehaviorAcrossResources.md)
