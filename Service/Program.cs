using Dark;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddHealthChecks();
builder.Services.AddResponseCompression();

builder.Services.ConfigureHttpJsonOptions(x => x.SerializerOptions.TypeInfoResolver = MessageSerializerContext.Default);
builder.Services.AddSingleton(new FileSystemRepository("Data/"));

var app = builder.Build();

app.UseExceptionHandler(c => c.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
    context.Response.StatusCode = error switch
    {
        IOException => StatusCodes.Status404NotFound,
        ConcurrencyException => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError
    };
    ErrorResponse errorResponse = new(error?.Message ?? "");
    await context.Response.WriteAsJsonAsync(errorResponse, MessageSerializerContext.Default.ErrorResponse);
}));

app.UseStaticFiles(
    new StaticFileOptions
    {
        ContentTypeProvider = new FileExtensionContentTypeProvider(
            new Dictionary<string, string>() { { ".jsonl", "application/jsonl; charset=utf-8" } }
        ),
        FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Data")),
        RequestPath = "/Read"
    }
);

app.UseRouting();

app.UseResponseCompression();

app.MapHealthChecks("/heartbeat").DisableHttpMetrics();
app.MapShortCircuit(404, "favicon.ico");

app.MapGet("GetIdsFor/{aggregate}", async (string aggregate, FileSystemRepository repository)
    => Results.Json(await repository.GetIdsForAggregate(aggregate), MessageSerializerContext.Default));

app.MapGet("Has/{aggregate}/{id}", async (string aggregate, string id, FileSystemRepository repository)
    => (await repository.Has(aggregate, id)) ? Results.Ok() : Results.NotFound());

app.MapGet("Read/{aggregate}/{id}", async (string aggregate, string id, FileSystemRepository repository)
    => Results.Text(string.Join('\n', await repository.Read(aggregate, id)), "application/jsonl; charset=utf-8"));

app.MapPut(
    "Append/{aggregate}/{id}/{expectedVersion:int}",
    async (string aggregate, string id, int expectedVersion, Stream body, FileSystemRepository repository)
        => await repository.Append(aggregate, id, await ReadAsStringArray(body), expectedVersion));

app.MapPost(
    "Overwrite/{aggregate}/{id}/{expectedVersion:int}",
    async (string aggregate, string id, int expectedVersion, Stream body, FileSystemRepository repository)
        => await repository.Overwrite(aggregate, id, await ReadAsStringArray(body), expectedVersion));

app.Run();

static async Task<string[]> ReadAsStringArray(Stream body) => (await new StreamReader(body).ReadToEndAsync()).Split('\n');
