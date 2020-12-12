using System;
using CodeIndex.Common;
using CodeIndex.MaintainIndex;

namespace CodeIndex.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //var config = new CodeIndexConfiguration
            //{
            //    MonitorFolder = @"D:\TestFolder\CodeFolder",
            //    LuceneIndex = @"D:\TestFolder\Index",
            //    ExcludedExtensions = ".DLL|.PBD",
            //    ExcludedPaths = "\\DEBUG\\|\\RELEASE\\|\\RELEASES\\|\\BIN\\|\\OBJ\\|\\DEBUGPUBLIC\\",
            //    IncludedExtensions = ".CS|.XML|.XAML|.JS|.TXT",
            //    SaveIntervalSeconds = 300
            //};

            //var logger = new NLogger();
            //var initializer = new IndexInitializer(logger);
            //var maintainer = new CodeFilesIndexMaintainer(config, logger);
            //maintainer.StartWatch();
            //initializer.InitializeIndex(config, out var failedIndexFiles);
            //maintainer.SetInitializeFinishedToTrue(failedIndexFiles);

            Console.WriteLine("Initialize complete");

            Console.WriteLine("Start monitoring, press any key to stop");
            Console.ReadLine();
            Console.WriteLine("Stop");
            //maintainer.Dispose();
        }
    }
}
