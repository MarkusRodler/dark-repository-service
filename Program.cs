using Dark;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddHealthChecks();
builder.Services.AddResponseCompression();
builder.Services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.AddContext<MessageSerializerContext>());

builder.Services.AddSingleton(new FileSystemRepository("Data/"));

var app = builder.Build();

app.UseExceptionHandler(c => c.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
    context.Response.StatusCode = error switch
    {
        IOException => 404,
        ConcurrencyException => 422,
        _ => 500
    };
    await context.Response.WriteAsJsonAsync(new { error = error?.Message });
}));

app.UseStaticFiles(
new StaticFileOptions
{
    ContentTypeProvider = new FileExtensionContentTypeProvider(
        new Dictionary<string, string>() { { ".jsonl", "application/jsonl; charset=utf-8" } }
    ),
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Data")),
    RequestPath = "/Read"
});

app.UseRouting();

app.UseResponseCompression();

app.MapHealthChecks("/heartbeat");
app.MapGet("favicon.ico", () => Results.NotFound());
app.MapGet("GetIdsFor/{aggregate}", (FileSystemRepository repository, string aggregate)
    => repository.GetIdsForAggregate(aggregate));
app.MapGet("Has/{aggregate}/{id}", async (FileSystemRepository repository, string aggregate, string id)
    => await repository.Has(aggregate, id) ? Results.Ok() : Results.NotFound());
app.MapGet("Read/{aggregate}/{id}", Read);
app.MapPut("Append/{aggregate}/{id}/{expectedVersion:int}", Append);
app.MapPost("Overwrite/{aggregate}/{id}/{expectedVersion:int}", Overwrite);

app.Run();

static async Task Read(HttpResponse response, FileSystemRepository repository, string aggregate, string id)
{
    response.ContentType = "application/jsonl; charset=utf-8";
    await response.WriteAsync(string.Join("\n", await repository.Read(aggregate, id)));
}

static async Task Append(
    HttpRequest request,
    FileSystemRepository repository,
    string aggregate,
    string id,
    int expectedVersion
)
{
    var eventLog = await ReadBodyAsListOfStrings(request);
    await repository.Append(aggregate, id, eventLog, expectedVersion);
}

static async Task Overwrite(
    HttpRequest request,
    FileSystemRepository repository,
    string aggregate,
    string id,
    int expectedVersion
)
{
    var eventLog = await ReadBodyAsListOfStrings(request);
    await repository.Overwrite(aggregate, id, eventLog, expectedVersion);
}

static async Task<IImmutableList<string>> ReadBodyAsListOfStrings(HttpRequest request)
{
    var list = ImmutableList.Create<string>();
    using (var reader = new StreamReader(request.Body))
    {
        var line = "";
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            list = list.Add(line);
        }
    }
    return list;
}
