// JsonUtility stand-in. Unity's serialiser maps public fields; System.Text.Json
// maps properties by default, so fields are opted in explicitly.
using System.Text.Json;

namespace UnityEngine
{
    internal static class StubJson
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
        };

        public static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
    }
}
