using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            services.AddSingleton<FileSystemRepository>(new FileSystemRepository("Data/"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            else {
                app.UseExceptionHandler("/error");
            }

            app.UseRouting();

            app.UseResponseCompression();

            app.UseMiddleware<ErrorHandlingMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/heartbeat");
                endpoints.MapGet("getIdsFor/{aggregate}", getIdsFor);
                endpoints.MapGet("has/{aggregate}/{id}", has);
                endpoints.MapGet("read/{aggregate}/{id}", read);
                endpoints.MapPost("append/{aggregate}/{id}/{expectedVersion:int}", append);
                endpoints.MapPost("overwrite/{aggregate}/{id}/{expectedVersion:int}", overwrite);
            });
        }

        public static async Task getIdsFor(HttpContext context)
        {
            var aggregate = context.GetRouteValue("aggregate") as string;

            var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
            await context.Response.WriteAsJsonAsync(repository.getIdsForAggregate(aggregate));
        }

        public static Task has(HttpContext context)
        {
            var aggregate = context.GetRouteValue("aggregate") as string;
            var id = context.GetRouteValue("id") as string;

            var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
            var hasEventLog = repository.has(aggregate, id);
            if (hasEventLog is false) {
                context.Response.StatusCode = 404;
            }
            return Task.CompletedTask;
        }

        public static async Task read(HttpContext context)
        {
            var aggregate = context.GetRouteValue("aggregate") as string;
            var id = context.GetRouteValue("id") as string;

            context.Response.ContentType = "application/jsonl; charset=utf-8";
            var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
            try {
                var entries = repository.read(aggregate, id);
                await context.Response.WriteAsync(String.Join("\n", entries));
            }
            catch (IOException exception) {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync(exception.Message);
            }
        }

        public static async Task append(HttpContext context)
        {
            var aggregate = context.GetRouteValue("aggregate") as string;
            var id = context.GetRouteValue("id") as string;
            var expectedVersion = Convert.ToInt32(context.GetRouteValue("expectedVersion") ?? 0);
            var eventLog = ImmutableList.Create<string>();
            using (var reader = new StreamReader(context.Request.Body)) {
                var line = "";
                while ((line = await reader.ReadLineAsync()) is not null) {
                    eventLog = eventLog.Add(line);
                }
            }

            var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
            try {
                await repository.append(aggregate, id, eventLog, expectedVersion);
            }
            catch (ConcurrencyException) {
                throw new InvalidOperationException("Concurrency exception occured");
            }
        }

        public static async Task overwrite(HttpContext context)
        {
            var aggregate = context.GetRouteValue("aggregate") as string;
            var id = context.GetRouteValue("id") as string;
            var expectedVersion = Convert.ToInt32(context.GetRouteValue("expectedVersion") ?? 0);
            var eventLog = ImmutableList.Create<string>();
            using (var reader = new StreamReader(context.Request.Body)) {
                var line = "";
                while ((line = await reader.ReadLineAsync()) is not null) {
                    eventLog = eventLog.Add(line);
                }
            }

            var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
            try {
                await repository.overwrite(aggregate, id, eventLog, expectedVersion);
            }
            catch (ConcurrencyException) {
                throw new InvalidOperationException("Concurrency exception occured");
            }
        }
    }
}
