using System.IO;
using System.Text;

namespace CodeIndex.Files
{
    public static class FilesContentHelper
    {
        public static string ReadAllText(string fullName)
        {
            using var fileStream = new FileStream(fullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true);

            return streamReader.ReadToEnd();
        }
    }
}