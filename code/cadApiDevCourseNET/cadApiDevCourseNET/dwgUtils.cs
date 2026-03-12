#if NCAD
using Teigha.Runtime;
using HostMgd.ApplicationServices;
using HostMgd.Windows;
#else
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.Interop;
#endif

namespace cadApiDevCourseNET
{
    internal class dwgUtils
    {
        public static void WriteToCmd(string text)
        {
            Document doc = dwgUtils.CurrentDoc;
            string suffix = "";
#if ACAD
            suffix = "\n";
#endif
            doc.Editor.WriteMessage(text + suffix);

        }

        public static Document CurrentDoc
        {
            get
            {
                return Application.DocumentManager.MdiActiveDocument;
            }
        }

        public static void ZoomToObjects()
        {
            CurrentDoc.Editor.UpdateScreen();
            CurrentDoc.Editor.Regen();

            string zoomCommand = "_ZOOM Г\n";
#if NCAD
            nanoCAD.Application app = Application.AcadApplication as nanoCAD.Application;
            app.ActiveDocument.SendCommand(zoomCommand);
#else
            AcadApplication app = Application.AcadApplication as AcadApplication;
            app.ActiveDocument.SendCommand(zoomCommand);
#endif
        }
    }
}
