using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json.Serialization;
using System.Text.Json;

namespace cadApiDevCourseNET.GeojsonSchema
{
    public class GeoJsonSerializer
    {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Converters =
            {
                new GeometryConverter(),
                new PositionConverter(),
                new GeoJsonObjectConverter(),
                new JsonStringEnumConverter()
            }
        };

        private static readonly JsonSerializerOptions PrettyOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            Converters =
            {
                new GeometryConverter(),
                new PositionConverter(),
                new GeoJsonObjectConverter(),
                new JsonStringEnumConverter()
            }
        };

        public static string Serialize(IGeoJsonObject geoJsonObject, bool pretty = false)
        {
            var options = pretty ? PrettyOptions : DefaultOptions;
            return JsonSerializer.Serialize(geoJsonObject, geoJsonObject.GetType(), options);
        }

        public static T Deserialize<T>(string json) where T : IGeoJsonObject
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }

        public static IGeoJsonObject Deserialize(string json)
        {
            return JsonSerializer.Deserialize<GeoJsonObject>(json, DefaultOptions);
        }

        public static bool TryDeserialize(string json, out IGeoJsonObject result)
        {
            try
            {
                result = Deserialize(json);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }

    // Extension methods for easy conversion
    public static class GeoJsonExtensions
    {
        public static string ToGeoJson(this IGeoJsonObject geoJsonObject, bool pretty = false)
        {
            return GeoJsonSerializer.Serialize(geoJsonObject, pretty);
        }

        public static T FromGeoJson<T>(this string json) where T : IGeoJsonObject
        {
            return GeoJsonSerializer.Deserialize<T>(json);
        }
    }
}
