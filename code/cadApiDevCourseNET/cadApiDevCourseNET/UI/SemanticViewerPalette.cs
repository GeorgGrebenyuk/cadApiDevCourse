
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Integration;

#if NCAD
using HostMgd.Windows;
#else
using Autodesk.AutoCAD.Windows;
#endif

namespace cadApiDevCourseNET.UI
{
    internal class SemanticViewerPalette
    {
        private static PaletteSet? mPaletteSet;
        private static Guid paletteId = Guid.NewGuid(); // Guid.Parse("{efbb6807-d965-4dfa-a36e-a4a4669066f2}");

        public static void CreatePalette()
        {
            if (mPaletteSet == null)
            {
                mPaletteSet = new PaletteSet("SemanticViweroalette", paletteId);
                mPaletteSet.Size = new System.Drawing.Size(200, 300);

                var hostView = new ElementHost
                {
                    AutoSize = false,
                    Dock = System.Windows.Forms.DockStyle.Fill,
                    Child = new SemanticViewer()
                };


                mPaletteSet.Add("ExtendedProps", hostView);
            }
            mPaletteSet.Visible = true;
        }
    }
}
