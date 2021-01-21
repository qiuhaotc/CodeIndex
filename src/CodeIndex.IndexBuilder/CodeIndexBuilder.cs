using System;
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
using Microsoft.Extensions.Logging;

namespace CodeIndex.IndexBuilder
{
    public class CodeIndexBuilder : IDisposable
    {
        public CodeIndexBuilder(string name, LucenePoolLight codeIndexPool, LucenePoolLight hintIndexPool, ILogger log)
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
        public ILogger Log { get; }

        public void InitIndexFolderIfNeeded()
        {
            if (!Directory.Exists(CodeIndexPool.LuceneIndex))
            {
                Log.LogInformation($"Create {Name} index folder {CodeIndexPool.LuceneIndex}");
                Directory.CreateDirectory(CodeIndexPool.LuceneIndex);
            }

            if (!Directory.Exists(HintIndexPool.LuceneIndex))
            {
                Log.LogInformation($"Create {Name} index folder {HintIndexPool.LuceneIndex}");
                Directory.CreateDirectory(HintIndexPool.LuceneIndex);
            }
        }

        public List<FileInfo> BuildIndexByBatch(IEnumerable<FileInfo> fileInfos, bool needCommit, bool triggerMerge, bool applyAllDeletes, CancellationToken cancellationToken, bool brandNewBuild, int batchSize = 10000)
        {
            cancellationToken.ThrowIfCancellationRequested();
            fileInfos.RequireNotNull(nameof(fileInfos));
            batchSize.RequireRange(nameof(batchSize), int.MaxValue, 50);

            var codeDocuments = new List<Document>();
            var wholeWords = new HashSet<string>();
            var newHintWords = new HashSet<string>();
            var failedIndexFiles = new List<FileInfo>();

            try
            {
                foreach (var fileInfo in fileInfos)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        if (fileInfo.Exists)
                        {
                            var source = CodeSource.GetCodeSource(fileInfo, FilesContentHelper.ReadAllText(fileInfo.FullName));

                            AddHintWords(newHintWords, wholeWords, source.Content);

                            var doc = IndexBuilderHelper.GetDocumentFromSource(source);
                            codeDocuments.Add(doc);

                            Log.LogInformation($"{Name}: Add index for {source.FilePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failedIndexFiles.Add(fileInfo);
                        Log.LogError($"{Name}: Add index for {fileInfo.FullName} failed, exception: " + ex);
                    }

                    if (codeDocuments.Count >= batchSize)
                    {
                        BuildIndex(needCommit, triggerMerge, applyAllDeletes, codeDocuments, newHintWords, cancellationToken, brandNewBuild);
                        codeDocuments.Clear();
                        newHintWords.Clear();
                    }
                }

                if (codeDocuments.Count > 0)
                {
                    BuildIndex(needCommit, triggerMerge, applyAllDeletes, codeDocuments, newHintWords, cancellationToken, brandNewBuild);
                }

                return failedIndexFiles;
            }
            finally
            {
                wholeWords.Clear();
                newHintWords.Clear();
                codeDocuments.Clear();
            }
        }

        const int HintWordMinLength = 4;
        public const int HintWordMaxLength = 199;

        void AddHintWords(HashSet<string> hintWords, string content)
        {
            var words = WordSegmenter.GetWords(content, HintWordMinLength, HintWordMaxLength);
            foreach (var word in words)
            {
                hintWords.Add(word);
            }
        }

        void AddHintWords(HashSet<string> hintWords, HashSet<string> wholeWords, string content)
        {
            var words = WordSegmenter.GetWords(content, HintWordMinLength, HintWordMaxLength);

            foreach (var word in words)
            {
                if (wholeWords.Add(word)) // Avoid Distinct Value
                {
                    hintWords.Add(word);
                }
            }
        }

        public void DeleteAllIndex()
        {
            Log.LogInformation($"{Name}: Delete All Index start");
            CodeIndexPool.DeleteAllIndex();
            HintIndexPool.DeleteAllIndex();
            Log.LogInformation($"{Name}: Delete All Index finished");
        }

