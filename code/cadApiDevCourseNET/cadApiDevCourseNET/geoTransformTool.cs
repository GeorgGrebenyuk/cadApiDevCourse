
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NCAD
using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.Geometry;
using Teigha.DatabaseServices;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
#endif

namespace cadApiDevCourseNET
{
    struct Wgs84Point
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;

        public Wgs84Point(double lat, double lon, double altitude = 0.0)
        {
            Latitude = lat;
            Longitude = lon;
            Altitude = altitude;
        }

    }

    public class PseudoMercatorConverter
    {
        // Earth's radius in meters for Pseudo-Mercator (EPSG:3857)
        private const double EarthRadius = 6378137; // WGS84 semi-major axis

        // Max latitude for Pseudo-Mercator (approximately 85.051129°)
        // This is the limit to avoid mathematical singularities
        private const double MaxLatitude = 85.0511287798066;
        private const double MinLatitude = -85.0511287798066;

        /// <summary>
        /// Converts WGS84 coordinates to Pseudo-Mercator (EPSG:3857)
        /// </summary>
        /// <param name="latitude">Latitude in degrees (-90 to 90)</param>
        /// <param name="longitude">Longitude in degrees (-180 to 180)</param>
        /// <returns>(x, y) coordinates in meters</returns>
        public static (double x, double y) WGS84ToPseudoMercator(double latitude, double longitude)
        {
            // Clamp latitude to valid range
            double lat = Math.Max(MinLatitude, Math.Min(MaxLatitude, latitude));

            // Convert to radians
            double latRad = DegreesToRadians(lat);
            double lonRad = DegreesToRadians(longitude);

            // Calculate Pseudo-Mercator coordinates
            double x = EarthRadius * lonRad;
            double y = EarthRadius * Math.Log(Math.Tan(Math.PI / 4 + latRad / 2));

            return (x, y);
        }

        /// <summary>
        /// Converts Pseudo-Mercator (EPSG:3857) coordinates back to WGS84
        /// </summary>
        /// <param name="x">X coordinate in meters (easting)</param>
        /// <param name="y">Y coordinate in meters (northing)</param>
        /// <returns>(latitude, longitude) in degrees</returns>
        public static (double latitude, double longitude) PseudoMercatorToWGS84(double x, double y)
        {
            // Convert to radians
            double lonRad = x / EarthRadius;

            // Calculate latitude using inverse formula
            double latRad = Math.PI / 2 - 2 * Math.Atan(Math.Exp(-y / EarthRadius));

            // Convert to degrees
            double longitude = RadiansToDegrees(lonRad);
            double latitude = RadiansToDegrees(latRad);

            // Normalize longitude to -180 to 180 range
            longitude = NormalizeLongitude(longitude);

            return (latitude, longitude);
        }



        /// <summary>
        /// Calculates the scale factor at a given latitude
        /// </summary>
        public static double GetScaleFactor(double latitude)
        {
            double latRad = DegreesToRadians(latitude);
            return 1 / Math.Cos(latRad);
        }

        /// <summary>
        /// Calculates the distance in meters for 1 degree of longitude at given latitude
        /// </summary>
        public static double MetersPerDegreeLongitude(double latitude)
        {
            double latRad = DegreesToRadians(latitude);
            return EarthRadius * Math.PI / 180 * Math.Cos(latRad);
        }

        /// <summary>
        /// Calculates the distance in meters for 1 degree of latitude (approximately constant)
        /// </summary>
        public static double MetersPerDegreeLatitude()
        {
            // In Pseudo-Mercator, this varies, but this is an approximation
            return EarthRadius * Math.PI / 180;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        private static double RadiansToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }

        private static double NormalizeLongitude(double longitude)
        {
            while (longitude < -180) longitude += 360;
            while (longitude > 180) longitude -= 360;
            return longitude;
        }
    }


    internal class geoTransformTool
    {
        public double OffsetX { get; set; } = 0.0;
        public double OffsetY { get; set; } = 0.0;

        public static geoTransformTool AskFromUser()
        {
            PromptKeywordOptions opts = new PromptKeywordOptions("");
            opts.Message = "\nПересчитывать ли координаты";
            opts.Keywords.Add("Да");
            opts.Keywords.Add("Нет");
            opts.AllowNone = false;

            PromptResult pKeyResult = dwgUtils.CurrentDoc.Editor.GetKeywords(opts);
            if (pKeyResult.Status == PromptStatus.OK && pKeyResult.StringResult == "Да") return new geoTransformTool(true);
            else return new geoTransformTool(false);
        }

        public geoTransformTool(bool useTransform)
        {
            pUseTransform = useTransform;

            Document doc = dwgUtils.CurrentDoc;
            Database db = doc.Database;

            bool isPropX = false;
            bool isPropY = false;

            DatabaseSummaryInfo dbInfo = db.SummaryInfo;
            var userProps = dbInfo.CustomProperties;
            while (userProps.MoveNext())
            {
                if (userProps.Key.ToString() == "OffsetX")
                {
                    if (userProps.Value != null) OffsetX = double.Parse(userProps.Value.ToString());
                    isPropX = true;
                }

                if (userProps.Key.ToString() == "OffsetY")
                {
                    if (userProps.Value != null) OffsetY = double.Parse(userProps.Value.ToString());
                    isPropY = true;
                }
            }

            if (!isPropX | !isPropY)
            {
                DatabaseSummaryInfoBuilder builder = new DatabaseSummaryInfoBuilder(dbInfo);
                if (!isPropX) builder.CustomPropertyTable.Add("OffsetX", OffsetX.ToString());
                if (!isPropY) builder.CustomPropertyTable.Add("OffsetY", OffsetX.ToString());
                db.SummaryInfo = builder.ToDatabaseSummaryInfo();
            }

        }

        public Point3d FromWgs84(Wgs84Point coords, double z = 0.0)
        {
            var result = PseudoMercatorConverter.WGS84ToPseudoMercator(coords.Latitude, coords.Longitude);
            if (pUseTransform) return new Point3d(result.x + OffsetX, result.y + OffsetY, z);
            else return new Point3d(coords.Latitude + OffsetX, coords.Longitude + OffsetY, z);
        }

        public Wgs84Point ToWgs84(double x, double y, double z)
        {
            var result = PseudoMercatorConverter.PseudoMercatorToWGS84(x, y);
            if (pUseTransform) return new Wgs84Point(result.latitude - OffsetX, result.longitude - OffsetY, z);
            else return new Wgs84Point(x - OffsetX, y - OffsetY, z);
        }

        private bool pUseTransform = false;
    }
}
