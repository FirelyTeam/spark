# ADR 0001 - Transactional behavior across FHIR resources

- Status: Draft 

## Context
We lack the necessary capabilities to set up transactions across several documents in our MongoDB store, both within one
collection and across collections.  This affects our capability to rollback transaction operations and versioning of our
resources.

For example an operation on a single FHIR resource requires us to write across three collections, one document in each
collection: `resources`, `counters`, and `searchindex`.

In the future we might want to run the `IndexService` separately from the update in a background service.

### Brief on the contents of the collections:
- `resources`
  - This collection contains the actual FHIR resources.
- `searchindex`
  - This is our search index, it is built by IndexService and indexes FHIR resources based on FHIR Search Parameters.
- `counters`
  - contains the last version id for a resource.

### Requirements

#### The simple case - Updates one FHIR Resource
Writing one FHIR resource to our MongoDB store requires us to write one document to each of the three collections
mentioned above. Failing to write any of the documents for this one FHIR resource means we must roll back the
transaction.

#### The advanced case - Runs an update across several FHIR Resources
Writing several FHIR resources to our MongoDB store iterates over the simple case in *one* transaction, Failing to
write any of the documents across all the FHIR resources means we must roll back the transaction which ran across all
the FHIR resources.

### Current Architecture
```
FhirService.StoreAsync(Entry)
├── ResourceStorageService.AddAsync(Entry)
│   ├── Transfer.Internalize(Entry)
│   │   ├── Import.Add(Entry)
│   │   └── Import.Internalize()
│   │       └── InternalizeKeys()
│   │           └── InternalizeKey()
│   │               └── (...)
│   │                   └── GuidGeneratorExtensions.NextKey() / GuidGeneratorExtensions.NextHistoryKey()
│   │
│   └── MongoFhirStore.AddAsync(Entry)
│
└── ServiceListener.InformAsync(Entry)
└── IndexService.ProcessAsync(Entry)
```

`Transfer.Internalize(Entry)` has the side effect that when it calls out to `Import.Internalize()` it eventually remaps
the version id in its call chain and then writes that new version id to the `counters` collection through the
`GuidGeneratorExtensions.NextKey()` / `GuidGeneratorExtensions.NextHistoryKey()` method which again calls
`GuidGenerator.Next()`, this must be avoided and must be kept in the same transaction as storing the FHIR resource to
the `resources` collection.

`ServiceListener.InformAsync(Entry)` informs other `ServiceListener` and one of these are the `IndexService`, this
architecture is kind of nice if it ran independently in a background service, but currently it does not.

## Decision
We must have full control over the transaction meaning we should be able to start and stop transactions exactly when we
want in our code. It should be accessible from outside our MongoFhirStore.

Writing to the `counters` collection should probably be lifted out of the `GuidGenerator`, and should occur only when we
actually write the resource to the `resources` collection.

## Consequences

### Positive
- Resources are not created, updated, or deleted unless all preconditions are met
- References between resources do not come out of sync
- Enables true transaction support across resources

### Negative
- Performance impact
