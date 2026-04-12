# CodeIndex Project Guidelines

## Project Overview

CodeIndex is a local code full-text search engine built on **Lucene.NET**, providing a Blazor Server Web UI and REST API. It supports multi-index management, real-time file monitoring, code highlight previews, and has a companion Visual Studio extension.

## Layer Architecture (dependencies flow downward only)

```
CodeIndex.Server          ← ASP.NET Core Blazor host + REST controllers
CodeIndex.Search          ← Search facade, HTML highlight generation
CodeIndex.MaintainIndex   ← Index lifecycle management, FileSystemWatcher scheduling
CodeIndex.IndexBuilder    ← Lucene.NET index low-level CRUD
CodeIndex.Files           ← File system reading and change preprocessing
CodeIndex.Common          ← Shared DTOs, configuration, validation utilities
```

**Rule**: Lower-layer projects must not reference higher-layer projects. All cross-layer data transfer uses types from `CodeIndex.Common`.

## Tech Stack

- **.NET 10 / C#** — target framework `net10.0`
- **Lucene.NET 4.8.0-beta00016** — indexing and search engine
- **ASP.NET Core Blazor Server** — Web UI
- **NLog 6.x** — logging (injected via `ILogger<T>`)
- **NUnit 4** — unit testing
- **NSwag** — OpenAPI/Swagger documentation

## Coding Conventions

### Naming
- Classes, methods, properties: PascalCase
- Private fields: camelCase, no underscore prefix (e.g. `indexChangeCount`)
- Interfaces: `I` prefix (e.g. `ILucenePool`)
- DTOs / configuration classes: prefer C# `record` types (e.g. `IndexConfig`, `SearchRequest`)

### Parameter Validation
Always use `ArgumentValidation` extension methods at method entry points:
```csharp
name.RequireNotNullOrEmpty(nameof(name));
batchSize.RequireRange(nameof(batchSize), int.MaxValue, 50);
```

### Error Handling and Return Values
- API layer always returns `FetchResult<T>` containing `Status` (`Success` + `StatusDesc`)
- Exceptions are caught at the Service/Controller layer and populated into `FetchResult`, never rethrown
- Log messages must include the index name prefix: `$"{IndexConfig.IndexName}: <action>"`

### Thread Safety
- Classes involving `IndexWriter`/`IndexSearcher` must use `ReaderWriterLockSlim`
- Wrap write operations with a write lock, read operations with a read lock, using the `EnterReaderWriterLock` helper

### Logging
Inject `ILogger<T>` or `ILogger` via constructor; do not expose the logger outside the class.

## Testing Conventions

- Test classes inherit from `BaseTestLight` (lightweight) or `BaseTest` (includes `CodeIndexConfiguration`)
- Each test runs in an isolated sandbox under `TEMP/CodeIndex.Test_{Guid}/`; `TearDown` cleans up automatically
- Use `DummyLog` instead of a real `ILogger`
- Test file structure mirrors source modules: `CodeIndex.Test/IndexBuilder/`, `CodeIndex.Test/Search/`, etc.

## Build and Test Commands

```bash
dotnet build src/CodeIndex.sln
dotnet test src/CodeIndex.Test/CodeIndex.Test.csproj
```

## Key Domain Concepts

| Concept | Description |
|---------|-------------|
| `CodeSource` | An indexed file (`FilePath`, `Content`, `CodePK` (Guid), etc.) |
| `IndexConfig` | Configuration for one index instance (`record`, contains monitor folder and filter rules) |
| `ILucenePool` | Lucene operation abstraction (`BuildIndex`, `Search`, `DeleteIndex`) |
| `LucenePoolLight` | `ILucenePool` implementation with lazy loading and read-write locks |
| `CodeIndexBuilder` | High-level index builder managing both the code index and the hint HintIndex pools |
| `IndexMaintainer` | Runtime index manager: initialization + file change monitoring |
| `IndexManagement` | Registry of all indexes (Singleton, `ConcurrentDictionary`) |
| `SearchService` | Search facade, wraps HTML highlights, returns `FetchResult<T>` |
| `FetchResult<T>` | Unified response wrapper (`Status` + `Result`) |

## REST API

- `api/Lucene/` — search endpoints, no authentication required
- `api/Management/` — management endpoints, most require Cookie authentication (`[Authorize]`)

Authentication: Cookie-based; login requires a CAPTCHA (stored in Session).
