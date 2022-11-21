using Dark;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddHealthChecks();
builder.Services.AddResponseCompression();

MessageSerializerContext jsonSerializerContext = new();
builder.Services.AddSingleton(jsonSerializerContext);
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
    await context.Response.WriteAsJsonAsync(errorResponse, jsonSerializerContext.ErrorResponse);
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

app.MapHealthChecks("/heartbeat");
app.MapGet("favicon.ico", RespondWith.Status(StatusCodes.Status404NotFound));
app.MapGet("GetIdsFor/{aggregate}", RespondWith.GetIdsForAggregate);
app.MapGet("Has/{aggregate}/{id}", RespondWith.Has);
app.MapGet("Read/{aggregate}/{id}", RespondWith.Read);
app.MapPut("Append/{aggregate}/{id}/{expectedVersion:int}", RespondWith.Append);
app.MapPost("Overwrite/{aggregate}/{id}/{expectedVersion:int}", RespondWith.Overwrite);

app.Run();
