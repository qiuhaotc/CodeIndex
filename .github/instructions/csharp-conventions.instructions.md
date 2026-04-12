---
description: "Use when writing C# code in this project. Covers naming conventions, parameter validation, error handling, logging, thread safety, and FetchResult patterns specific to CodeIndex."
applyTo: "src/**/*.cs"
---

# C# Coding Conventions

## Naming Rules

- **Classes, methods, properties, events**: PascalCase (`CodeIndexBuilder`, `BuildIndexByBatch`)
- **Private fields**: camelCase, no prefix, no underscore (`indexChangeCount`, `readerWriteLock`)
- **Interfaces**: `I` prefix (`ILucenePool`, `IIndexMaintainer`)
- **DTOs / configuration classes**: prefer C# `record` (`IndexConfig`, `SearchRequest`)
- **Constants**: PascalCase (`HintWordMinLength`, `SplitChar`)

## Parameter Validation

Use `ArgumentValidation` extension methods (in `CodeIndex.Common`) at the entry point of all public methods:

```csharp
using CodeIndex.Common;

public void SomeMethod(string name, ILucenePool pool, int batchSize)
{
    name.RequireNotNullOrEmpty(nameof(name));
    pool.RequireNotNull(nameof(pool));
    batchSize.RequireRange(nameof(batchSize), int.MaxValue, 50);
    // ...
}
```

## Error Handling — FetchResult Pattern

The API layer never throws exceptions; always wrap results in `FetchResult<T>`:

```csharp
FetchResult<IEnumerable<CodeSource>> result;
try
{
    result = new FetchResult<IEnumerable<CodeSource>>
    {
        Result = SearchCodeSource(searchRequest),
        Status = new Status { Success = true }
    };
}
catch (Exception ex)
{
    result = new FetchResult<IEnumerable<CodeSource>>
    {
        Status = new Status { Success = false, StatusDesc = ex.ToString() }
    };
    log.LogError(ex, $"{indexConfig.IndexName}: SearchCodeSource failed");
}
return result;
```

## Logging

- Inject `ILogger<T>` or `ILogger` via constructor; name the field `log`
- **Every log entry must include the index name prefix**: `$"{IndexConfig.IndexName}: <action description>"`
- Log levels: `LogInformation` (normal flow), `LogError` (exceptions, include the Exception parameter), `LogDebug` (debug/search results)

```csharp
log.LogInformation($"{IndexConfig.IndexName}: Index initialized successfully");
log.LogError(ex, $"{IndexConfig.IndexName}: Failed to build index for {filePath}");
```

## Thread Safety — ReaderWriterLockSlim

Any class involving `IndexWriter`/`IndexSearcher` must be protected with a read-write lock:

```csharp
readonly ReaderWriterLockSlim readerWriteLock = new ReaderWriterLockSlim();

// Write operation
using var writeLock = new EnterReaderWriterLock(readerWriteLock, true);
// ... write code

// Read operation
using var readLock = new EnterReaderWriterLock(readerWriteLock, false);
// ... read code
```

When lazy-loading `IndexWriter`, use double-checked locking:

```csharp
readonly object syncLockForWriter = new object();
IndexWriter indexWriter;

IndexWriter GetWriter()
{
    if (indexWriter == null)
    {
        lock (syncLockForWriter)
        {
            indexWriter ??= CreateWriter();
        }
    }
    return indexWriter;
}
```

## Lazy-Initialized Properties (`??=`)

```csharp
QueryParser queryParserNormal;
public QueryParser QueryParserNormal => queryParserNormal ??= LucenePoolLight.GetQueryParser();
```

## Dependency Injection

- Inject dependencies via constructor; do not use Service Locator
- Lower-layer projects (IndexBuilder, Files) must not reference higher-layer projects (Server, Search)
- Use types from `CodeIndex.Common` for cross-layer data transfer
