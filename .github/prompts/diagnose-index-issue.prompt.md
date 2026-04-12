---
description: "Diagnose and fix a failing or broken Lucene index: reindex all files, check IndexMaintainer state, reset index status, or rebuild from scratch."
agent: "agent"
argument-hint: "Problem description, e.g.: index status stuck at Initializing, or search returns no results"
---

Diagnose and fix the following CodeIndex issue: `$SELECTION_OR_INPUT`

## Diagnostic Steps

### 1. Check index status

`IndexStatus` state machine:
```
Idle -> Initializing -> Initialized -> Monitoring
                                    -> Error
                                    -> Disposing -> Disposed
```

When stuck in `Initializing` or `Error`:
- Check the `IndexNow` property for the current state
- Call `api/Management/GetIndexLists` to get runtime status
- Check `api/Lucene/GetLogs` for error entries with the index name prefix

### 2. Common issues

| Symptom | Likely Cause | Check |
|---------|-------------|-------|
| No search results | Index not committed / Searcher not refreshed | Is `Commit()` called? `DirectoryReader.OpenIfChanged` |
| Stuck at Initializing | Exception during file scan | Verify `MonitorFolder` exists and is accessible |
| FileSystemWatcher not firing | Path misconfiguration | `MonitorFolder` vs `MonitorFolderRealPath` (Docker path mapping) |
| Index growing too large | Segments not merged | Check `ForceMerge` calls on `IndexWriter` |

### 3. Rebuild the index

To fully rebuild (wipe existing index and rescan from the file system):

1. Call `api/Management/StopIndex?indexPk={pk}` to stop monitoring
2. Delete Lucene files under `{LuceneIndex}/CodeIndexes/{pk}/CodeIndex/` on disk
3. Call `api/Management/StartIndex?indexPk={pk}` to restart (triggers `InitializeIndex`)

### 4. Check filter configuration

If certain files are missing from search results:
- `IndexConfig.IncludedExtensions` - only extensions in this list are indexed (empty = all)
- `IndexConfig.ExcludedExtensions` - exclusion list
- `IndexConfig.ExcludedPaths` - excluded paths (pipe-separated)

## Based on the described problem, analyze the relevant code and provide a fix.

Key files to review:
- `src/CodeIndex.MaintainIndex/IndexMaintainer.cs` - state management and file monitoring
- `src/CodeIndex.IndexBuilder/LucenePoolLight.cs` - index read/write operations
- `src/CodeIndex.IndexBuilder/CodeIndexBuilder.cs` - batch build logic