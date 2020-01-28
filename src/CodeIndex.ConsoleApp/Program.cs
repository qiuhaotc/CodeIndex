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
            initializer.InitializeIndex(@"D:\TestFolder\CodeFolder", @"D:\TestFolder\Index", new[] { ".dll", ".pbd" }, new []{ "DEBUG/", "RELEASE/", "RELEASES/", "BIN/", "OBJ/", "LOG/", "DEBUGPUBLIC/" }, "*", new[] { ".cs", ".xml", ".xaml", ".js", ".txt" });
            Console.WriteLine("Initialize complete");
            var maintainer = new CodeFilesIndexMaintainer(@"D:\TestFolder\CodeFolder", @"D:\TestFolder\Index", new[]{".dll", ".pbd"}, new[] { "DEBUG/", "RELEASE/", "RELEASES/", "BIN/", "OBJ/", "LOG/", "DEBUGPUBLIC/" }, 300, new[] { ".cs", ".xml", ".xaml", ".js", ".txt" }, logger);
            Console.WriteLine("Start monitoring, press any key to stop");
            Console.ReadLine();
            Console.WriteLine("Stop");
            maintainer.Dispose();
        }
    }
}
