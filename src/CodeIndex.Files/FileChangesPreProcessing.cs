using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeIndex.Common;

namespace CodeIndex.Files
{
    public static class FileChangesPreProcessing
    {
        public static void PreProcessingChanges(this IList<ChangedSource> orderedNeedProcessingChanges, string prefix, IndexConfig indexConfig, ILog log)
        {
            log.Info($"{indexConfig.IndexName}: Pre Processing {prefix}Changes Start, changes count: {orderedNeedProcessingChanges.Count}");

            RemoveTemplateChanges(orderedNeedProcessingChanges, indexConfig, log);

            RemoveTemplateDeletedChanges(orderedNeedProcessingChanges, indexConfig, log);

            RemoveDuplicatedChanges(orderedNeedProcessingChanges, indexConfig, log);

            log.Info($"{indexConfig.IndexName}: Pre Processing {prefix}Changes Finished");
        }

        static void RemoveTemplateChanges(IList<ChangedSource> orderedNeedProcessingChanges, IndexConfig indexConfig, ILog log)
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
                        change.ChangesType = WatcherChangeTypes.Changed;
                        change.FilePath = change.OldPath;
                        change.OldPath = null;
                        needDeleted.Add(templateRenameChange);

                        log.Info($"{indexConfig.IndexName}: Template Change Found {templateRenameChange}, remove this and update {change} from Renamed to Changed");
                    }
                }
            }

            needDeleted.ForEach(u => orderedNeedProcessingChanges.Remove(u));
        }

        static void RemoveTemplateDeletedChanges(IList<ChangedSource> orderedNeedProcessingChanges, IndexConfig indexConfig, ILog log)
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

                        log.Info($"{indexConfig.IndexName}: Template Deleted Found {change}, remove this and update {tempCreatedChanges} from Created to Changed");
                    }
                }
            }

            needDeleted.ForEach(u => orderedNeedProcessingChanges.Remove(u));
        }

        static void RemoveDuplicatedChanges(IList<ChangedSource> orderedNeedProcessingChanges, IndexConfig indexConfig, ILog log)
        {
            var needDeleted = new List<ChangedSource>();

            for (var i = 0; i < orderedNeedProcessingChanges.Count; i++)
            {
                var change = orderedNeedProcessingChanges[i];

                var duplicateChanges = orderedNeedProcessingChanges.Skip(i + 1).FirstOrDefault(u => u.ChangesType == change.ChangesType && PathEquals(u.FilePath, change.FilePath) && PathEquals(u.OldPath, change.OldPath));

                if (duplicateChanges != null)
                {
                    needDeleted.Add(change);

                    log.Info($"{indexConfig.IndexName}: Duplicate Changes Found {change} and remove");
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
