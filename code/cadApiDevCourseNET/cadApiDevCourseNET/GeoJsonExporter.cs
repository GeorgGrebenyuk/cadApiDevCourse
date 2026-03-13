using cadApiDevCourseNET.GeojsonSchema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


#if NCAD
using Teigha.Runtime;
using HostMgd.ApplicationServices;
using HostMgd.Windows;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Geometry;
#else
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
#endif
namespace cadApiDevCourseNET
{
    internal class GeoJsonExporter
    {
        public const string LAYER_LinearObjects = "LAYER_LinearObjects";
        public const string LAYER_PolygonObjects = "LAYER_PolygonObjects";


        public GeoJsonExporter()
        {
            mTransformInfo = geoTransformTool.AskFromUser();
        }

        public void ExportEntities(ObjectIdCollection entities, string saveDirectory)
        {
            Database cadDb = dwgUtils.CurrentDoc.Database;
            Dictionary<string, FeatureCollection> tmpGeojsonData = new Dictionary<string, FeatureCollection>();

            using (Transaction cadTrans = cadDb.TransactionManager.StartTransaction())
            {
                foreach (ObjectId entId in entities)
                {
                    Entity? cadEnt = cadTrans.GetObject(entId, OpenMode.ForRead) as Entity;
                    if (cadEnt == null) continue;

                    Dictionary<string, object> entProperties = new Dictionary<string, object>()
                    {
                        {"Layer", cadEnt.Layer },
                        {"Handle", cadEnt.Handle.Value.ToString() },
                    };

                    Curve? entAsCurve = cadEnt as Curve;
                    if (entAsCurve != null)
                    {
                        if (!tmpGeojsonData.ContainsKey(LAYER_LinearObjects)) tmpGeojsonData.Add(LAYER_LinearObjects, new FeatureCollection() { Name = LAYER_LinearObjects });

                        GJ_Geometry? geometry = null;
                        Line? entAsLine = cadEnt as Line;
                        if (entAsLine != null) geometry = getFromLine(entAsLine);

                        Polyline? entAsPline = cadEnt as Polyline;
                        if (entAsPline != null) geometry = getFromPolyline(entAsPline);

                        Polyline3d? entAsPline3d = cadEnt as Polyline3d;
                        if (entAsPline3d != null) geometry = getFromPolyline3d(entAsPline3d, cadTrans);

                        entProperties.Add("Area", entAsCurve.Area);

                        if (geometry == null) continue;
                        tmpGeojsonData[LAYER_LinearObjects].Features.Add(new GJ_Feature() { Geometry = geometry, Properties = entProperties });
                    }

                    Hatch? entAsHatch = cadEnt as Hatch;
                    if ( entAsHatch != null)
                    {
                        if (!tmpGeojsonData.ContainsKey(LAYER_PolygonObjects)) tmpGeojsonData.Add(LAYER_PolygonObjects, new FeatureCollection() { Name = LAYER_PolygonObjects });

                        GJ_MultiPolygon geometry = getfromHatch(entAsHatch);

                        entProperties.Add("Area", entAsHatch.Area);
                        entProperties.Add("Pattern", entAsHatch.PatternName);

                        tmpGeojsonData[LAYER_PolygonObjects].Features.Add(new GJ_Feature() { Geometry = geometry, Properties = entProperties });

                    }

                    BlockReference? entAsBlockRef = cadEnt as BlockReference;
                    if (entAsBlockRef != null)
                    {
                        if (!tmpGeojsonData.ContainsKey(entAsBlockRef.Name)) tmpGeojsonData.Add(entAsBlockRef.Name, new FeatureCollection() { Name = entAsBlockRef.Name });

                        GJ_Point geometry = new GJ_Point();
                        geometry.Coordinates = transformPoint(entAsBlockRef.Position);

                        //read attributes
                        AttributeCollection attrColl = entAsBlockRef.AttributeCollection;

                        foreach (ObjectId attrId in attrColl)
                        {
                            AttributeReference attrRef = cadTrans.GetObject(attrId, OpenMode.ForRead) as AttributeReference;
                            if (attrRef == null) continue;

                            entProperties[attrRef.Tag] = attrRef.TextString;
                        }
                        tmpGeojsonData[entAsBlockRef.Name].Features.Add(new GJ_Feature() { Geometry = geometry, Properties = entProperties });
                    }

                }
            }

            foreach (var geojsonData in tmpGeojsonData)
            {
                if (!geojsonData.Value.Features.Any()) continue;

                string savePath = Path.Combine(saveDirectory, geojsonData.Key + ".geojson");
                File.WriteAllText(savePath, GeoJsonSerializer.Serialize(geojsonData.Value, true));
            }
        }

        private GJ_LineString getFromLine(Line dwgLine)
        {
            GJ_LineString result = new GJ_LineString();
            result.Coordinates.Add(transformPoint(dwgLine.StartPoint));
            result.Coordinates.Add(transformPoint(dwgLine.EndPoint));
            return result;
        }

        private GJ_LineString getFromPolyline(Polyline polyline)
        {
            GJ_LineString result = new GJ_LineString();
            for (int vertexIndex = 0; vertexIndex < polyline.NumberOfVertices; vertexIndex++)
            {
                var vertex = transformPoint(polyline.GetPoint3dAt(vertexIndex));
                result.Coordinates.Add(vertex);
            }
            return result;
        }

        private GJ_LineString getFromPolyline3d(Polyline3d pline3d, Transaction tr)
        {
            GJ_LineString result = new GJ_LineString();

            foreach (ObjectId vertexId in pline3d)
            {
                var vertex = (PolylineVertex3d)tr.GetObject(vertexId, OpenMode.ForRead);
                result.Coordinates.Add(transformPoint(vertex.Position));
            }
            return result;
        }

        private GJ_MultiPolygon getfromHatch(Hatch hatch)
        {
            GJ_MultiPolygon geometry = new GJ_MultiPolygon();
            for (int loopIndex = 0; loopIndex < hatch.NumberOfLoops; loopIndex++)
            {
                HatchLoop loop = hatch.GetLoopAt(loopIndex);
                if (loop.LoopType.HasFlag(HatchLoopTypes.External))
                {
                    List<GJ_Position> border = new List<GJ_Position>();
                    foreach (BulgeVertex vertex in loop.Polyline)
                    {
                        border.Add(transformPoint(vertex.Vertex));
                    }
                    geometry.Coordinates.Add(new List<List<GJ_Position>>() { border });
                }
            }

            return geometry;
        }




        private GJ_Position transformPoint(Point3d pos)
        {
            var result = mTransformInfo.ToWgs84(pos.X, pos.Y, pos.Z);
            return new GJ_Position(result.Longitude, result.Latitude, result.Altitude);
        }

        private GJ_Position transformPoint(Point2d pos)
        {
            var result = mTransformInfo.ToWgs84(pos.X, pos.Y, 0.0);
            return new GJ_Position(result.Longitude, result.Latitude, 0.0);
        }

        private geoTransformTool mTransformInfo;
    }
}
