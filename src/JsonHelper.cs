
using System.Text.Json;

namespace MeshTunnel
{
    public static class JsonHelper
    {
        public static Dictionary<string, object>? Deserialize(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return ReadElement(doc.RootElement) as Dictionary<string, object>;
        }

        private static object? ReadElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        dict[prop.Name] = ReadElement(prop.Value);
                    }
                    return dict;

                case JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ReadElement(item));
                    }
                    return list;

                case JsonValueKind.String:
                    if (element.TryGetDateTime(out DateTime dt))
                        return dt;
                    return element.GetString();

                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long l))
                        return l;
                    return element.GetDouble();

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;

                default:
                    throw new NotSupportedException($"Unsupported JSON token: {element.ValueKind}");
            }
        }
    }
}