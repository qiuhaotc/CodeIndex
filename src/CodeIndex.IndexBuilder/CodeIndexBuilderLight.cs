using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CodeIndex.Common;
using CodeIndex.Files;
using Lucene.Net.Documents;
using Lucene.Net.Search;

namespace CodeIndex.IndexBuilder
{
    public class CodeIndexBuilderLight : IDisposable
    {
        public CodeIndexBuilderLight(string name, LucenePoolLight codeIndexPool, LucenePoolLight hintIndexPool, ILog log)
        {
            name.RequireNotNullOrEmpty(nameof(name));
            codeIndexPool.RequireNotNull(nameof(codeIndexPool));
            hintIndexPool.RequireNotNull(nameof(hintIndexPool));
            log.RequireNotNull(nameof(log));

            Name = name;
            CodeIndexPool = codeIndexPool;
            HintIndexPool = hintIndexPool;
            Log = log;
        }

        public string Name { get; }
        public LucenePoolLight CodeIndexPool { get; }
        public LucenePoolLight HintIndexPool { get; }
        public ILog Log { get; }

        public void InitIndexFolderIfNeeded()
        {
            if (!Directory.Exists(CodeIndexPool.LuceneIndex))
            {
                Log.Info($"Create {Name} index folder {CodeIndexPool.LuceneIndex}");
                Directory.CreateDirectory(CodeIndexPool.LuceneIndex);
            }

            if (!Directory.Exists(HintIndexPool.LuceneIndex))
            {
                Log.Info($"Create {Name} index folder {HintIndexPool.LuceneIndex}");
                Directory.CreateDirectory(HintIndexPool.LuceneIndex);
            }
        }

        public void BuildIndexByBatch(IEnumerable<FileInfo> fileInfos, out List<FileInfo> failedIndexFiles, bool needCommit, bool triggerMerge, bool applyAllDeletes, CancellationToken cancellationToken, int batchSize = 10000)
        {
            fileInfos.RequireNotNull(nameof(fileInfos));
            batchSize.RequireRange(nameof(batchSize), int.MaxValue, 50);

            var documents = new List<Document>();
            failedIndexFiles = new List<FileInfo>();

            foreach (var fileInfo in fileInfos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (fileInfo.Exists)
                    {
                        var source = CodeSource.GetCodeSource(fileInfo, FilesContentHelper.ReadAllText(fileInfo.FullName));

                        // TODO: Build Hint

                        var doc = CodeIndexBuilder.GetDocumentFromSource(source);
                        documents.Add(doc);

                        Log.Info($"{Name}: Add index For {source.FilePath}");
                    }
                }
                catch (Exception ex)
                {
                    failedIndexFiles.Add(fileInfo);
                    Log.Error($"{Name}: Add index for {fileInfo.FullName} failed, exception: " + ex);
                }

                if (documents.Count >= batchSize)
                {
                    BuildIndex(needCommit, triggerMerge, applyAllDeletes, documents);
                    documents.Clear();
                }
            }

            if (documents.Count > 0)
            {
                BuildIndex(needCommit, triggerMerge, applyAllDeletes, documents);
            }
        }

        public void DeleteAllIndex()
        {
            Log.Info($"{Name}: Delete All Index start");
            CodeIndexPool.DeleteAllIndex();
            HintIndexPool.DeleteAllIndex();
            Log.Info($"{Name}: Delete All Index finished");
        }

        public IEnumerable<(string FilePath, DateTime LastWriteTimeUtc)> GetAllIndexedCodeSource()
        {
            return CodeIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue).Select(u => (u.Get(nameof(CodeSource.FilePath)), new DateTime(long.Parse(u.Get(nameof(CodeSource.LastWriteTimeUtc)))))).ToList();
        }

        void BuildIndex(bool needCommit, bool triggerMerge, bool applyAllDeletes, List<Document> documents)
        {
            Log.Info($"{Name}: Build index start, documents count {documents.Count}");
            CodeIndexPool.BuildIndex(documents, needCommit, triggerMerge, applyAllDeletes);
            Log.Info($"{Name}: Build index finished");
        }

        public bool IsDisposing { get; private set; }

        public void Dispose()
        {
            if (!IsDisposing)
            {
                IsDisposing = true;
                CodeIndexPool.Dispose();
                HintIndexPool.Dispose();
            }
        }

        public void UpdateIndex(FileInfo fileInfo)
        {
            throw new NotImplementedException();
        }

        public void DeleteIndex((string FilePath, DateTime LastWriteTimeUtc) codeSource)
        {
            throw new NotImplementedException();
        }

        public void Commit()
        {
            CodeIndexPool.Commit();
            HintIndexPool.Commit();
        }
    }
}
