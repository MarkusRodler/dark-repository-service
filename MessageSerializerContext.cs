using System.Text.Json.Serialization;

[JsonSerializable(typeof(IImmutableList<string>))]
sealed partial class MessageSerializerContext : JsonSerializerContext { }