        public IEnumerable<(string FilePath, DateTime LastWriteTimeUtc)> GetAllIndexedCodeSource()
        {
            return CodeIndexPool.SearchWithSpecificFields(
                new MatchAllDocsQuery(),
                int.MaxValue,
                nameof(CodeSource.LastWriteTimeUtc), nameof(CodeSource.FilePath)).Select(u => (u.Get(nameof(CodeSource.FilePath)), new DateTime(u.GetField(nameof(CodeSource.LastWriteTimeUtc)).GetInt64Value() ?? throw new ArgumentException(nameof(CodeSource.LastWriteTimeUtc)))));
        }

        public IndexBuildResults CreateIndex(FileInfo fileInfo)
        {
            try
            {
                if (fileInfo.Exists)
                {
                    var source = CodeSource.GetCodeSource(fileInfo, FilesContentHelper.ReadAllText(fileInfo.FullName));

                    var words = new HashSet<string>();
                    AddHintWords(words, source.Content);

                    var doc = IndexBuilderHelper.GetDocumentFromSource(source);
                    CodeIndexPool.UpdateIndex(GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), source.FilePath), doc);

                    foreach (var word in words)
                    {
                        HintIndexPool.UpdateIndex(new Term(nameof(CodeWord.Word), word), new Document
                        {
                            new StringField(nameof(CodeWord.Word), word, Field.Store.YES),
                            new StringField(nameof(CodeWord.WordLower), word.ToLowerInvariant(), Field.Store.YES)
                        });
                    }

                    Log.LogInformation($"{Name}: Create index For {source.FilePath} finished");
                }

