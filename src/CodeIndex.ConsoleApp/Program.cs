using System;
using CodeIndex.MaintainIndex;

namespace CodeIndex.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var initializer = new IndexInitializer();
            initializer.InitializeIndex(@"D:\TestFolder\CodeFolder", @"D:\TestFolder\Index", new[] { ".dll", ".pbd" }, new []{ "DEBUG/", "RELEASE/", "RELEASES/", "BIN/", "OBJ/", "LOG/", "DEBUGPUBLIC/" }, "*", new[] { ".cs", ".xml", ".xaml", ".js" });
            Console.WriteLine("Initialize complete");
            var maintainer = new CodeFilesIndexMaintainer(@"D:\TestFolder\CodeFolder", @"D:\TestFolder\Index", new[]{".dll", ".pbd"}, new[] { "DEBUG/", "RELEASE/", "RELEASES/", "BIN/", "OBJ/", "LOG/", "DEBUGPUBLIC/" }, 300, new[] { ".cs", ".xml", ".xaml", ".js" });
            Console.WriteLine("Start monitoring, press any key to stop");
            Console.ReadLine();
            Console.WriteLine("Stop");
            maintainer.Dispose();

            // TODO: Add Log Support
        }
    }
}
