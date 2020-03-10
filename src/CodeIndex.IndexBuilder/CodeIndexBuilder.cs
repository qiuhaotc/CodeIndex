using System;
using System.Collections.Generic;
using System.IO;
using CodeIndex.Common;
using CodeIndex.Files;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace CodeIndex.IndexBuilder
{
    public static class CodeIndexBuilder
    {
        public static void BuildIndex(string luceneIndex, bool triggerMerge, bool applyAllDeletes, bool needFlush, IEnumerable<FileInfo> fileInfos, bool deleteExistIndex, ILog log, out List<FileInfo> failedIndexFiles, int batchSize = 1000)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            fileInfos.RequireNotNull(nameof(fileInfos));
            batchSize.RequireRange(nameof(batchSize), int.MaxValue, 50);

            var needDeleteExistIndex = deleteExistIndex && IndexExists(luceneIndex);
            var documents = new List<Document>();
            failedIndexFiles = new List<FileInfo>();

            foreach (var fileInfo in fileInfos)
            {
                try
                {
                    if (fileInfo.Exists)
                    {
                        var source = CodeSource.GetCodeSource(fileInfo, FilesContentHelper.ReadAllText(fileInfo.FullName));

                        if (needDeleteExistIndex)
                        {
                            DeleteIndex(luceneIndex, new Term(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, source.FilePath));
                        }

                        var doc = GetDocumentFromSource(source);
                        documents.Add(doc);

                        log?.Info($"Add index For {source.FilePath}");
                    }
                }
                catch (Exception ex)
                {
                    failedIndexFiles.Add(fileInfo);
                    log?.Error($"Add index for {fileInfo.FullName} failed, exception: " + ex.ToString());
                }

                if (documents.Count >= batchSize)
                {
                    BuildIndex(luceneIndex, triggerMerge, applyAllDeletes, documents, needFlush, log);
                    documents.Clear();
                }
            }

            if(documents.Count > 0)
            {
                BuildIndex(luceneIndex, triggerMerge, applyAllDeletes, documents, needFlush, log);
            }
        }

        public static void BuildIndex(string luceneIndex, bool triggerMerge, bool applyAllDeletes, bool needFlush, IEnumerable<CodeSource> codeSources, bool deleteExistIndex = true, ILog log = null)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            codeSources.RequireNotNull(nameof(codeSources));

            var needDeleteExistIndex = deleteExistIndex && IndexExists(luceneIndex);
            var documents = new List<Document>();

            foreach (var source in codeSources)
            {
                if (needDeleteExistIndex)
                {
                    DeleteIndex(luceneIndex, new Term(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, source.FilePath));
                }

                var doc = GetDocumentFromSource(source);
                documents.Add(doc);

                log?.Info($"Add index For {source.FilePath}");
            }

            BuildIndex(luceneIndex, triggerMerge, applyAllDeletes, documents, needFlush, log);
        }

        static void BuildIndex(string luceneIndex, bool triggerMerge, bool applyAllDeletes, List<Document> documents, bool needFlush, ILog log)
        {
            log?.Info($"Build index start, documents count {documents.Count}");
            LucenePool.BuildIndex(luceneIndex, triggerMerge, applyAllDeletes, documents, needFlush);
            log?.Info($"Build index finished");
        }

        public static List<(string FilePath, DateTime LastWriteTimeUtc)> GetAllIndexedCodeSource(string luceneIndex)
        {
            return LucenePool.GetAllIndexedCodeSource(luceneIndex);
        }

        public static void DeleteIndex(string luceneIndex, params Query[] searchQueries)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            searchQueries.RequireContainsElement(nameof(searchQueries));

            LucenePool.DeleteIndex(luceneIndex, searchQueries);
        }

        public static void DeleteIndex(string luceneIndex, params Term[] terms)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            terms.RequireContainsElement(nameof(terms));

            LucenePool.DeleteIndex(luceneIndex, terms);
        }

        public static void UpdateIndex(string luceneIndex, Term term, Document document)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            term.RequireNotNull(nameof(term));
            document.RequireNotNull(nameof(document));

            LucenePool.UpdateIndex(luceneIndex, term, document);
        }

        public static bool IndexExists(string luceneIndex)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));

            return DirectoryReader.IndexExists(FSDirectory.Open(luceneIndex));
        }

        public static void DeleteAllIndex(string luceneIndex)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));

            if (IndexExists(luceneIndex))
            {
                LucenePool.DeleteAllIndex(luceneIndex);
            }
        }

        static string ToStringSafe(this string value)
        {
            return value ?? string.Empty;
        }

        public static Document GetDocumentFromSource(CodeSource source)
        {
            return new Document
            {
                new TextField(nameof(source.FileName), source.FileName.ToStringSafe(), Field.Store.YES),
                // StringField indexes but doesn't tokenize
                new StringField(nameof(source.FileExtension), source.FileExtension.ToStringSafe(), Field.Store.YES),
                new StringField(nameof(source.FilePath) + Constants.NoneTokenizeFieldSuffix, source.FilePath.ToStringSafe(), Field.Store.YES),
                new TextField(nameof(source.FilePath), source.FilePath.ToStringSafe(), Field.Store.YES),
                new TextField(nameof(source.Content), source.Content.ToStringSafe(), Field.Store.YES),
                new Int64Field(nameof(source.IndexDate), source.IndexDate.Ticks, Field.Store.YES),
                new Int64Field(nameof(source.LastWriteTimeUtc), source.LastWriteTimeUtc.Ticks, Field.Store.YES),
                new StringField(nameof(source.CodePK), source.CodePK.ToString(), Field.Store.YES)
            };
        }

        public static void UpdateCodeFilePath(Document codeSourceDocumnet, string oldFullPath, string nowFullPath)
        {
            var pathField = codeSourceDocumnet.Get(nameof(CodeSource.FilePath));
            codeSourceDocumnet.RemoveField(nameof(CodeSource.FilePath));
            codeSourceDocumnet.RemoveField(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix);
            codeSourceDocumnet.Add(new TextField(nameof(CodeSource.FilePath), pathField.Replace(oldFullPath, nowFullPath), Field.Store.YES));
            codeSourceDocumnet.Add(new StringField(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, pathField.Replace(oldFullPath, nowFullPath), Field.Store.YES));
        }

        public static Document GetDocument(string luceneIndex, Term term)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            term.RequireNotNull(nameof(term));

            var document = LucenePool.GetDocument(luceneIndex, term);
            return document;
        }
    }
}
