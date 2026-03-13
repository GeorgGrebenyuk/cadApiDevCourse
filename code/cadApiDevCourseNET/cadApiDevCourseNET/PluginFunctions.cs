using System.IO;
using cadApiDevCourseNET.UI;
#if NCAD
using Teigha.Runtime;
using HostMgd.ApplicationServices;
using HostMgd.Windows;
#else
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
#endif

namespace cadApiDevCourseNET
{
    public class PluginFunctions
    {
        [CommandMethod("command_Test1")]
        public void command_Test1()
        {
            Document cadDoc = Application.DocumentManager.MdiActiveDocument;
            if (cadDoc == null) return;

            string suffix = "";
#if ACAD
            suffix = "\n";
#endif
            cadDoc.Editor.WriteMessage("Привет, CAD 2" + suffix);
        }

        [CommandMethod("CadDevCourse_GeojsonImport")]
        public void command_GeojsonImport()
        {
            OpenFileDialog dialog = new OpenFileDialog("Выбор Geojson-файлов", "", "geojson", "",
                OpenFileDialog.OpenFileDialogFlags.AllowMultiple);
            string[] inputFiles;

#if D_NC26 || D_AC2022

            inputFiles = Directory.GetFiles(@"C:\Users\Georg\Documents\GitHub\cadApiDevCourse\samples\osm",
                "*.geojson", SearchOption.TopDirectoryOnly);
#else
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                dwgUtils.WriteToCmd("Выбор файлов был сброшен. ... ");
                return;
            }
            inputFiles = dialog.GetFilenames();

#endif

            GeoJsonImporter importer = new GeoJsonImporter();
            importer.Start(inputFiles);

            dwgUtils.ZoomToObjects();

        }

        [CommandMethod("CadDevCourse_ShowSemantic", CommandFlags.UsePickSet)]
        public void command_ShowSemantic()
        {
            SemanticViewerPalette.CreatePalette();
        }
    }
}
