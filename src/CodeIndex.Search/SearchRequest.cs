using System;
using System.ComponentModel.DataAnnotations;

namespace CodeIndex.Search
{
    public record SearchRequest
    {
        public Guid IndexPk { get; set; }
        [MaxLength(1000)]
        public string Content { get; set; }
        [MaxLength(200)]
        public string FileName { get; set; }
        [MaxLength(20)]
        public string FileExtension { get; set; }
        [MaxLength(1000)]
        public string FilePath { get; set; }
        public bool CaseSensitive { get; set; }
        public bool PhaseQuery { get; set; }
        public int? ShowResults { get; set; }
        public bool Preview { get; set; }
        public bool NeedReplaceSuffixAndPrefix { get; set; }
        public bool ForWeb { get; set; }
        [MaxLength(32)]
        public string CodePK { get; set; }
        public bool IsEmpty => string.IsNullOrWhiteSpace(Content) && string.IsNullOrWhiteSpace(FileName) && string.IsNullOrWhiteSpace(FileExtension) && string.IsNullOrWhiteSpace(FilePath)
            || IndexPk == Guid.Empty;
    }
}
