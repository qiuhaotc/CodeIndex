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

        public static CodeSource GetCodeSource(DirectoryInfo directoryInfo)
        {
            return new CodeSource
            {
                FileExtension = directoryInfo.Extension,
                FileName = directoryInfo.Name,
                FilePath = directoryInfo.FullName,
                IndexDate = DateTime.UtcNow,
                Content = File.ReadAllText(directoryInfo.FullName)
            };
        }
    }
}
