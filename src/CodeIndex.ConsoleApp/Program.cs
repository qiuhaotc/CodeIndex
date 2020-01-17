using System;
using CodeIndex.MaintainIndex;

namespace CodeIndex.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var initializer = new IndexInitializer();
            initializer.InitializeIndex(@"D:\TestFolder\CodeFolder", @"D:\TestFolder\Index");
            Console.WriteLine("Initialize complete");
            var maintainer = new CodeFilesIndexMaintainer(@"D:\TestFolder\CodeFolder", @"D:\TestFolder\Index", Array.Empty<string>(), Array.Empty<string>());
            Console.WriteLine("Start monitoring, press any key to stop");
            Console.ReadLine();
            Console.WriteLine("Stop");
            maintainer.Dispose();

            // TODO: Add Log Support
        }
    }
}