                return IndexBuildResults.Successful;
            }
            catch (Exception ex)
            {
                Log.LogError($"{Name}: Create index for {fileInfo.FullName} failed, exception: " + ex);

                if (ex is IOException)
                {
                    return IndexBuildResults.FailedWithIOException;
                }
                else if (ex is OperationCanceledException)
                {
                    throw;
                }

                return IndexBuildResults.FailedWithError;
            }
        }

        void BuildIndex(bool needCommit, bool triggerMerge, bool applyAllDeletes, List<Document> codeDocuments, HashSet<string> newHintWords, CancellationToken cancellationToken, bool brandNewBuild)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Log.LogInformation($"{Name}: Build code index start, documents count {codeDocuments.Count}");

            Parallel.ForEach(
                codeDocuments,
                () => new List<Document>(),
                (codeDocument, status, documentLists) =>
                {
                    documentLists.Add(codeDocument);
                    return documentLists;
                },
                documentLists =>
                {
                    if (documentLists.Count > 0)
                    {
                        CodeIndexPool.BuildIndex(documentLists, needCommit, triggerMerge, applyAllDeletes);
                    }
                });

            Log.LogInformation($"{Name}: Build code index finished");

            Log.LogInformation($"{Name}: Build {(brandNewBuild ? "brand New" : "exist")} hint index start, documents count {newHintWords.Count}");

            if (brandNewBuild)
            {
                Parallel.ForEach(
                newHintWords,
                () => new List<Document>(),
                (word, status, documentLists) =>
                {
                    documentLists.Add(new Document
                    {
                        new StringField(nameof(CodeWord.Word), word, Field.Store.YES),
                        new StringField(nameof(CodeWord.WordLower), word.ToLowerInvariant(), Field.Store.YES)
                    });

                    return documentLists;
                },
                documentLists =>
                {
                    if (documentLists.Count > 0)
                    {
                        HintIndexPool.BuildIndex(documentLists, needCommit, triggerMerge, applyAllDeletes);
                    }
                });
            }
            else
            {
                Parallel.ForEach(newHintWords, word =>
                {
                    HintIndexPool.UpdateIndex(new Term(nameof(CodeWord.Word), word), new Document
                    {
                        new StringField(nameof(CodeWord.Word), word, Field.Store.YES),
                        new StringField(nameof(CodeWord.WordLower), word.ToLowerInvariant(), Field.Store.YES)
                    });
                });

                if (needCommit || triggerMerge || applyAllDeletes)
                {
                    HintIndexPool.Commit();
                }
            }

            Log.LogInformation($"{Name}: Build hint index finished");
        }

        public bool RenameFolderIndexes(string oldFolderPath, string nowFolderPath, CancellationToken cancellationToken)
        {
            try
            {
                var documents = CodeIndexPool.Search(new PrefixQuery(GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), oldFolderPath)), 1);

                foreach (var document in documents)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    RenameIndex(document, oldFolderPath, nowFolderPath);
                }

                Log.LogInformation($"{Name}: Rename folder index from {oldFolderPath} to {nowFolderPath} successful");
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"{Name}: Rename folder index from {oldFolderPath} to {nowFolderPath} failed, exception: " + ex);
                return false;
            }
        }

        public IndexBuildResults RenameFileIndex(string oldFilePath, string nowFilePath)
        {
            try
            {
                var documents = CodeIndexPool.Search(new TermQuery(GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), oldFilePath)), 1);

                if (documents.Length == 1)
                {
                    RenameIndex(documents[0], oldFilePath, nowFilePath);

                    Log.LogInformation($"{Name}: Rename file index from {oldFilePath} to {nowFilePath} successful");

                    return IndexBuildResults.Successful;
                }

                if (documents.Length == 0)
                {
                    Log.LogInformation($"{Name}: Rename file index failed, unable to find any document from {oldFilePath}, possible template file renamed, fallback to create index.");
                    return CreateIndex(new FileInfo(nowFilePath));
                }

                Log.LogWarning($"{Name}: Rename file index from {oldFilePath} to {nowFilePath} failed, unable to find one document, there are {documents.Length} document(s) founded");
                return IndexBuildResults.FailedWithError;
            }
            catch (Exception ex)
            {
                Log.LogError($"{Name}: Rename file index from {oldFilePath} to {nowFilePath} failed, exception: " + ex);

                if (ex is IOException)
                {
                    return IndexBuildResults.FailedWithIOException;
                }

                return IndexBuildResults.FailedWithError;
            }
        }

        void RenameIndex(Document document, string oldFilePath, string nowFilePath)
        {
            var pathField = document.Get(nameof(CodeSource.FilePath));
            var nowPath = pathField.Replace(oldFilePath, nowFilePath);
            document.RemoveField(nameof(CodeSource.FilePath));
            document.RemoveField(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix);
            document.Add(new TextField(nameof(CodeSource.FilePath), nowPath, Field.Store.YES));
            document.Add(new StringField(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, nowPath, Field.Store.YES));
            CodeIndexPool.UpdateIndex(new Term(nameof(CodeSource.CodePK), document.Get(nameof(CodeSource.CodePK))), document);
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

        public IndexBuildResults UpdateIndex(FileInfo fileInfo, CancellationToken cancellationToken)
        {
            try
            {
                if (fileInfo.Exists)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var source = CodeSource.GetCodeSource(fileInfo, FilesContentHelper.ReadAllText(fileInfo.FullName));

                    var words = new HashSet<string>();
                    AddHintWords(words, source.Content);

                    var doc = IndexBuilderHelper.GetDocumentFromSource(source);
                    CodeIndexPool.UpdateIndex(GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), source.FilePath), doc, out var rawDocuments);

                    if (rawDocuments.Length >= 1)
                    {
                        var rawWords = new HashSet<string>();
                        AddHintWords(rawWords, GetCodeSourceFromDocument(rawDocuments[0]).Content);

                        var wordsNeedToRemove = rawWords.Except(words).ToArray();
                        var wordsNeedToAdd = words.Except(rawWords);
                        words = wordsNeedToAdd.ToHashSet();

                        Log.LogInformation($"{Name}: Find {wordsNeedToRemove.Length} Delete Candidates Words, {words.Count} Update Candidates Words With Path {source.FilePath}");

                        if (rawDocuments.Length > 1)
                        {
                            Log.LogError($"{Name}: Find {rawDocuments.Length} Documents With Path {source.FilePath} To Update");
                        }

                        foreach (var needToDeleteWord in wordsNeedToRemove)
                        {
                            if (!CodeIndexPool.Exists(new TermQuery(new Term(GetCaseSensitiveField(nameof(CodeSource.Content)), needToDeleteWord))))
                            {
                                HintIndexPool.DeleteIndex(new Term(nameof(CodeWord.Word), needToDeleteWord));
                            }
                        }
                    }
                    else
                    {
                        Log.LogError($"{Name}: Find 0 Document To Update With Path {source.FilePath}, Create New Index");
                    }

                    foreach (var word in words)
                    {
                        HintIndexPool.UpdateIndex(new Term(nameof(CodeWord.Word), word), new Document
                        {
                            new StringField(nameof(CodeWord.Word), word, Field.Store.YES),
                            new StringField(nameof(CodeWord.WordLower), word.ToLowerInvariant(), Field.Store.YES)
                        });
                    }

                    Log.LogInformation($"{Name}: Update index For {source.FilePath} finished");
                }

                return IndexBuildResults.Successful;
            }
            catch (Exception ex)
            {
                Log.LogError($"{Name}: Update index for {fileInfo.FullName} failed, exception: " + ex);

                if (ex is IOException)
                {
                    return IndexBuildResults.FailedWithIOException;
                }
                else if (ex is OperationCanceledException)
                {
                    throw;
                }

                return IndexBuildResults.FailedWithError;
            }
        }

        public HashSet<string> GetAllHintWords()
        {
            return HintIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue).Select(u => u.Get(nameof(CodeWord.Word))).ToHashSet();
        }

        public bool DeleteIndex(string filePath)
        {
            try
            {
                CodeIndexPool.DeleteIndex(new PrefixQuery(GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), filePath)), out var documentsBeenDeleted);

                if (documentsBeenDeleted.Length >= 1)
                {
                    var wordsNeedToRemove = new HashSet<string>();

                    foreach (var document in documentsBeenDeleted)
                    {
                        AddHintWords(wordsNeedToRemove, GetCodeSourceFromDocument(document).Content);
                    }

                    Log.LogInformation($"{Name}: Find {wordsNeedToRemove.Count} Delete Candidates Words With Path {filePath}");

                    if (documentsBeenDeleted.Length > 1)
                    {
                        Log.LogInformation($"{Name}: Find {documentsBeenDeleted.Length} Documents With Path {filePath} To Delete");
                    }

                    foreach (var needToDeleteWord in wordsNeedToRemove)
                    {
                        if (!CodeIndexPool.Exists(new TermQuery(new Term(GetCaseSensitiveField(nameof(CodeSource.Content)), needToDeleteWord))))
                        {
                            HintIndexPool.DeleteIndex(new Term(nameof(CodeWord.Word), needToDeleteWord));
                        }
                    }
                }
                else
                {
                    Log.LogWarning($"{Name}: Find No Documents To Delete For {filePath}");
                }

                Log.LogInformation($"{Name}: Delete index For {filePath} finished");

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"{Name}: Delete index for {filePath} failed, exception: " + ex);
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

        public static string GetCaseSensitiveField(string fieldName)
        {
            return $"{fieldName}{Constants.CaseSensitive}";
        }

        public static CodeSource GetCodeSourceFromDocument(Document document)
        {
            return new CodeSource
            {
                CodePK = document.Get(nameof(CodeSource.CodePK)),
                Content = document.Get(nameof(CodeSource.Content)),
                FileExtension = document.Get(nameof(CodeSource.FileExtension)),
                FileName = document.Get(nameof(CodeSource.FileName)),
                FilePath = document.Get(nameof(CodeSource.FilePath)),
                IndexDate = new DateTime(document.GetField(nameof(CodeSource.IndexDate)).GetInt64Value() ?? throw new ArgumentException(nameof(CodeSource.IndexDate))),
                LastWriteTimeUtc = new DateTime(document.GetField(nameof(CodeSource.LastWriteTimeUtc)).GetInt64Value() ?? throw new ArgumentException(nameof(CodeSource.LastWriteTimeUtc)))
            };
        }
    }
}
