#if NCAD
using Teigha.Runtime;
#else
using Autodesk.AutoCAD.Runtime;
#endif

namespace cadApiDevCourseNET
{


    public class PluginLoader : IExtensionApplication
    {

        public void Initialize()
        {

        }

        public void Terminate()
        {

        }
    }
}