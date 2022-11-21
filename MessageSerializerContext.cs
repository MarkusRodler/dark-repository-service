using Dark;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(IImmutableList<string>))]
sealed partial class MessageSerializerContext : JsonSerializerContext { }
