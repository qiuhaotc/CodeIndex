---
description: "Use when writing unit tests for CodeIndex project. Covers BaseTest/BaseTestLight usage, test isolation, DummyLog, NUnit 4 patterns, and file system sandbox setup."
applyTo: "src/CodeIndex.Test/**/*.cs"
---

# Testing Conventions

## Base Class Selection

| Scenario | Base Class |
|----------|------------|
| Need full `CodeIndexConfiguration` | `BaseTest` |
| Only need temp directory + ILogger stub | `BaseTestLight` |

```csharp
// Lightweight test (recommended for most cases)
[TestFixture]
public class MyComponentTest : BaseTestLight
{
    // TempDir, TempIndexDir, MonitorFolder, DummyLog are provided by the base class
}

// Requires configuration object
[TestFixture]
public class MyIntegrationTest : BaseTest
{
    // Adds Config (CodeIndexConfiguration) property
}
```

## Available Base Class Properties

```
TempDir           → Test root temp directory (TEMP/CodeIndex.Test_{Guid}/)
TempIndexDir      → Index storage directory
MonitorFolder     → Simulated monitored code directory
DummyLog          → ILogger test double (no I/O)
Config            → CodeIndexConfiguration (BaseTest only)
```

## Test Structure Pattern

```csharp
[TestFixture]
public class LucenePoolLightTest : BaseTestLight
{
    LucenePoolLight pool;

    [SetUp]
    public void SetUp()
    {
        pool = new LucenePoolLight(TempIndexDir, DummyLog);
    }

    [TearDown]
    public void TearDown()
    {
        pool?.Dispose();
        // Directory cleanup is handled by BaseTestLight — do not delete again
    }

    [Test]
    public void BuildIndex_WithValidSource_ShouldCreateDocument()
    {
        var source = new CodeSource
        {
            FileName = "test.cs",
            FilePath = @"C:\Code\test.cs",
            Content = "public class Test {}",
            CodePK = Guid.NewGuid()
        };

        pool.BuildIndex(source, true);

        var results = pool.Search(new TermQuery(new Term(nameof(CodeSource.FileName), "test.cs")), 10);
        Assert.That(results, Has.Length.EqualTo(1));
    }
}
```

## ILogger Stub

Use `DummyLog` instead of `ILogger`; do not use mocks or custom ILogger implementations:

```csharp
// Correct
var builder = new CodeIndexBuilder(DummyLog, pool);

// Wrong — do not mock
var mockLog = new Mock<ILogger<CodeIndexBuilder>>();
```

## Test File Directory Structure

Test files must be placed in the corresponding module directory:

```
CodeIndex.Test/
  Common/          ← Tests for CodeIndex.Common
  Files/           ← Tests for CodeIndex.Files
  IndexBuilder/    ← Tests for CodeIndex.IndexBuilder
  MaintainIndex/   ← Tests for CodeIndex.MaintainIndex
  Search/          ← Tests for CodeIndex.Search
```

## File System Sandbox

Do not manually create temp directories; use the properties provided by the base class:

```csharp
// Correct — use base class paths
var filePath = Path.Combine(MonitorFolder, "sample.cs");
File.WriteAllText(filePath, "test content");

// Wrong — do not hardcode paths
var filePath = @"C:\Temp\test.cs";
```

## Async Tests

Lucene operations are synchronous; avoid async/await unless necessary. For scenarios that require async (e.g., waiting for `FileSystemWatcher` events inside `IndexMaintainer`), use `Task.Delay`:

```csharp
[Test]
public async Task MaintainIndexes_WhenFileChanged_ShouldUpdateIndex()
{
    File.WriteAllText(Path.Combine(MonitorFolder, "a.cs"), "new content");
    await Task.Delay(500); // wait for FileSystemWatcher to fire
    // assert index was updated
}
```
