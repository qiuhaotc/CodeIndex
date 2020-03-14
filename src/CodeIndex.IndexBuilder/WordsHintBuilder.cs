using System.Collections.Generic;
using CodeIndex.Common;
using Lucene.Net.Documents;

namespace CodeIndex.IndexBuilder
{
    public static class WordsHintBuilder
    {
        // TODO: Add test

        static HashSet<string> Words { get; } = new HashSet<string>();

        public static void BuildIndexByBatch(CodeIndexConfiguration config, bool triggerMerge, bool applyAllDeletes, bool needFlush, ILog log, int batchSize = 1000)
        {
            config.RequireNotNull(nameof(config));
            batchSize.RequireRange(nameof(batchSize), int.MaxValue, 50);

            var documents = new List<Document>();
            
            log?.Info($"Start build hint words for {config.LuceneIndexForHint}");

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

            Words.Clear();

            log?.Info($"Finished build hint words for {config.LuceneIndexForHint}");
        }

        internal static void AddWords(string[] words)
        {
            foreach (var word in words)
            {
                if (word.Length > 3)
                {
                    Words.Add(word);
                }
            }
        }

        static void BuildIndex(CodeIndexConfiguration config, bool triggerMerge, bool applyAllDeletes, List<Document> documents, bool needFlush, ILog log)
        {
            log?.Info($"Build index start, documents count {documents.Count}");
            LucenePool.BuildIndex(config.LuceneIndexForHint, triggerMerge, applyAllDeletes, documents, needFlush);
            LucenePool.SaveResultsAndClearLucenePool(config.LuceneIndexForHint);
            log?.Info($"Build index finished");
        }
    }
}
