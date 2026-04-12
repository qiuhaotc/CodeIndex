---
description: "Generate NUnit unit tests for a CodeIndex class or method, following BaseTestLight/BaseTest patterns with DummyLog and file system sandbox isolation."
agent: "agent"
argument-hint: "Class or method to test, e.g.: LucenePoolLight.BuildIndex"
---

Write NUnit 4 unit tests for: `$SELECTION_OR_INPUT`

## Requirements

1. **Inherit the correct base class**
   - Only temp directory and ILogger needed -> inherit `BaseTestLight`
   - Needs `CodeIndexConfiguration` -> inherit `BaseTest`

2. **Test structure**

```csharp
[TestFixture]
public class {ClassName}Test : BaseTestLight
{
    {ClassName} sut; // System Under Test

    [SetUp]
    public void SetUp()
    {
        sut = new {ClassName}(TempIndexDir, DummyLog);
    }

    [TearDown]
    public void TearDown()
    {
        sut?.Dispose();
    }
}
```

3. **Test method naming**: `{MethodName}_{Scenario}_{ExpectedResult}`
   Example: `BuildIndex_WithValidCodeSource_ShouldCreateSearchableDocument`

4. **Assertions use the NUnit constraint model** (`Assert.That`):
   ```csharp
   Assert.That(result, Is.Not.Null);
   Assert.That(results, Has.Length.EqualTo(1));
   Assert.That(() => sut.Method(null), Throws.ArgumentException);
   ```

5. **Coverage scope**:
   - Happy path
   - Boundary conditions (empty string, zero, max value)
   - Invalid arguments (null input, verify `RequireNotNull` throws `ArgumentNullException`)
   - Concurrency scenarios (if the class under test uses `ReaderWriterLockSlim`)

6. **Verifying Lucene results**: use `TermQuery(new Term(nameof(CodeSource.{Field}), value))` to confirm documents were written correctly.

7. **File paths**: use sub-paths under `MonitorFolder` or `TempDir`; do not hardcode paths.

Based on the currently selected code context, generate a complete test class file and place it in `src/CodeIndex.Test/{matching module}/`.