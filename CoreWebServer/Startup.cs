using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Rock.Rest;

namespace CoreWebServer
{
    public class Startup
    {
        public Startup( IConfiguration configuration )
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices( IServiceCollection services )
        {
            services.AddAuthentication( CookieAuthenticationDefaults.AuthenticationScheme )
                .AddCookie( o =>
                {
                    o.LoginPath = "/api/Auth/Login";
                    o.Cookie.Name = ".ROCK";
                } );

            services.AddMvc(o => o.EnableEndpointRouting = false)
                .SetCompatibilityVersion( CompatibilityVersion.Version_2_2 )
                .AddJsonOptions( options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Unspecified;
                } );

            services.AddHttpContextAccessor();

            services.AddOData();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure( IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory )
        {
            if ( env.IsDevelopment() )
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();
            app.UseMiddleware<Rock.Rest.Filters.ApiKeyMiddleware>();

            app.UseMvc( routeBuilder =>
            {
                routeBuilder.EnableDependencyInjection();
            } );

            System.Web.HttpContext.Configure( app.ApplicationServices.GetRequiredService<IHttpContextAccessor>() );

            app.UseRockApi();

            new TestContext().Database.Migrate();
        }
    }

    public class TestContext : Rock.Data.DbContext
    {
        public TestContext() : base( "RockContext" ) { }
    }
}
