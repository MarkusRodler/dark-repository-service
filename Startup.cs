using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace Dark
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration) => Configuration = configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHealthChecks();
            services.AddResponseCompression();

            services.AddSingleton(new FileSystemRepository("Data/"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.UseRouting();

            app.UseResponseCompression();

            app.UseMiddleware<ErrorHandlingMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/heartbeat");
                endpoints.MapGet("getIdsFor/{aggregate}", GetIdsFor);
                endpoints.MapGet("has/{aggregate}/{id}", Has);
                endpoints.MapGet("read/{aggregate}/{id}", Read);
                endpoints.MapPut("append/{aggregate}/{id}/{expectedVersion:int}", Append);
                endpoints.MapPost("overwrite/{aggregate}/{id}/{expectedVersion:int}", Overwrite);
            });
        }

        static async Task GetIdsFor(HttpContext context)
        {
            var aggregate = context.GetRouteValue("aggregate") + "";

            var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
            try
            {
                await context.Response.WriteAsJsonAsync(repository.GetIdsForAggregate(aggregate));
            }
            catch (Exception exception)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync(exception.Message);
            }
        }

        static Task Has(HttpContext context)
        {
            var aggregate = context.GetRouteValue("aggregate") + "";
            var id = context.GetRouteValue("id") + "";

            var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
            var hasEventLog = repository.Has(aggregate, id);
            if (hasEventLog is false)
            {
                context.Response.StatusCode = 404;
            }
            return Task.CompletedTask;
        }

        static async Task Read(HttpContext context)
        {
            var aggregate = context.GetRouteValue("aggregate") + "";
            var id = context.GetRouteValue("id") + "";

            context.Response.ContentType = "application/jsonl; charset=utf-8";
            var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
            try
            {
                var entries = repository.Read(aggregate, id);
                await context.Response.WriteAsync(string.Join("\n", entries));
            }
            catch (IOException exception)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync(exception.Message);
            }
        }

        static async Task Append(HttpContext context)
        {
            var aggregate = context.GetRouteValue("aggregate") + "";
            var id = context.GetRouteValue("id") + "";
            var expectedVersion = Convert.ToInt32(context.GetRouteValue("expectedVersion") ?? 0);
            var eventLog = ImmutableList.Create<string>();
            using (var reader = new StreamReader(context.Request.Body))
            {
                var line = "";
                while ((line = await reader.ReadLineAsync()) is not null)
                {
                    eventLog = eventLog.Add(line);
                }
            }

            var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
            try
            {
                await repository.Append(aggregate, id, eventLog, expectedVersion);
            }
            catch (ConcurrencyException)
            {
                throw new InvalidOperationException("Concurrency exception occured");
            }
        }

        static async Task Overwrite(HttpContext context)
        {
            var aggregate = context.GetRouteValue("aggregate") + "";
            var id = context.GetRouteValue("id") + "";
            var expectedVersion = Convert.ToInt32(context.GetRouteValue("expectedVersion") ?? 0);
            var eventLog = ImmutableList.Create<string>();
            using (var reader = new StreamReader(context.Request.Body))
            {
                var line = "";
                while ((line = await reader.ReadLineAsync()) is not null)
                {
                    eventLog = eventLog.Add(line);
                }
            }

            var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
            try
            {
                await repository.Overwrite(aggregate, id, eventLog, expectedVersion);
            }
            catch (ConcurrencyException)
            {
                throw new InvalidOperationException("Concurrency exception occured");
            }
        }
    }
}
