using System;
using CodeIndex.Common;
using CodeIndex.MaintainIndex;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace CodeIndex.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            var servicesProvider = BuildDi(config);
            using (servicesProvider as IDisposable)
            {
                using var management = servicesProvider.GetRequiredService<IndexManagement>();

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

            LogManager.Shutdown();
        }

        static IServiceProvider BuildDi(IConfiguration config)
        {
            return new ServiceCollection()
                .AddSingleton<IndexManagement>() // Runner is the custom class
                .AddLogging(loggingBuilder =>
                {
                    // configure Logging with NLog
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog(config);
                })
                .AddSingleton(new CodeIndexConfiguration { LuceneIndex = @"D:\\TestFolder\\Index" })
                .BuildServiceProvider();
        }
    }
}
