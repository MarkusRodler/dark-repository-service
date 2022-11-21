namespace Dark;

public static class RespondWith
{
    public static RequestDelegate Status(int code)
        => (HttpContext c) => { c.Response.StatusCode = code; return Task.CompletedTask; };

    public static async Task GetIdsForAggregate(HttpContext context)
    {
        var aggregate = (string)context.Request.RouteValues["aggregate"]!;
        var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
        var jsonSerializerContext = context.RequestServices.GetRequiredService<MessageSerializerContext>();
        var ids = await repository.GetIdsForAggregate(aggregate);
        await context.Response.WriteAsJsonAsync(ids, jsonSerializerContext.IImmutableListString);
    }

    public static async Task Has(HttpContext context)
    {
        var aggregate = (string)context.Request.RouteValues["aggregate"]!;
        var id = (string)context.Request.RouteValues["id"]!;
        var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
        if (await repository.Has(aggregate, id) is false)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }

    public static async Task Read(HttpContext context)
    {
        var aggregate = (string)context.Request.RouteValues["aggregate"]!;
        var id = (string)context.Request.RouteValues["id"]!;
        var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
        context.Response.ContentType = "application/jsonl; charset=utf-8";
        await context.Response.WriteAsync(string.Join('\n', await repository.Read(aggregate, id)));
    }

    public static async Task Append(HttpContext context)
    {
        var aggregate = (string)context.Request.RouteValues["aggregate"]!;
        var id = (string)context.Request.RouteValues["id"]!;
        var expectedVersion = Convert.ToInt32(context.Request.RouteValues["expectedVersion"]);
        var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
        var eventLog = await ReadBodyAsListOfStrings(context.Request);
        await repository.Append(aggregate, id, eventLog, expectedVersion);
    }

    public static async Task Overwrite(HttpContext context)
    {
        var aggregate = (string)context.Request.RouteValues["aggregate"]!;
        var id = (string)context.Request.RouteValues["id"]!;
        var expectedVersion = Convert.ToInt32(context.Request.RouteValues["expectedVersion"]);
        var repository = context.RequestServices.GetRequiredService<FileSystemRepository>();
        var eventLog = await ReadBodyAsListOfStrings(context.Request);
        await repository.Overwrite(aggregate, id, eventLog, expectedVersion);
    }

    static async Task<IImmutableList<string>> ReadBodyAsListOfStrings(HttpRequest request)
    {
        var list = ImmutableList.Create<string>();
        using (StreamReader reader = new(request.Body))
        {
            var line = "";
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                list = list.Add(line);
            }
        }
        return list;
    }

}
