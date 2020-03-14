using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeIndex.Common;
using CodeIndex.Files;
using CodeIndex.IndexBuilder;

namespace CodeIndex.MaintainIndex
{
    public class IndexInitializer
    {
        readonly ILog log;

        public IndexInitializer(ILog log = null)
        {
            this.log = log;
        }

        public void InitializeIndex(CodeIndexConfiguration config, string[] excludedExtensions, string[] excludedPaths, out List<FileInfo> failedIndexFiles, string includedExtenstion = "*", string[] includedExtensions = null, bool forceDeleteAllIndex = false)
        {
            config.RequireNotNull(nameof(config));
            
            log?.Info($"Initialize start for {config.LuceneIndex}");

            var allFiles = FilesFetcher.FetchAllFiles(config.MonitorFolder, excludedExtensions, excludedPaths, includedExtenstion, includedExtensions).ToList();
            List<FileInfo> needToBuildIndex = null;

            CodeIndexBuilder.InitIndexFolderIfNeeded(config, log);

            if (CodeIndexBuilder.IndexExists(config.LuceneIndexForCode))
            {
                if (forceDeleteAllIndex)
                {
                    log?.Info("Delete exist index");
                    CodeIndexBuilder.DeleteAllIndex(config);
                }
                else
                {
                    log?.Info("Compare index difference");

                    var allCodeSource = CodeIndexBuilder.GetAllIndexedCodeSource(config.LuceneIndexForCode);
                    needToBuildIndex = new List<FileInfo>();

                    foreach (var codeSource in allCodeSource)
                    {
                        var fileInfo = allFiles.FirstOrDefault(u => u.FullName == codeSource.FilePath);

                        if (fileInfo != null)
                        {
                            if (fileInfo.LastWriteTimeUtc != codeSource.LastWriteTimeUtc)
                            {
                                log?.Info($"File {fileInfo.FullName} modified");

                                CodeIndexBuilder.DeleteIndex(config.LuceneIndexForCode, CodeFilesIndexMaintainer.GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), codeSource.FilePath));
                                needToBuildIndex.Add(fileInfo);
                            }

                            allFiles.Remove(fileInfo);
                        }
                        else
                        {
                            log?.Info($"File {codeSource.FilePath} deleted");

                            CodeIndexBuilder.DeleteIndex(config.LuceneIndexForCode, CodeFilesIndexMaintainer.GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), codeSource.FilePath));
                        }
                    }

                    foreach (var needToCreateFiles in allFiles)
                    {
                        log?.Info($"Found new file {needToCreateFiles.FullName}");
                        needToBuildIndex.Add(needToCreateFiles);
                    }
                }
            }

            CodeIndexBuilder.BuildIndexByBatch(config, true, true, true, needToBuildIndex ?? allFiles, false, log, out failedIndexFiles);

            LucenePool.SaveResultsAndClearLucenePool(config.LuceneIndexForCode);

            WordsHintBuilder.BuildIndexByBatch(config, true, true, true, log);

            log?.Info($"Initialize finished for {config.LuceneIndex}");
        }
    }
}
