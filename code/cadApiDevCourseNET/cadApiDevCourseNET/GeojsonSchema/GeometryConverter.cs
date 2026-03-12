using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json.Serialization;
using System.Text.Json;

namespace cadApiDevCourseNET.GeojsonSchema
{
    public class GeometryConverter : JsonConverter<GJ_Geometry>
    {
        public override GJ_Geometry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
                throw new JsonException("Missing 'type' property");

            var type = typeElement.GetString();

            return type switch
            {
                "Point" => JsonSerializer.Deserialize<GJ_Point>(root.GetRawText(), options),
                "MultiPoint" => JsonSerializer.Deserialize<GJ_MultiPoint>(root.GetRawText(), options),
                "LineString" => JsonSerializer.Deserialize<GJ_LineString>(root.GetRawText(), options),
                "MultiLineString" => JsonSerializer.Deserialize<GJ_MultiLineString>(root.GetRawText(), options),
                "Polygon" => JsonSerializer.Deserialize<GJ_Polygon>(root.GetRawText(), options),
                "MultiPolygon" => JsonSerializer.Deserialize<GJ_MultiPolygon>(root.GetRawText(), options),
                "GeometryCollection" => JsonSerializer.Deserialize<GJ_GeometryCollection>(root.GetRawText(), options),
                _ => throw new JsonException($"Unknown geometry type: {type}")
            };
        }

        public override void Write(Utf8JsonWriter writer, GJ_Geometry value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }
    }

    public class PositionConverter : JsonConverter<GJ_Position>
    {
        public override GJ_Position Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected start of array");

            var coordinates = new List<double>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                if (reader.TokenType == JsonTokenType.Number)
                    coordinates.Add(reader.GetDouble());
            }

            return GJ_Position.FromArray(coordinates.ToArray());
        }

        public override void Write(Utf8JsonWriter writer, GJ_Position value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.Longitude);
            writer.WriteNumberValue(value.Latitude);

            if (value.Altitude.HasValue)
                writer.WriteNumberValue(value.Altitude.Value);

            writer.WriteEndArray();
        }
    }

    public class GeoJsonObjectConverter : JsonConverter<GeoJsonObject>
    {
        public override GeoJsonObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
                throw new JsonException("Missing 'type' property");

            var type = typeElement.GetString();

            return type switch
            {
                "Feature" => JsonSerializer.Deserialize<GJ_Feature>(root.GetRawText(), options),
                "FeatureCollection" => JsonSerializer.Deserialize<FeatureCollection>(root.GetRawText(), options),
                "Point" or "MultiPoint" or "LineString" or "MultiLineString"
                    or "Polygon" or "MultiPolygon" or "GeometryCollection"
                    => JsonSerializer.Deserialize<GJ_Geometry>(root.GetRawText(), options),
                _ => throw new JsonException($"Unknown GeoJSON type: {type}")
            };
        }

        public override void Write(Utf8JsonWriter writer, GeoJsonObject value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }
    }
}
