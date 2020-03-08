using System;
using System.IO;
using System.Linq;
using System.Text;

namespace CodeIndex.Files
{
    public static class FilesContentHelper
    {
        public static string ReadAllText(string fullName)
        {
            byte[] contents;

            using (var fileStream = new FileStream(fullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var fsLen = (int)fileStream.Length;
                contents = new byte[fsLen];
                fileStream.Read(contents, 0, contents.Length);
            }

            var bom = new byte[4];
            for (int index = 0; index < contents.Length && index < 4; index++)
            {
                bom[index] = contents[index];
            }

            var encoding = GetEncoding(bom);

            return encoding.GetString(contents);
        }

        /// <summary>
        /// Determines a text file's encoding by analyzing its byte order mark (BOM).
        /// Defaults to ASCII when detection of the text file's endianness fails.
        /// </summary>
        /// <param name="filename">The text file to analyze.</param>
        /// <returns>The detected encoding.</returns>
        static Encoding GetEncoding(byte[] bom)
        {
            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;

            return Encoding.Default;
        }
    }
}