using System.Text.Json;
using System.Text.Json.Serialization;

namespace Thanos.Parsers;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(MoveRequest))]
public partial class AppJsonSerializerContext : JsonSerializerContext;