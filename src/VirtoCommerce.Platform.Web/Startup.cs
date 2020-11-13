using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VirtoCommerce.Platform.App;
using VirtoCommerce.Platform.App.Extensions;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Modules;
using VirtoCommerce.Platform.Modules.Extensions;
using VirtoCommerce.Platform.Web.Middleware;
using VirtoCommerce.Platform.Web.PushNotifications;
using VirtoCommerce.Platform.Web.Swagger;

namespace VirtoCommerce.Platform.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            WebHostEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            HostConfiguration.WebRootPath = WebHostEnvironment.WebRootPath;
            HostConfiguration.ContentRootPath = WebHostEnvironment.ContentRootPath;
            HostConfiguration.IsDevelopment = WebHostEnvironment.IsDevelopment();

            //Get platform version from GetExecutingAssembly
            PlatformVersion.CurrentVersion = SemanticVersion.Parse(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion);

            services.AddOptions<PlatformOptions>().Bind(Configuration.GetSection("VirtoCommerce")).ValidateDataAnnotations();

            var mvcBuilder = services.AddCustomizedMvc();
            services.AddModules(Configuration, HostConfiguration.IsDevelopment, x => mvcBuilder.AddApplicationPartWithRelatedAssembly(x))
                .AddIOServices()
                .AddSwagger()
                .AddEvents();

            services.AddSignalR().AddPushNotifications(Configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseModules();
            app.UseSwagger();

            app.UseEndpoints(routes => routes.MapHub<PushNotificationHub>("/pushNotificationHub"));

            //Return all errors as Json response
            app.UseMiddleware<ApiErrorWrappingMiddleware>();
        }
    }
}
