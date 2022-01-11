using System.Text.Json.Serialization;

[JsonSerializable(typeof(IImmutableList<string>))]
partial class MessageSerializerContext : JsonSerializerContext { }
