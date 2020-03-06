using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.MaintainIndex;
using CodeIndex.Server.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CodeIndex.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            if (File.Exists("Nlog.config") && NLog.LogManager.Configuration == null)
            {
                NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("Nlog.config");
            }

            services.AddMvc();
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<WeatherForecastService>();
            services.AddSingleton<ILog>(new NLogger());
            services.AddScoped<Storage>();

            // Server Side Blazor doesn't register HttpClient by default
            if (!services.Any(x => x.ServiceType == typeof(HttpClient)))
            {
                // Setup HttpClient for server side in a client side compatible fashion
                services.AddScoped(s =>
                {
                    // Creating the URI helper needs to wait until the JS Runtime is initialized, so defer it.
                    var uriHelper = s.GetRequiredService<NavigationManager>();
                    return new HttpClient
                    {
                        BaseAddress = new Uri(uriHelper.BaseUri)
                    };
                });
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifeTime, ILog log)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            lifeTime.ApplicationStopping.Register(OnShutdown);

            System.Threading.Tasks.Task.Run(() => {
                try
                {
                    var initializer = new IndexInitializer(log);
                    initializer.InitializeIndex(MonitorFolder, LuceneIndex, new[] { ".dll", ".pbd" }, new[] { "DEBUG/", "RELEASE/", "RELEASES/", "BIN/", "OBJ/", "LOG/", "DEBUGPUBLIC/" }, "*", new[] { ".cs", ".xml", ".xaml", ".js", ".txt" });

                    log.Info("Initialize complete");

                    maintainer = new CodeFilesIndexMaintainer(MonitorFolder, LuceneIndex, new[] { ".dll", ".pbd" }, new[] { "DEBUG/", "RELEASE/", "RELEASES/", "BIN/", "OBJ/", "LOG/", "DEBUGPUBLIC/" }, 300, new[] { ".cs", ".xml", ".xaml", ".js", ".txt" }, log);

                    log.Info("Start monitoring");
                }
                catch (Exception ex)
                {
                    log.Error(ex.ToString());
                }
            });
        }

        CodeFilesIndexMaintainer maintainer;

        void OnShutdown()
        {
            maintainer?.Dispose();
            LucenePool.SaveResultsAndClearLucenePool(Configuration.GetValue<string>("LuceneIndex"));
        }

        string LuceneIndex => Configuration.GetValue<string>("LuceneIndex");
        string MonitorFolder => Configuration.GetValue<string>("MonitorFolder");
    }
}
