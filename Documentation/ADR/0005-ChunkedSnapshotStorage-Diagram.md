# Chunked Snapshot Storage Code Diagram

```mermaid
flowchart TD
    Search[SearchService creates Snapshot<br/>Id = public snapshot id<br/>Keys = full ordered result set]
        --> PagingStart[PagingService.StartPaginationAsync Snapshot]

    PagingStart --> StoreAdd[ISnapshotStore.AddSnapshotAsync]
    StoreAdd --> MongoAdd[MongoSnapshotStore.AddSnapshotAsync]

    MongoAdd --> Split{Keys.Count > 1000?}

    Split -- No --> SingleDoc[Store one legacy snapshot doc<br/>Id = Snapshot.Id<br/>SnapshotGroupId = null<br/>Keys = all keys]

    Split -- Yes --> ChunkDocs[Store many chunk docs<br/>Id = unique document id<br/>SnapshotGroupId = original Snapshot.Id<br/>StartIndex = first key index<br/>KeyCount = chunk key count<br/>Keys = chunk keys]

    Client[Client follows page link<br/>/fhir/_snapshot?_snapshot=id&_offset=n]
        --> Controller[FhirController.Snapshot]
        --> FhirService[FhirService.GetPageAsync snapshotKey, index]

    FhirService --> PreferV2{IPagingService2 available?}

    PreferV2 -- Yes --> PagingV2[PagingService.StartPaginationAsync snapshotKey, offset]
    PreferV2 -- No --> PagingV1[IPagingService.StartPaginationAsync snapshotKey<br/>legacy fallback]

    PagingV2 --> StoreV2[ISnapshotStore2.GetSnapshotAsync snapshotId, offset]
    StoreV2 --> MongoLookup[MongoSnapshotStore.GetSnapshotAsync snapshotId, offset]

    MongoLookup --> LegacyLookup{Find document by Id?}
    LegacyLookup -- Yes --> LegacySnapshot[Return legacy single-doc Snapshot]
    LegacyLookup -- No --> ChunkLookup[Find chunks by SnapshotGroupId<br/>overlapping requested page window]

    ChunkLookup --> Rehydrate[Snapshot.CreateWindow<br/>Id = public snapshot id<br/>StartIndex = first loaded chunk<br/>Keys = loaded chunk keys]

    LegacySnapshot --> Pagination[SnapshotPaginationService.GetPageAsync offset]
    Rehydrate --> Pagination

    Pagination --> Calculator[SnapshotPaginationCalculator.GetKeysForPage]
    Calculator --> Slice[Skip offset - StartIndex<br/>Take CountParam or default page size]
    Slice --> StoreFetch[IFhirStore.GetAsync page keys]
    StoreFetch --> Bundle[Return Bundle with page links]
```

## Storage Shape

```text
Public page token:
_snapshot = original Snapshot.Id

Mongo chunk documents:
Id              = unique Mongo document id
SnapshotGroupId = original Snapshot.Id
StartIndex      = global offset of first key in this chunk
KeyCount        = number of keys in this chunk
Keys            = only this chunk's keys
```
