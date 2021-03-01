using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeIndex.Common;

namespace CodeIndex.Files
{
    public static class FileChangesPreProcessing
    {
        public static void PreProcessingChanges(this IList<ChangedSource> orderedNeedProcessingChanges, string prefix, IndexConfig indexConfig, Action<string> doLog, Func<string, WatcherChangeTypes, bool> renameChangeIsExcludedFromIndex)
        {
            doLog.Invoke($"{indexConfig.IndexName}: Pre Processing {prefix}Changes Start, changes count: {orderedNeedProcessingChanges.Count}");

            RemoveTemplateChanges(orderedNeedProcessingChanges, indexConfig, doLog, renameChangeIsExcludedFromIndex);

            RemoveTemplateDeletedChanges(orderedNeedProcessingChanges, indexConfig, doLog);

            RemoveDuplicatedChanges(orderedNeedProcessingChanges, indexConfig, doLog);

            doLog.Invoke($"{indexConfig.IndexName}: Pre Processing {prefix}Changes Finished");
        }

        static void RemoveTemplateChanges(IList<ChangedSource> orderedNeedProcessingChanges, IndexConfig indexConfig, Action<string> doLog, Func<string, WatcherChangeTypes, bool> renameChangeIsExcludedFromIndex)
        {
            var needDeleted = new List<ChangedSource>();

            for (var i = 0; i < orderedNeedProcessingChanges.Count; i++)
            {
                var change = orderedNeedProcessingChanges[i];

                if (change.ChangesType == WatcherChangeTypes.Renamed)
                {
                    var templateRenameChange = orderedNeedProcessingChanges.Skip(i + 1).FirstOrDefault(u => u.ChangesType == WatcherChangeTypes.Renamed && PathEquals(u.FilePath, change.OldPath));

                    if (templateRenameChange != null)
                    {
                        needDeleted.Add(templateRenameChange);

                        if (renameChangeIsExcludedFromIndex.Invoke(change.OldPath, WatcherChangeTypes.Changed))
                        {
                            needDeleted.Add(change);
                            doLog.Invoke($"{indexConfig.IndexName}: Template Change Found {templateRenameChange}, remove template change and {change} as path is excluded from index");
                        }
                        else
                        {
                            change.ChangesType = WatcherChangeTypes.Changed;
                            change.FilePath = change.OldPath;
                            change.OldPath = null;

                            doLog.Invoke($"{indexConfig.IndexName}: Template Change Found {templateRenameChange}, remove this and update {change} from Renamed to Changed");
                        }
                    }
                }
            }

            needDeleted.ForEach(u => orderedNeedProcessingChanges.Remove(u));
        }

        static void RemoveTemplateDeletedChanges(IList<ChangedSource> orderedNeedProcessingChanges, IndexConfig indexConfig, Action<string> doLog)
        {
            var needDeleted = new List<ChangedSource>();

            for (var i = 0; i < orderedNeedProcessingChanges.Count; i++)
            {
                var change = orderedNeedProcessingChanges[i];

                if (change.ChangesType == WatcherChangeTypes.Deleted)
                {
                    var tempCreatedChanges = orderedNeedProcessingChanges.Skip(i + 1).FirstOrDefault(u => u.ChangesType == WatcherChangeTypes.Created && PathEquals(u.FilePath, change.FilePath));

                    if (tempCreatedChanges != null)
                    {
                        needDeleted.Add(change);

                        tempCreatedChanges.ChangesType = WatcherChangeTypes.Changed;

                        doLog.Invoke($"{indexConfig.IndexName}: Template Deleted Found {change}, remove this and update {tempCreatedChanges} from Created to Changed");
                    }
                }
            }

            needDeleted.ForEach(u => orderedNeedProcessingChanges.Remove(u));
        }

        static void RemoveDuplicatedChanges(IList<ChangedSource> orderedNeedProcessingChanges, IndexConfig indexConfig, Action<string> doLog)
        {
            var needDeleted = new List<ChangedSource>();

            for (var i = 0; i < orderedNeedProcessingChanges.Count; i++)
            {
                var change = orderedNeedProcessingChanges[i];

                var duplicateChanges = orderedNeedProcessingChanges.Skip(i + 1).FirstOrDefault(u => u.ChangesType == change.ChangesType && PathEquals(u.FilePath, change.FilePath) && PathEquals(u.OldPath, change.OldPath));

                if (duplicateChanges != null)
                {
                    needDeleted.Add(change);

                    doLog.Invoke($"{indexConfig.IndexName}: Duplicate Changes Found {change} and remove");
                }
            }

            needDeleted.ForEach(u => orderedNeedProcessingChanges.Remove(u));
        }

        static bool PathEquals(string pathA, string pathB)
        {
            return string.Equals(pathA, pathB, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
