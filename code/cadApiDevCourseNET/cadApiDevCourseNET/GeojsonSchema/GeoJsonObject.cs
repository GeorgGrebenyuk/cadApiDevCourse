using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json.Serialization;
using System.Text.Json;

namespace cadApiDevCourseNET.GeojsonSchema
{
    // Base interface for all GeoJSON objects
    public interface IGeoJsonObject
    {
        string Type { get; }
    }

    // Base class for all GeoJSON objects
    public abstract class GeoJsonObject : IGeoJsonObject
    {
        [JsonPropertyName("type")]
        public abstract string Type { get; }
    }

    // Geometry types
    public abstract class GJ_Geometry : GeoJsonObject { }

    public class GJ_Point : GJ_Geometry
    {
        public override string Type => "Point";

        [JsonPropertyName("coordinates")]
        public GJ_Position Coordinates { get; set; }

        public GJ_Point()
        {
            Coordinates = new GJ_Position(0, 0, 0);
        }
    }

    public class GJ_MultiPoint : GJ_Geometry
    {
        public override string Type => "MultiPoint";

        [JsonPropertyName("coordinates")]
        public List<GJ_Position> Coordinates { get; set; }

        public GJ_MultiPoint()
        {
            Coordinates = new List<GJ_Position>();
        }
    }

    public class GJ_LineString : GJ_Geometry
    {
        public override string Type => "LineString";

        [JsonPropertyName("coordinates")]
        public List<GJ_Position> Coordinates { get; set; }

        public GJ_LineString()
        {
            Coordinates = new List<GJ_Position>();
        }
    }

    public class GJ_MultiLineString : GJ_Geometry
    {
        public override string Type => "MultiLineString";

        [JsonPropertyName("coordinates")]
        public List<List<GJ_Position>> Coordinates { get; set; }
        public GJ_MultiLineString()
        {
            Coordinates = new List<List<GJ_Position>>();
        }
    }

    public class GJ_Polygon : GJ_Geometry
    {
        public override string Type => "Polygon";

        [JsonPropertyName("coordinates")]
        public List<List<GJ_Position>> Coordinates { get; set; }

        public GJ_Polygon()
        {
            Coordinates = new List<List<GJ_Position>>();
        }
    }

    public class GJ_MultiPolygon : GJ_Geometry
    {
        public override string Type => "MultiPolygon";

        [JsonPropertyName("coordinates")]
        public List<List<List<GJ_Position>>> Coordinates { get; set; }

        public GJ_MultiPolygon()
        {
            Coordinates = new List<List<List<GJ_Position>>>();
        }
    }

    public class GJ_GeometryCollection : GJ_Geometry
    {
        public override string Type => "GeometryCollection";

        [JsonPropertyName("geometries")]
        public List<GJ_Geometry> Geometries { get; set; }

        public GJ_GeometryCollection()
        {
            Geometries = new List<GJ_Geometry>();
        }
    }

    // Feature
    public class GJ_Feature : GeoJsonObject
    {
        public override string Type => "Feature";

        [JsonPropertyName("geometry")]
        public GJ_Geometry Geometry { get; set; }

        [JsonPropertyName("properties")]
        public Dictionary<string, object> Properties { get; set; }

        public GJ_Feature()
        {
            Properties = new Dictionary<string, object>();
        }
    }

    // FeatureCollection
    public class FeatureCollection : GeoJsonObject
    {
        public override string Type => "FeatureCollection";

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("features")]
        public List<GJ_Feature> Features { get; set; }

        public FeatureCollection()
        {
            Features = new List<GJ_Feature>();
        }
    }

    // Position (coordinates)
    public class GJ_Position
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double? Altitude { get; set; }

        public GJ_Position(double longitude, double latitude, double? altitude = null)
        {
            Longitude = longitude;
            Latitude = latitude;
            Altitude = altitude;
        }

        public double[] ToArray()
        {
            return Altitude.HasValue
                ? new[] { Longitude, Latitude, Altitude.Value }
                : new[] { Longitude, Latitude };
        }

        public static GJ_Position FromArray(double[] coordinates)
        {
            if (coordinates == null || coordinates.Length < 2)
                throw new ArgumentException("Coordinates array must have at least 2 elements");

            return coordinates.Length == 3
                ? new GJ_Position(coordinates[0], coordinates[1], coordinates[2])
                : new GJ_Position(coordinates[0], coordinates[1]);
        }
    }
}
