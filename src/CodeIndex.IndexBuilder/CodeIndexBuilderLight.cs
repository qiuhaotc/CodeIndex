using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeIndex.Common;
using CodeIndex.Files;
using Lucene.Net.Documents;
using Lucene.Net.Index;
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

        public ConcurrentBag<FileInfo> BuildIndexByBatch(IEnumerable<FileInfo> fileInfos, bool needCommit, bool triggerMerge, bool applyAllDeletes, CancellationToken cancellationToken, int batchSize = 10000)
        {
            fileInfos.RequireNotNull(nameof(fileInfos));
            batchSize.RequireRange(nameof(batchSize), int.MaxValue, 50);

            var codeDocuments = new ConcurrentBag<Document>();
            var hintWords = new ConcurrentDictionary<string, int>();
            var failedIndexFiles = new ConcurrentBag<FileInfo>();
            var readWriteSlimLock = new ReaderWriterLockSlim();

            Parallel.ForEach(fileInfos, fileInfo =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                readWriteSlimLock.EnterReadLock();
                try
                {

                    if (fileInfo.Exists)
                    {
                        var source = CodeSource.GetCodeSource(fileInfo, FilesContentHelper.ReadAllText(fileInfo.FullName));

                        AddHintWords(hintWords, source.Content);

                        var doc = CodeIndexBuilder.GetDocumentFromSource(source);
                        codeDocuments.Add(doc);

                        Log.Info($"{Name}: Add index For {source.FilePath}");
                    }
                }
                catch (Exception ex)
                {
                    failedIndexFiles.Add(fileInfo);
                    Log.Error($"{Name}: Add index for {fileInfo.FullName} failed, exception: " + ex);
                }
                finally
                {
                    readWriteSlimLock.ExitReadLock();
                }

                if (codeDocuments.Count >= batchSize)
                {
                    readWriteSlimLock.EnterWriteLock();
                    try
                    {
                        if (codeDocuments.Count >= batchSize)
                        {
                            BuildIndex(needCommit, triggerMerge, applyAllDeletes, codeDocuments, hintWords, cancellationToken);
                            codeDocuments.Clear();
                            hintWords.Clear();
                        }
                    }
                    finally
                    {
                        readWriteSlimLock.ExitWriteLock();
                    }
                }
            });

            if (codeDocuments.Count > 0)
            {
                BuildIndex(needCommit, triggerMerge, applyAllDeletes, codeDocuments, hintWords, cancellationToken);
            }

            return failedIndexFiles;
        }

        void AddHintWords(HashSet<string> hintWords, string content)
        {
            var words = WordSegmenter.GetWords(content).Where(word => word.Length > 3 && word.Length < 200);
            foreach (var word in words)
            {
                hintWords.Add(word);
            }
        }

        void AddHintWords(ConcurrentDictionary<string, int> hintWords, string content)
        {
            var words = WordSegmenter.GetWords(content).Where(word => word.Length > 3 && word.Length < 200);
            foreach (var word in words)
            {
                hintWords.TryAdd(word, 0);
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

        void BuildIndex(bool needCommit, bool triggerMerge, bool applyAllDeletes, ConcurrentBag<Document> codeDocuments, ConcurrentDictionary<string, int> words, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Log.Info($"{Name}: Build code index start, documents count {codeDocuments.Count}");
            CodeIndexPool.BuildIndex(codeDocuments, needCommit, triggerMerge, applyAllDeletes);
            Log.Info($"{Name}: Build code index finished");

            Log.Info($"{Name}: Build hint index start, documents count {words.Count}");

            foreach (var word in words)
            {
                cancellationToken.ThrowIfCancellationRequested();

                HintIndexPool.UpdateIndex(new Term(nameof(CodeWord.Word), word.Key), new Document
                {
                    new StringField(nameof(CodeWord.Word), word.Key, Field.Store.YES),
                    new StringField(nameof(CodeWord.WordLower), word.Key.ToLowerInvariant(), Field.Store.YES)
                });
            }

            if (needCommit || triggerMerge || applyAllDeletes)
            {
                HintIndexPool.Commit();
            }

            Log.Info($"{Name}: Build hint index finished");
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

        public bool UpdateIndex(FileInfo fileInfo, CancellationToken cancellationToken)
        {
            try
            {
                if (fileInfo.Exists)
                {
                    var source = CodeSource.GetCodeSource(fileInfo, FilesContentHelper.ReadAllText(fileInfo.FullName));

                    var words = new HashSet<string>();
                    AddHintWords(words, source.Content);

                    var doc = CodeIndexBuilder.GetDocumentFromSource(source);
                    CodeIndexPool.UpdateIndex(GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), source.FilePath), doc);

                    foreach (var word in words)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        HintIndexPool.UpdateIndex(new Term(nameof(CodeWord.Word), word), new Document
                        {
                            new StringField(nameof(CodeWord.Word), word, Field.Store.YES),
                            new StringField(nameof(CodeWord.WordLower), word.ToLowerInvariant(), Field.Store.YES)
                        });
                    }

                    Log.Info($"{Name}: Update index For {source.FilePath} finished");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"{Name}: Update index for {fileInfo.FullName} failed, exception: " + ex);

                if (ex is OperationCanceledException)
                {
                    throw;
                }

                return false;
            }
        }

        public bool DeleteIndex(string filePath)
        {
            try
            {
                CodeIndexPool.DeleteIndex(GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), filePath));
                Log.Info($"{Name}: Delete index For {filePath} finished");

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"{Name}: Delete index for {filePath} failed, exception: " + ex);
                return false;
            }
        }

        public void Commit()
        {
            CodeIndexPool.Commit();
            HintIndexPool.Commit();
        }

        public Term GetNoneTokenizeFieldTerm(string fieldName, string termValue)
        {
            return new Term($"{fieldName}{Constants.NoneTokenizeFieldSuffix}", termValue);
        }
    }
}
