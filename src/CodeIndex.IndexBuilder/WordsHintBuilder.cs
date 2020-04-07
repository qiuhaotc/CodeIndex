using System.Collections.Generic;
using System.Linq;
using CodeIndex.Common;
using Lucene.Net.Documents;

namespace CodeIndex.IndexBuilder
{
    public static class WordsHintBuilder
    {
        public static HashSet<string> Words { get; } = new HashSet<string>();

        public static void BuildIndexByBatch(CodeIndexConfiguration config, bool triggerMerge, bool applyAllDeletes, bool needFlush, ILog log, bool firstInitialize, int batchSize = 10000)
        {
            config.RequireNotNull(nameof(config));
            batchSize.RequireRange(nameof(batchSize), int.MaxValue, 50);

            var documents = new List<Document>();

            log?.Info($"Start build hint words for {config.LuceneIndexForHint}");

            if (firstInitialize)
            {
                foreach (var word in Words)
                {
                    documents.Add(new Document
                    {
                         new StringField(nameof(CodeWord.Word), word, Field.Store.YES),
                         new StringField(nameof(CodeWord.WordLower), word.ToLowerInvariant(), Field.Store.YES)
                    });

                    if (documents.Count > batchSize)
                    {
                        BuildIndex(config, triggerMerge, applyAllDeletes, documents, needFlush, log);
                        documents.Clear();
                    }
                }

                if (documents.Count > 0)
                {
                    BuildIndex(config, triggerMerge, applyAllDeletes, documents, needFlush, log);
                }
            }
            else
            {
                UpdateHintWordsAndSaveIndex(config, Words.ToArray(), log, batchSize);
            }

            Words.Clear();

            log?.Info($"Finished build hint words for {config.LuceneIndexForHint}");
        }

        public static void AddWords(string[] words)
        {
            foreach (var word in words)
            {
                if (word.HasValidLength())
                {
                    Words.Add(word);
                }
            }
        }

        public static void UpdateWordsHint(CodeIndexConfiguration config, string[] words, ILog log)
        {
            words = words.Where(HasValidLength).Distinct().ToArray();

            UpdateHintWordsAndSaveIndex(config, words, log, needSaveIndex: false);
        }

        static void UpdateHintWordsAndSaveIndex(CodeIndexConfiguration config, string[] words, ILog log, int batchSize = -1, bool needSaveIndex = true)
        {
            var totalUpdate = 0;

            log?.Info($"Update hint index start, words count {words.Length}");

            foreach (var word in words)
            {
                var document = new Document
                {
                     new StringField(nameof(CodeWord.Word), word, Field.Store.YES),
                     new StringField(nameof(CodeWord.WordLower), word.ToLowerInvariant(), Field.Store.YES)
                };

                LucenePool.UpdateIndex(config.LuceneIndexForHint, new Lucene.Net.Index.Term(nameof(CodeWord.Word), word), document);

                totalUpdate++;

                if (needSaveIndex && batchSize > 0 && totalUpdate > batchSize)
                {
                    totalUpdate = 0;
                    LucenePool.SaveResultsAndClearLucenePool(config.LuceneIndexForHint);
                }
            }

            if (needSaveIndex && batchSize > 0 && totalUpdate > 0)
            {
                LucenePool.SaveResultsAndClearLucenePool(config.LuceneIndexForHint);
            }

            log?.Info($"Update hint index finished");
        }

        static void BuildIndex(CodeIndexConfiguration config, bool triggerMerge, bool applyAllDeletes, List<Document> documents, bool needFlush, ILog log)
        {
            log?.Info($"Build index start, documents count {documents.Count}");
            LucenePool.BuildIndex(config.LuceneIndexForHint, triggerMerge, applyAllDeletes, documents, needFlush);
            LucenePool.SaveResultsAndClearLucenePool(config.LuceneIndexForHint);
            log?.Info($"Build index finished");
        }

        static bool HasValidLength(this string content)
        {
            return content.Length > 3 && content.Length < 200;
        }
    }
}
