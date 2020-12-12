using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CodeIndex.Common;
using CodeIndex.MaintainIndex;
using CodeIndex.Search;
using Microsoft.AspNetCore.Authentication.Cookies;
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
        // TODO: Add swagger support

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = ctx =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api"))
                        {
                            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        }
                        else
                        {
                            ctx.Response.Redirect(ctx.RedirectUri);
                        }

                        return Task.FromResult(0);
                    }
                };
            });

            services.AddMvc();
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<ILog>(new NLogger());
            services.AddSingleton<IndexManagement>();
            services.AddScoped<Storage>();

            config = new CodeIndexConfiguration();
            Configuration.GetSection("CodeIndex").Bind(config);
            services.AddSingleton(config);

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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifeTime, ILog log, IndexManagement indexManagement)
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

            // TODO: Delete

            maintainer = new IndexMaintainer(new IndexConfig
            {
                ExcludedExtensions = config.ExcludedExtensions,
                ExcludedPaths = config.ExcludedPaths,
                IncludedExtensions = config.IncludedExtensions,
                IndexName = "TestIndex",
                MaxContentHighlightLength = config.MaxContentHighlightLength,
                MonitorFolder = config.MonitorFolder,
                OpenIDEUriFormat = config.OpenIDEUriFormat,
                MonitorFolderRealPath = config.MonitorFolderRealPath,
                SaveIntervalSeconds = config.SaveIntervalSeconds
            }, config, log);

            maintainer.InitializeIndex(false).ContinueWith(u => maintainer.MaintainIndexes());

            CodeIndexSearcherLight = new CodeIndexSearcherLight(maintainer);

            IndexManagement = indexManagement;
        }

        IndexMaintainer maintainer;
        CodeIndexConfiguration config;
        public static CodeIndexSearcherLight CodeIndexSearcherLight { get; private set; }
        public static IndexManagement IndexManagement { get; private set; }

        void OnShutdown()
        {
            maintainer?.Dispose();
            IndexManagement?.Dispose();
        }
    }
}
