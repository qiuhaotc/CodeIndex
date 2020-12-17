using System;
using CodeIndex.Common;
using CodeIndex.MaintainIndex;

namespace CodeIndex.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new NLogger();
            using var management = new IndexManagement(new CodeIndexConfiguration { LuceneIndex = @"D:\\TestFolder\\Index" }, logger);

            var indexLists = management.GetIndexList();

            if (indexLists.Status.Success)
            {
                if (indexLists.Result.Length == 0)
                {
                    var indexConfig = new IndexConfig
                    {
                        IndexName = "Test",
                        MonitorFolder = @"D:\TestFolder\CodeFolder",
                        ExcludedExtensions = ".DLL|.PBD",
                        ExcludedPaths = "\\DEBUG\\|\\RELEASE\\|\\RELEASES\\|\\BIN\\|\\OBJ\\|\\DEBUGPUBLIC\\",
                        IncludedExtensions = ".CS|.XML|.XAML|.JS|.TXT"
                    };

                    management.AddIndex(indexConfig);
                    management.StartIndex(indexConfig.Pk);
                }
                else
                {
                    management.StartIndex(indexLists.Result[0].IndexConfig.Pk);
                }
            }

            Console.WriteLine("Press any key to stop");
            Console.ReadLine();
            Console.WriteLine("Stop");
        }
    }
}
