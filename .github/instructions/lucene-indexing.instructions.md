---
description: "Use when working with Lucene.NET indexing, LucenePoolLight, CodeIndexBuilder, IndexMaintainer, or search queries. Covers IndexWriter/IndexSearcher lifecycle, query building, CodeSource document structure, and thread safety."
applyTo: "src/CodeIndex.IndexBuilder/**/*.cs"
---

# Lucene.NET Indexing Conventions

## Document Structure - CodeSource

Each indexed file maps to one Lucene Document. Field mapping (see DocumentConverter.cs):

| CodeSource Property | Lucene Field | Index Type |
|---------------------|--------------|------------|
| FileName | FileName | StringField (exact) + TextField (tokenized) |
| FilePath | FilePath | StringField |
| FileExtension | FileExtension | StringField |
| Content | Content | TextField (tokenized, CodeAnalyzer) |
| IndexDate | IndexDate | Int64Field |
| LastWriteTimeUtc | LastWriteTimeUtc | Int64Field |
| CodePK | CodePK | StringField (Guid, primary key) |

## Index Operations

Never manipulate IndexWriter directly; always go through ILucenePool:

```csharp
// Create / overwrite
pool.BuildIndex(codeSource, needCommit: false); // needCommit=false for batch

// Update (delete then add)
pool.UpdateIndex(new Term(nameof(CodeSource.CodePK), codePK.ToString()), codeSource, needCommit: true);

// Delete
pool.DeleteIndex(new Term(nameof(CodeSource.CodePK), codePK.ToString()), needCommit: true);

// Single commit after a batch
pool.Commit();
```

## Batch Indexing

Disable per-file commits during batch operations and call Commit() once at the end to reduce overhead:

```csharp
foreach (var source in sources)
{
    pool.BuildIndex(source, needCommit: false);
}
pool.Commit();
```

## Query Building

Use QueryGenerator or the standard Lucene query API; never concatenate raw query strings:

```csharp
// Exact field match
var query = new TermQuery(new Term(nameof(CodeSource.FileExtension), ".cs"));

// Range query
var query = LongPoint.NewRangeQuery(nameof(CodeSource.IndexDate), startTicks, endTicks);

// Boolean combination
var boolQuery = new BooleanQuery();
boolQuery.Add(query1, Occur.MUST);
boolQuery.Add(query2, Occur.SHOULD);
```

## Dual-Index Design

CodeIndexBuilder manages two Lucene pools simultaneously:
- CodeIndex Pool - stores CodeSource (file content)
- HintIndex Pool - stores CodeWord (tokens for auto-complete hints)

Both pools must be updated together when adding or updating a file.

## IndexWriter / IndexSearcher Lifecycle

- IndexWriter is lazy-loaded (??=) and kept open for the lifetime of the instance
- Before searching, use DirectoryReader.OpenIfChanged to get the latest IndexSearcher
- On Dispose, always call Commit() before closing the writer
- All writer/searcher operations must run under ReaderWriterLockSlim protection

## Analyzer

The project uses the custom CodeAnalyzer (in CodeTokenUtils/) to tokenize code identifiers (camelCase, underscore splitting, etc.). Do not replace it with StandardAnalyzer; doing so degrades code search accuracy.