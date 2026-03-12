
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
using Teigha.Colors;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
#endif

using cadApiDevCourseNET.GeojsonSchema;


namespace cadApiDevCourseNET
{
    internal class dwgEntCreator
    {
        public dwgEntCreator(geoTransformTool transformInfo, BlockTableRecord targetBlock, Transaction tr)
        {
            this.mTrans = tr;
            this.mTransformInfo = transformInfo;
            this.mTargetBlock = targetBlock;

            this.mTargetLayerId = dwgUtils.CurrentDoc.Database.Clayer;
        }

        public void SetActiveLayer(string layerName)
        {
            Random r = new Random();
            Database cadDb = dwgUtils.CurrentDoc.Database;
            LayerTable cadLayerTable = this.mTrans.GetObject(cadDb.LayerTableId, OpenMode.ForRead) as LayerTable;
            if (layerName != "" && !cadLayerTable.Has(layerName))
            {
                LayerTableRecord newLayerDef = new LayerTableRecord();
                newLayerDef.Name = layerName;
                newLayerDef.Color = Color.FromColorIndex(ColorMethod.ByAci, (short)r.Next(0, 250));
                this.mTrans.GetObject(cadDb.LayerTableId, OpenMode.ForWrite);
                cadLayerTable.Add(newLayerDef);
                this.mTrans.AddNewlyCreatedDBObject(newLayerDef, true);

                this.mTargetLayerId = newLayerDef.Id;
            }
            else this.mTargetLayerId = cadLayerTable[layerName];

            //cadDb.Clayer = cadLayerTable[layerName];
        }
         
        public ObjectId CreatePoint(GJ_Position point)
        {
            DBPoint dwgPoint = new DBPoint(transformPoint(point));
            return addToDocument(dwgPoint);
        }

        public ObjectId CreateBlockReference(DBPoint basePoint, Dictionary<string, object> props, string gisLayername)
        {
            string blockName = "Block_" + gisLayername;
            Document doc = dwgUtils.CurrentDoc;
            Database cadDb = doc.Database;

            BlockTable blockTable = this.mTrans.GetObject(cadDb.BlockTableId,
                       OpenMode.ForRead) as BlockTable;

            ObjectId blocDefId = ObjectId.Null;
            if (blockTable.Has(blockName) == false)
            {
                using (BlockTableRecord blockDef = new BlockTableRecord())
                {
                    blockDef.Name = blockName;
                    blockDef.Origin = new Point3d(0, 0, 0);

                    Circle figure = new Circle();
                    figure.Center = new Point3d(0, 0, 0);
                    figure.Radius = 2;
                    blockDef.AppendEntity(figure);

                    this.mTrans.GetObject(cadDb.BlockTableId, OpenMode.ForWrite);
                    blockTable.Add(blockDef);
                    this.mTrans.AddNewlyCreatedDBObject(blockDef, true);

                    blocDefId = blockDef.Id;
                    figure.Dispose();
                }

                BlockTableRecord blockDef2 = this.mTrans.GetObject(blocDefId, OpenMode.ForWrite) as BlockTableRecord;

                int propCounter = 0;
                foreach (string propName in props.Keys)
                {
                    using (AttributeDefinition attDef = new AttributeDefinition())
                    {
                        attDef.Invisible = true;
                        attDef.Position = new Point3d(0, propCounter * 3, 0);
                        attDef.Prompt = propName;
                        attDef.Tag = propName;
                        attDef.TextString = "";
                        attDef.Height = 1;
                        attDef.Justify = AttachmentPoint.MiddleCenter;

                        blockDef2.AppendEntity(attDef);
                        propCounter++;
                    }
                }
            }
            else blocDefId = blockTable[blockName];

            if (blocDefId != ObjectId.Null)
            {
                //
                BlockTableRecord blockDef2 = this.mTrans.GetObject(blocDefId,
                    OpenMode.ForRead) as BlockTableRecord;

                using (BlockReference blockRef = new BlockReference(basePoint.Position, blocDefId))
                {
                    blockRef.LayerId = basePoint.LayerId;
                    this.mTargetBlock.AppendEntity(blockRef);
                    this.mTrans.AddNewlyCreatedDBObject(blockRef, true);

                    if (blockDef2.HasAttributeDefinitions)
                    {
                        foreach (ObjectId objId in blockDef2)
                        {
                            DBObject blockDefObject = this.mTrans.GetObject(objId, OpenMode.ForRead) as DBObject;
                            if (blockDefObject is AttributeDefinition)
                            {
                                AttributeDefinition blockDefObjectAD = blockDefObject as AttributeDefinition;
                                if (!blockDefObjectAD.Constant)
                                {
                                    using (AttributeReference attrRef = new AttributeReference())
                                    {
                                        attrRef.SetAttributeFromBlock(blockDefObjectAD, blockRef.BlockTransform);
                                        attrRef.Position = blockDefObjectAD.Position.TransformBy(blockRef.BlockTransform);

                                        if (props.ContainsKey(blockDefObjectAD.Tag))
                                        {
                                            var propValue = props[blockDefObjectAD.Tag];
                                            if (propValue != null) attrRef.TextString = propValue.ToString();
                                        }

                                        blockRef.AttributeCollection.AppendAttribute(attrRef);
                                        this.mTrans.AddNewlyCreatedDBObject(attrRef, true);
                                    }
                                }
                            }
                        }
                    }

                    return blockRef.Id;
                }
            }
            return ObjectId.Null;
        }

