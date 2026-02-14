# ADR-0002: Single branch for all supported FHIR Versions

## Status
Draft

## Context
Spark maintains separate branches per FHIR version (STU3, R4, R4B), causing:
- Code duplication across branches
- Bug fixes and features must be applied separately to each branch
- Increased maintenance burden and risk of divergence
- Difficult to ensure consistent behavior across versions

## Discussion

### Alternatives Considered

1. **Keep separate branches (status quo)**
   - Pros: No migration effort, versions are completely isolated
   - Cons: Ongoing duplication, diverging codebases, higher maintenance cost

2. **Single branch with compile-time switches (#if directives)**
   - Pros: Single codebase, simpler project structure
   - Cons: Cluttered code, harder to read, build complexity

3. **Single branch with multi-project architecture**
   - Pros: Clean separation, shared code in Core, type-safe version handling
   - Cons: Initial refactoring effort, more projects to manage

## Decision
The multi-project approach provides the best balance between code reuse and maintainability. It allows shared logic to live in `*.Core` projects while version-specific implementations remain isolated. 

We will consolidate all FHIR versions into a single master branch using a multi-project structure with shared core libraries.

### Proposed Structure
```
src/
├── Spark.Engine.Core/       # Shared code, interfaces
├── Spark.Engine.STU3/       # Hl7.Fhir.STU3
├── Spark.Engine.R4/         # Hl7.Fhir.R4
├── Spark.Engine.R4B/        # Hl7.Fhir.R4B
├── Spark.Mongo.Core/        # Shared MongoDB code
├── Spark.Mongo.STU3/
├── Spark.Mongo.R4/
├── Spark.Mongo.R4B/
└── Spark.Web/               # Selects version via config
```

## Consequences
