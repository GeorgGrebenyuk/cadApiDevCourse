
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NCAD
using Teigha.DatabaseServices;
#else

using Autodesk.AutoCAD.DatabaseServices;
#endif

using cadApiDevCourseNET.GeojsonSchema;

namespace cadApiDevCourseNET
{
    internal class GeoJsonImporter
    {
        public GeoJsonImporter()
        {
            mTransfromInfo = geoTransformTool.AskFromUser();
        }

        public void Start(string[] paths)
        {
            Database cadDb = dwgUtils.CurrentDoc.Database;

            cadDb.Pdmode = 34;
            cadDb.Pdsize = 1;

            var semanticManager = SemanticManager.CreateInstance();

            foreach (string gjPath in paths)
            {
                string jsonRaw = File.ReadAllText(gjPath);
                FeatureCollection? geojsonData = GeoJsonSerializer.Deserialize(jsonRaw) as FeatureCollection;
                if (geojsonData == null)
                {
                    dwgUtils.WriteToCmd($"Файл {gjPath} не удалось прочитать ");
                    continue;

                    
                }

                var existedProperties = semanticManager.GetPropertyDefinitions();
                List<PropertyDef> tmpExistedProperties = existedProperties.ToList();



                using (Transaction cadTrans = cadDb.TransactionManager.StartTransaction())
                {
                    BlockTable blockTable = cadTrans.GetObject(cadDb.BlockTableId,
                        OpenMode.ForRead) as BlockTable;
                    BlockTableRecord modelSpace = cadTrans.GetObject(blockTable[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite) as BlockTableRecord;

                    mDwgEntCreator = new dwgEntCreator(mTransfromInfo, modelSpace, cadTrans);
                    mDwgEntCreator.SetActiveLayer(geojsonData.Name);

                    foreach (GJ_Feature feature in geojsonData.Features)
                    {
                        ObjectIdCollection importedObjects = importObject(feature.Geometry);

                        List<PropertyValue> propValues = new List<PropertyValue>();
                        foreach (var featurePropDef in feature.Properties)
                        {
                            PropertyDef propDef = new PropertyDef()
                            {
                                PropDefId = tmpExistedProperties.Count() + 1,
                                Caption = featurePropDef.Key,
                                Category = geojsonData.Name
                            };

                            if (!tmpExistedProperties.Contains(propDef)) tmpExistedProperties.Add(propDef);
                            else propDef = tmpExistedProperties.Where(p => p.Caption == featurePropDef.Key 
                                && p.Category == geojsonData.Name).First();

                            PropertyValue propValue = new PropertyValue()
                            {
                                PropDefId = propDef.PropDefId,
                                ValueStr = featurePropDef.Value?.ToString() ?? ""
                            };
                            propValues.Add(propValue);
                        }


                        foreach (ObjectId objId in importedObjects)
                        {
                            DBObject objEntity = cadTrans.GetObject(objId, OpenMode.ForWrite);

                            semanticManager.SaveObjectsProperties(objEntity, cadTrans, propValues.ToArray());


                            DBPoint? objAsPoint = cadTrans.GetObject(objId, OpenMode.ForWrite) as DBPoint;
                            if (objAsPoint == null) continue;

                            mDwgEntCreator.CreateBlockReference(objAsPoint, feature.Properties, geojsonData.Name);
                            objAsPoint.Erase();
                        }

                    }

                    semanticManager.SavePropDefs(tmpExistedProperties.ToArray());

                    cadTrans.Commit();
                }
            }
        }

        private ObjectIdCollection importObject(GJ_Geometry geometry)
        {
            ObjectIdCollection collection = new ObjectIdCollection();
            switch(geometry.Type)
            {
                case "Point":
                    GJ_Point? gjGeometryPoint = geometry as GJ_Point;
                    if (gjGeometryPoint != null) collection.Add(mDwgEntCreator.CreatePoint(gjGeometryPoint.Coordinates));
                    break;
                case "MultiPoint":
                    GJ_MultiPoint? gjGeometryMPoint = geometry as GJ_MultiPoint;
                    if (gjGeometryMPoint != null)
                    {
                        foreach (var gjPos in gjGeometryMPoint.Coordinates)
                        {
                            collection.Add(mDwgEntCreator.CreatePoint(gjPos));
                        }
                    }
                    break;
                case "LineString":
                    GJ_LineString? gjHeometryAsLS = geometry as GJ_LineString;
                    if (gjHeometryAsLS != null)
                    {
                        collection.Add(mDwgEntCreator.CreateLineObject(gjHeometryAsLS.Coordinates));
                    }
                    break;
                case "MultiLineString":
                    GJ_MultiLineString? gjGeometryMLineString = geometry as GJ_MultiLineString;
                    if (gjGeometryMLineString != null)
                    {
                        foreach (var gjLineS in gjGeometryMLineString.Coordinates)
                        {
                            collection.Add(mDwgEntCreator.CreateLineObject(gjLineS));
                        }
                    }
                    break;
                case "Polygon":
                    GJ_Polygon? geometryAsPlg = geometry as GJ_Polygon;
                    if (geometryAsPlg != null)
                    {
                        collection.Add(mDwgEntCreator.CreateHatch(geometryAsPlg.Coordinates));
                    }
                    break;
                case "MultiPolygon":
                    GJ_MultiPolygon? gjGeometryMPolygon = geometry as GJ_MultiPolygon;
                    if (gjGeometryMPolygon != null)
                    {
                        foreach (var gjPlg in gjGeometryMPolygon.Coordinates)
                        {
                            collection.Add(mDwgEntCreator.CreateHatch(gjPlg));
                        }
                    }
                    break;
                case "GeometryCollection":
                    GJ_GeometryCollection? geometryAsCollection = geometry as GJ_GeometryCollection;
                    if (geometryAsCollection != null)
                    {
                        foreach (var gjGeomPart in geometryAsCollection.Geometries)
                        {
                            ObjectIdCollection coll = importObject(gjGeomPart);
                            foreach (ObjectId subId in coll)
                            {
                                collection.Add(subId);
                            }
                        }
                    }
                    break;

            }
            return collection;
        }

        private dwgEntCreator mDwgEntCreator;
        private geoTransformTool mTransfromInfo;
    }
}
