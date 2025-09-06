var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddHealthChecks();
builder.Services.AddResponseCompression();

builder.Services.ConfigureHttpJsonOptions(x => x.SerializerOptions.TypeInfoResolver = JsonContext.Default);
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
    await context.Response.WriteAsJsonAsync(errorResponse, JsonContext.Default.ErrorResponse);
}));

app.UseStaticFiles(
    new StaticFileOptions()
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

app.MapGet("GetIdsFor/{aggregate}", async (string aggregate, FileSystemRepository repo)
    => Results.Json(await repo.GetIdsForAggregate(aggregate), JsonContext.Default));

app.MapGet("Has/{aggregate}/{id}", async ([AsParameters] Data data, FileSystemRepository repo)
    => (await repo.Has(data)) ? Results.Ok() : Results.NotFound());

app.MapGet("Read/{aggregate}/{id}/{afterLine:int?}", async ([AsParameters] Read x) =>
{
    x.Response.ContentType = "application/jsonl; charset=utf-8";
    await foreach (var @event in x.Repo.Read(new(x.Aggregate, x.Id), new(x.Query, x.AfterLine), x.Ct))
    {
        await x.Response.WriteAsync(@event + '\n', x.Ct);
    }
});

app.MapPut("Append/{aggregate}/{id}/{version:int}", async ([AsParameters] Write x)
    => await x.Repo.Append(x.Data, await x.Body.AsStringArray(x.Ct), x.Condition, x.Ct));

app.MapPost("Overwrite/{aggregate}/{id}/{version:int}", async ([AsParameters] Write x)
    => await x.Repo.Overwrite(x.Data, await x.Body.AsStringArray(x.Ct), x.Condition, x.Ct));

app.Run();

struct Read
{
    public string Aggregate { get; set; }
    public string Id { get; set; }
    [FromQuery] public string? Query { get; set; }
    public FileSystemRepository Repo { get; set; }
    public HttpResponse Response { get; set; }
    public CancellationToken Ct { get; set; }
    public int? AfterLine { get; set; }
}

struct Write
{
    public string Aggregate { get; set; }
    public string Id { get; set; }
    [FromQuery] public string? FailIf { get; set; }
    public Stream Body { get; set; }
    public FileSystemRepository Repo { get; set; }
    public CancellationToken Ct { get; set; }
    public int? Version { get; set; }

    public readonly Data Data => new(Aggregate, Id);
    public readonly Condition Condition => new(FailIf, Version);
}
