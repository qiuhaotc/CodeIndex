using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeIndex.Common;
using CodeIndex.Files;
using CodeIndex.IndexBuilder;
using Lucene.Net.Index;

namespace CodeIndex.MaintainIndex
{
    public class IndexInitializer
    {
        readonly ILog log;

        public IndexInitializer(ILog log = null)
        {
            this.log = log;
        }

        public void InitializeIndex(string codeFolder, string luceneIndex, string[] excludedExtensions, string[] excludedPaths, out List<FileInfo> failedIndexFiles, string includedExtenstion = "*", string[] includedExtensions = null, bool forceDeleteAllIndex = false)
        {
            codeFolder.RequireNotNull(nameof(codeFolder));
            luceneIndex.RequireNotNull(nameof(luceneIndex));
            
            log?.Info("Initialize start");

            var allFiles = FilesFetcher.FetchAllFiles(codeFolder, excludedExtensions, excludedPaths, includedExtenstion, includedExtensions).ToList();
            List<FileInfo> needToBuildIndex = null;

            if (CodeIndexBuilder.IndexExists(luceneIndex))
            {
                if (forceDeleteAllIndex)
                {
                    log?.Info("Delete exist index");
                    CodeIndexBuilder.DeleteAllIndex(luceneIndex);
                }
                else
                {
                    log?.Info("Compare index difference");

                    var allCodeSource = CodeIndexBuilder.GetAllIndexedCodeSource(luceneIndex);
                    needToBuildIndex = new List<FileInfo>();

                    foreach (var codeSource in allCodeSource)
                    {
                        var fileInfo = allFiles.FirstOrDefault(u => u.FullName == codeSource.FilePath);

                        if (fileInfo != null)
                        {
                            if (fileInfo.LastWriteTimeUtc != codeSource.LastWriteTimeUtc)
                            {
                                log?.Info($"File {fileInfo.FullName} modified");

                                CodeIndexBuilder.DeleteIndex(luceneIndex, CodeFilesIndexMaintainer.GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), codeSource.FilePath));
                                needToBuildIndex.Add(fileInfo);
                            }

                            allFiles.Remove(fileInfo);
                        }
                        else
                        {
                            log?.Info($"File {codeSource.FilePath} deleted");

                            CodeIndexBuilder.DeleteIndex(luceneIndex, CodeFilesIndexMaintainer.GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), codeSource.FilePath));
                        }
                    }

                    foreach (var needToCreateFiles in allFiles)
                    {
                        log?.Info($"Found new file {needToCreateFiles.FullName}");
                        needToBuildIndex.Add(needToCreateFiles);
                    }
                }
            }
            else if (!Directory.Exists(luceneIndex))
            {
                log?.Info($"Create index {luceneIndex}");
                Directory.CreateDirectory(luceneIndex);
            }

            CodeIndexBuilder.BuildIndex(luceneIndex, true, true, true, needToBuildIndex ?? allFiles, false, log, out failedIndexFiles);

            LucenePool.SaveResultsAndClearLucenePool(luceneIndex);

            log?.Info("Initialize finished");
        }
    }
}
