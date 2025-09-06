[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(Event))]
[JsonSerializable(typeof(IAsyncEnumerable<string>))]
[JsonSerializable(typeof(string[]))]
sealed partial class JsonContext : JsonSerializerContext;
