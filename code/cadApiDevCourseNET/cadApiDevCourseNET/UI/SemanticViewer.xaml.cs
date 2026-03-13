using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

#if NCAD
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;
using HostMgd.EditorInput;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
#endif

namespace cadApiDevCourseNET.UI
{
    class PropertyToShow
    {
        public string Caption { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return $"Prop: {Caption}; Value: {Value}";
        }
    }
    /// <summary>
    /// Interaction logic for SemanticViewer.xaml
    /// </summary>
    public partial class SemanticViewer : UserControl
    {
        public SemanticViewer()
        {
            InitializeComponent();
            mPropDefs = new PropertyDef[] { };

            DocumentCollection dm = Application.DocumentManager;
            dm.MdiActiveDocument.ImpliedSelectionChanged += MdiActiveDocument_ImpliedSelectionChanged;
            dm.DocumentBecameCurrent += Dm_DocumentBecameCurrent;

        }

        private void Dm_DocumentBecameCurrent(object sender, DocumentCollectionEventArgs e)
        {
            mPropDefs = new PropertyDef[] { };
        }

        private void MdiActiveDocument_ImpliedSelectionChanged(object sender, EventArgs e)
        {
            this.ListView_Info.Items.Clear();

            Editor ed = dwgUtils.CurrentDoc.Editor;

            SelectionSet sset = ed.SelectImplied().Value;
            if (sset.Count > 1 | sset.Count == 0) return;
            ObjectId selectedEntId = sset.GetObjectIds().First();

            var semanticManager = SemanticManager.CreateInstance();
            var entProps = semanticManager.GetObjectsProperties(selectedEntId);
            if (!entProps.Any()) return;

            if (mPropDefs == null || !mPropDefs.Any()) mPropDefs = semanticManager.GetPropertyDefinitions();
            foreach ( var entProp in entProps )
            {
                var propDefs = mPropDefs.Where(p=>p.PropDefId == entProp.PropDefId);
                if (!propDefs.Any())
                {
                    mPropDefs = semanticManager.GetPropertyDefinitions();
                }
                propDefs = mPropDefs.Where(p => p.PropDefId == entProp.PropDefId);
                if (!propDefs.Any()) continue;

                PropertyToShow prop = new PropertyToShow()
                {
                    Caption = propDefs.First().Caption,
                    Value = entProp.ValueStr
                };
                this.ListView_Info.Items.Add(prop);
                ed.WriteMessage(prop.ToString() + "\n");

            }

        }

        private PropertyDef[] mPropDefs;
    }


}
