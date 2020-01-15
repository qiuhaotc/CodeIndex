using System;
using System.IO;

namespace CodeIndex.Common
{
    public class CodeSource
    {
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string FilePath { get; set; }
        public string Content { get; set; }
        public DateTime IndexDate { get; set; }

        public static CodeSource GetCodeSource(FileInfo fileInfo, string content)
        {
            return new CodeSource
            {
                FileExtension = fileInfo.Extension,
                FileName = fileInfo.Name,
                FilePath = fileInfo.FullName,
                IndexDate = DateTime.UtcNow,
                Content = content
            };
        }
    }
}
