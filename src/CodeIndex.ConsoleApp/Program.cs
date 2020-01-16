using System;
using CodeIndex.MaintainIndex;

namespace CodeIndex.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var maintainer = new CodeFilesIndexMaintainer(@"D:\TestFolder\CodeFolder", @"D:\TestFolder\Index", Array.Empty<string>(), Array.Empty<string>());

            Console.ReadLine();
            Console.WriteLine("Stop");
            maintainer.Dispose();

            // TODO: Add Log Support
        }
    }
}