        public ObjectId CreateLineObject(List<GJ_Position> points)
        {
            Entity dwgLinearEntity;
            if (points.Count < 1) return ObjectId.Null;
            if (points.Count == 2)
            {
                dwgLinearEntity = new Line(transformPoint(points[0]), transformPoint(points[1]));
            }
            else
            {
                var zValues = points.Select(point => point.Altitude).Distinct();
                if (zValues.Count() > 1)
                {
                    dwgLinearEntity = new Polyline3d();
                    for (int vertexIndex = 0; vertexIndex< points.Count; vertexIndex++)
                    {
                        var vertex = transformPoint(points[vertexIndex]);
                        ((Polyline3d)dwgLinearEntity).AppendVertex(new PolylineVertex3d(vertex));
                    }
                }
                else
                {
                    dwgLinearEntity = new Polyline(points.Count);
                    for (int vertexIndex = 0; vertexIndex < points.Count; vertexIndex++)
                    {
                        var vertex = transformPoint(points[vertexIndex]);
                        ((Polyline)dwgLinearEntity).AddVertexAt(vertexIndex, new Point2d(vertex.X, vertex.Y), 0.0, 0.0, 0.0);
                    }
                    
                    ((Polyline)dwgLinearEntity).Elevation = zValues?.First() ?? 0.0;
                }
            }
            return addToDocument(dwgLinearEntity);
        }

        public ObjectId CreateHatch(List<List<GJ_Position>> controus)
        {
            Hatch dwgHatch = new Hatch();
            dwgHatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            dwgHatch.Transparency = new Transparency(20);

            for (int plgContourIndex = 0; plgContourIndex < controus.Count; plgContourIndex++)
            {
                List<GJ_Position> contourPoints = controus[plgContourIndex];

                Point2dCollection contourVertices = new Point2dCollection();
                DoubleCollection contourBulges = new DoubleCollection();

                for (int vertexIndex = 0; vertexIndex < contourPoints.Count; vertexIndex++)
                {
                    var vertex = transformPoint(contourPoints[vertexIndex]);

                    contourVertices.Add(new Point2d(vertex.X, vertex.Y));
                    contourBulges.Add(0.0);
                }

                HatchLoopTypes types;
                if (plgContourIndex == 0) types = HatchLoopTypes.External | HatchLoopTypes.Polyline;
                else types = HatchLoopTypes.Polyline;

                dwgHatch.AppendLoop(types, contourVertices, contourBulges);

            }
            dwgHatch.EvaluateHatch(true);
            return addToDocument(dwgHatch);

        }


        private ObjectId addToDocument(Entity dwgEnt)
        {
            dwgEnt.LayerId = this.mTargetLayerId;
            this.mTargetBlock.AppendEntity(dwgEnt);
            this.mTrans.AddNewlyCreatedDBObject(dwgEnt, true);

            return dwgEnt.Id;
        }

        private Point3d transformPoint(GJ_Position pos)
        {
            return mTransformInfo.FromWgs84(new Wgs84Point(pos.Latitude, pos.Longitude, pos.Altitude ?? 0.0));
        }


        private ObjectId mTargetLayerId;
        private Transaction mTrans;
        private BlockTableRecord mTargetBlock;
        private geoTransformTool mTransformInfo;
    }
}
