using System;

namespace CodeIndex.Common
{
    public record SearchRequest
    {
        public Guid IndexPk { get; set; }
        public string Content { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string FilePath { get; set; }
        public bool CaseSensitive { get; set; }
        public bool PhaseQuery { get; set; }
        public int? ShowResults { get; set; }
        public bool Preview { get; set; }
        public bool NeedReplaceSuffixAndPrefix { get; set; }
        public bool ForWeb { get; set; }
    }
}
