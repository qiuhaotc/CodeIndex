using System.IO;
using System.Text;

namespace CodeIndex.Files
{
    public static class FilesEncodingHelper
    {
        public static Encoding GetEncoding(string fullPath)
        {
            Encoding encoding;

            using (var reader = new StreamReader(fullPath, Encoding.UTF8, true))
            {
                reader.Peek();
                encoding = reader.CurrentEncoding;
            }

            return encoding;
        }
    }
}