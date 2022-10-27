using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Exceptional;

namespace Samples.AspNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            HostingEnvironment = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            // Make IOptions<ExceptionalSettings> available for injection everywhere
            services.AddExceptional(Configuration.GetSection("Exceptional"), settings =>
            {
                //settings.DefaultStore.ApplicationName = "Samples.AspNetCore";
                settings.UseExceptionalPageOnThrow = HostingEnvironment.IsDevelopment();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            new Exception("Startup test exception - see how I'm captured! This happens due to a pre-.Configure() IStartupFilter").LogNoContext();
            // Boilerplate we're no longer using with Exceptional
            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //    app.UseBrowserLink();
            //}
            //else
            //{
            //    app.UseExceptionHandler("/Home/Error");
            //}
            app.UseExceptional();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}"));
        }
    }
}
