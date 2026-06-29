# ADR-0005: Chunked Snapshot Storage

## Status
Accepted

## Context
Search and history paging store snapshot state in the MongoDB `snapshots` collection.
A snapshot contains the ordered keys found by the search, plus include, revinclude, and
paging metadata. For large result sets, the `Keys` array can make a single snapshot
document exceed MongoDB's document size limit.

Snapshot links already expose a single `_snapshot` value and an `_offset` value. The
public `_snapshot` value must remain stable while Spark resolves the requested offset to
the correct stored keys.

Existing public interfaces must remain source-compatible until the next major version.

## Discussion

### Alternatives Considered

1. **Increase the MongoDB document limit**
   - Pros: No application change.
   - Cons: MongoDB's BSON document size limit is fixed at 16MB and cannot solve this problem.

2. **Store chunks with unique document ids and a shared group id** *(chosen)*
   - Pros: Avoids the MongoDB document limit, keeps each document addressable, preserves
     the existing public `_snapshot` token, and supports page lookup by offset.
   - Cons: Snapshot reads need chunk selection and in-memory rehydration before paging.

## Decision
Split large snapshots into multiple MongoDB documents.

- Each chunk document has a unique `Id`.
- All chunks for one logical snapshot share `SnapshotGroupId`.
- The public `_snapshot` URL parameter continues to use the original `Snapshot.Id`,
  which is stored as `SnapshotGroupId` on chunk documents.
- Each chunk stores `StartIndex` and `KeyCount` so Spark can find the chunk or chunks
  needed for a requested `_offset`.
- Snapshot metadata required for pagination is copied to each chunk.
- The default chunk size is 1000 keys per MongoDB document.

To preserve compatibility, Spark will add new additive interfaces:

- `ISnapshotStore2 : ISnapshotStore` adds offset-aware snapshot lookup.
- `IPagingService2 : IPagingService` passes the requested offset through to the store.

The old interfaces remain available. In the next major version, the old interfaces can be
removed and the `*2` interfaces can be renamed to the original names.

## Consequences

### Positive
- Large snapshots no longer need to fit in one MongoDB document.
- Existing `_snapshot` links remain stable.
- Existing custom implementations of `ISnapshotStore` and `IPagingService` remain
  source-compatible.
- Legacy single-document snapshots remain readable.

### Negative
- Snapshot persistence and retrieval are more complex.
- Chunk documents duplicate metadata.
- The old and new interfaces must coexist until the next major version.

### Risks
- A page can cross a chunk boundary, so snapshot reads must load all chunks overlapping
  the requested page window.
- Future changes to page size behavior must keep chunk-window selection and pagination
  calculation aligned.

## References
- [MongoDB BSON document size limit](https://www.mongodb.com/docs/manual/reference/limits/#mongodb-limit-BSON-Document-Size)
