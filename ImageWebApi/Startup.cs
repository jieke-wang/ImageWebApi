using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ImageWebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCaching(options =>
            {
                options.SizeLimit = 8_589_934_592; // 最大缓存 1GB
                options.MaximumBodySize = 536_870_912; // 64M
                options.UseCaseSensitivePaths = false; // 缓存的路径是否区分大小写
            });
            services.AddResponseCompression(options =>
            {
                options.MimeTypes = new List<string> { "text/plain", "text/html", "text/xml", "application/json", "text/css", "application/x-javascript", "application/javascript" };
                options.ExcludedMimeTypes = new List<string> { "application/octet-stream" };
                options.EnableForHttps = true;

                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });

            services
                .AddHttpClient("RemoteIamgePoolClient")
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return new SocketsHttpHandler
                    {
                        PooledConnectionLifetime = TimeSpan.FromMinutes(60),
                        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10),
                        MaxConnectionsPerServer = 10,
                        UseCookies = false,
                        AllowAutoRedirect = true,
                    };
                });

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ImageWebApi", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ImageWebApi v1"));

            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseStaticFiles();

            app.UseResponseCaching();
            //app.Use(async (context, next) =>
            //{
            //    context.Response.GetTypedHeaders().CacheControl =
            //        new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
            //        {
            //            Public = true,
            //            MaxAge = TimeSpan.FromDays(365),
            //        };
            //    context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
            //        new string[] { "Accept-Encoding" };

            //    await next();

            //    //if (context.Response.StatusCode == 404)
            //    //{
            //    //    context.Response.GetTypedHeaders().CacheControl =
            //    //        new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
            //    //        {
            //    //            Public = true,
            //    //            MaxAge = TimeSpan.FromSeconds(10),
            //    //        };
            //    //    context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
            //    //        new string[] { "Accept-Encoding" };
            //    //}
            //});

            app.UseResponseCompression();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.Run(async context =>
            {
                context.Response.StatusCode = 404;
                context.Response.ContentType = Configuration["ImageApiSetting:DefaultImageMimeType"];
                await context.Response.Body.WriteAsync(await System.IO.File.ReadAllBytesAsync(System.IO.Path.Combine(env.ContentRootPath, Configuration["ImageApiSetting:ImageRootDir"], Configuration["ImageApiSetting:DefaultImage"])));
            });
        }
    }
}
