using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NCAD
using Teigha.DatabaseServices;
#else

using Autodesk.AutoCAD.DatabaseServices;
#endif

namespace cadApiDevCourseNET
{
    public class PropertyDef
    {
        public int PropDefId;
        public string Caption;
        public string Category;

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            PropertyDef? objOther = obj as PropertyDef;
            if (objOther == null) return false;

            return objOther.Caption == Caption && objOther.Category == Category;
        }
    }

    public class PropertyValue
    {
        public int PropDefId;
        public string ValueStr;
    }

    internal class SemanticManager
    {
        public const string DICT_SEMANTIC = "DICT_SEMANTIC";
        public const string APP_SEMANTIC = "APP_SEMANTIC";

        private SemanticManager()
        {
            initData();
        }

        public static SemanticManager CreateInstance()
        {
            if (mInstance == null) mInstance = new SemanticManager();
            return mInstance;
        }

        private void initData()
        {
            Database cadDb = dwgUtils.CurrentDoc.Database;
            using (Transaction cadTrans = cadDb.TransactionManager.StartTransaction())
            {
                RegAppTable regTable = cadTrans.GetObject(cadDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;

                if (regTable.Has(APP_SEMANTIC) == false)
                {
                    using (RegAppTableRecord regRecord = new RegAppTableRecord())
                    {
                        regRecord.Name = APP_SEMANTIC;
                        cadTrans.GetObject(cadDb.RegAppTableId, OpenMode.ForWrite);
                        regTable.Add(regRecord);
                        cadTrans.AddNewlyCreatedDBObject(regRecord, true);

                    }
                    cadTrans.Commit();
                }
            }
        }

        public void SavePropDefs(PropertyDef[] propertyDefs)
        {
            initData();

            Database cadDb = dwgUtils.CurrentDoc.Database;
            using (Transaction cadTrans = cadDb.TransactionManager.StartTransaction())
            {
                DBDictionary dbDict = cadTrans.GetObject(cadDb.NamedObjectsDictionaryId,
                    OpenMode.ForWrite) as DBDictionary;
                if (dbDict == null)
                {
                    dwgUtils.WriteToCmd("Не удалось получить словарь БД");
                    return;
                }

                ObjectId semanticDictId;
                if (!dbDict.Contains(DICT_SEMANTIC))
                {
                    DBDictionary semanticDict0 = new DBDictionary();
                    dbDict.SetAt(DICT_SEMANTIC, semanticDict0);
                    cadTrans.AddNewlyCreatedDBObject(semanticDict0, true);
                    semanticDictId = semanticDict0.Id;
                }
                else
                {
                    semanticDictId = dbDict.GetAt(DICT_SEMANTIC);
                }

                DBDictionary semanticDict = cadTrans.GetObject(semanticDictId,
                    OpenMode.ForWrite) as DBDictionary;

                var existedProps = GetPropertyDefinitions();
                int propCounter = existedProps.Count() + 1;
                
                foreach (PropertyDef newPropDef in propertyDefs)
                {
                    if (existedProps.Contains(newPropDef)) continue;

                    Xrecord propDefRecord = new Xrecord();
                    using (ResultBuffer propDefRecordRB = new ResultBuffer())
                    {
                        propDefRecordRB.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, APP_SEMANTIC));
                        propDefRecordRB.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, newPropDef.PropDefId));
                        propDefRecordRB.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, newPropDef.Caption));
                        propDefRecordRB.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, newPropDef.Category));
                        propDefRecord.XData = propDefRecordRB;
                    }

                    semanticDict.SetAt($"Property_{propCounter}", propDefRecord);
                    propCounter++;
                }
                cadTrans.Commit();
            }
        }

        public PropertyDef[] GetPropertyDefinitions()
        {
            PropertyDef[] result = new PropertyDef[] { };

            Database cadDb = dwgUtils.CurrentDoc.Database;
            using (Transaction cadTrans = cadDb.TransactionManager.StartTransaction())
            {
                DBDictionary dbDict = cadTrans.GetObject(cadDb.NamedObjectsDictionaryId,
                    OpenMode.ForWrite) as DBDictionary;
                if (dbDict == null) return result;

                ObjectId semanticDictId;
                if (!dbDict.Contains(DICT_SEMANTIC)) return result;
                else
                {
                    semanticDictId = dbDict.GetAt(DICT_SEMANTIC);
                }

                DBDictionary semanticDict = cadTrans.GetObject(semanticDictId,
                    OpenMode.ForWrite) as DBDictionary;

                List<PropertyDef> tmpPropDefs = new List<PropertyDef>();
                
                foreach (var propDefRaw in semanticDict)
                {
                    Xrecord? propDef = cadTrans.GetObject(propDefRaw.Value, OpenMode.ForRead) as Xrecord;
                    if (propDef == null) continue;

                    ResultBuffer propDefData = propDef.XData;
                    if (propDefData == null) continue;

                    PropertyDef propDef2 = new PropertyDef();

                    var propDefDataArray = propDefData.AsArray();
                    propDef2.PropDefId = (int)propDefDataArray[1].Value;
                    propDef2.Caption = (string)propDefDataArray[2].Value;
                    propDef2.Category = (string)propDefDataArray[3].Value;
                    tmpPropDefs.Add(propDef2);

                }

                result = tmpPropDefs.ToArray();
            }
            return result;
        }

        public void SaveObjectsProperties(DBObject dwgEnt, Transaction cadTrans, PropertyValue[] properties)
        {
            initData();
            if (dwgEnt.ExtensionDictionary == ObjectId.Null) dwgEnt.CreateExtensionDictionary();
            DBDictionary? entDict = cadTrans.GetObject(dwgEnt.ExtensionDictionary, OpenMode.ForWrite) as DBDictionary;
            if (entDict == null)
            {
                dwgUtils.WriteToCmd("Не получилось получить словарь объекта");
                return;
            }

            ObjectId propDictId;
            if (entDict.Contains(DICT_SEMANTIC))
            {
                entDict.Remove(DICT_SEMANTIC);
            }

            DBDictionary propsDict0 = new DBDictionary();
            entDict.SetAt(DICT_SEMANTIC, propsDict0);
            cadTrans.AddNewlyCreatedDBObject(propsDict0, true);
            propDictId = propsDict0.Id;

            DBDictionary propsDict = cadTrans.GetObject(propDictId, OpenMode.ForWrite) as DBDictionary;

            int counter = 0;
            foreach (PropertyValue propValue in properties)
            {
                string propValueKey = $"Property_{counter}";
                ObjectId xRecordId;
                if (propsDict.Contains(propValueKey))
                {
                    xRecordId = propsDict.GetAt(propValueKey);
                }
                else
                {
                    Xrecord rec = new Xrecord();
                    propsDict.SetAt(propValueKey, rec);
                    cadTrans.AddNewlyCreatedDBObject(rec, true);
                    xRecordId = rec.Id;
                }

                Xrecord propValueRecord = cadTrans.GetObject(xRecordId, OpenMode.ForWrite) as Xrecord;
                if (propValueRecord == null) continue;

                using (ResultBuffer propValueRecordRB = new ResultBuffer())
                {
                    propValueRecordRB.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, APP_SEMANTIC));
                    propValueRecordRB.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, propValue.PropDefId));
                    propValueRecordRB.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, propValue.ValueStr));

                    propValueRecord.XData = propValueRecordRB;
                }

                counter++;
            }
        }

        public PropertyValue[] GetObjectsProperties(ObjectId dwgEntId)
        {
            PropertyValue[] result = new PropertyValue[] { };
            Database cadDb = dwgUtils.CurrentDoc.Database;

            using (Transaction cadTrans = cadDb.TransactionManager.StartTransaction())
            {
                DBObject dwgEnt = cadTrans.GetObject(dwgEntId, OpenMode.ForRead);

                if (dwgEnt.ExtensionDictionary == ObjectId.Null) return result;

                DBDictionary? entDict = cadTrans.GetObject(dwgEnt.ExtensionDictionary, OpenMode.ForWrite) as DBDictionary;
                if (entDict == null) return result;

                ObjectId propDictId;
                if (!entDict.Contains(DICT_SEMANTIC)) return result;

                DBDictionary propsDict = cadTrans.GetObject(entDict.GetAt(DICT_SEMANTIC), OpenMode.ForWrite) as DBDictionary;

                List<PropertyValue> tmpValues = new List<PropertyValue>();

                foreach (var propValEntry in propsDict)
                {
                    Xrecord propValueRecord = cadTrans.GetObject(propValEntry.Value, OpenMode.ForRead) as Xrecord;
                    if (propValueRecord == null) continue;

                    ResultBuffer propDefData = propValueRecord.GetXDataForApplication(APP_SEMANTIC);

                    PropertyValue tmpPropValue = new PropertyValue();
                    tmpPropValue.PropDefId = (int)propDefData.AsArray()[1].Value;
                    tmpPropValue.ValueStr = (string)propDefData.AsArray()[2].Value;

                    tmpValues.Add(tmpPropValue);

                }

                result = tmpValues.ToArray();

            }
            return result;
        }

        private static SemanticManager mInstance;
    }
}
