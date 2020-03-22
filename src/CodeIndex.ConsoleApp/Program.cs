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
            var initializer = new IndexInitializer(logger);
            var maintainer = new CodeFilesIndexMaintainer(new CodeIndexConfiguration { MonitorFolder = @"D:\TestFolder\CodeFolder", LuceneIndex = @"D:\TestFolder\Index" }, new[] { ".dll", ".pbd" }, new[] { "DEBUG/", "RELEASE/", "RELEASES/", "BIN/", "OBJ/", "LOG/", "DEBUGPUBLIC/" }, 300, new[] { ".cs", ".xml", ".xaml", ".js", ".txt" }, logger);
            maintainer.StartWatch();
            initializer.InitializeIndex(new CodeIndexConfiguration { MonitorFolder = @"D:\TestFolder\CodeFolder", LuceneIndex = @"D:\TestFolder\Index" }, new[] { ".dll", ".pbd" }, new[] { "DEBUG/", "RELEASE/", "RELEASES/", "BIN/", "OBJ/", "LOG/", "DEBUGPUBLIC/" }, out var failedIndexFiles, "*", new[] { ".cs", ".xml", ".xaml", ".js", ".txt" });
            maintainer.SetInitalizeFinishedToTrue(failedIndexFiles);

            Console.WriteLine("Initialize complete");

            Console.WriteLine("Start monitoring, press any key to stop");
            Console.ReadLine();
            Console.WriteLine("Stop");
            maintainer.Dispose();
        }
    }
}
