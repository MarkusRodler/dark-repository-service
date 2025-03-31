using Dark;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(string[]))]
sealed partial class MessageSerializerContext : JsonSerializerContext;
