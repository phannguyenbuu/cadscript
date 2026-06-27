using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;


//using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class TextUnicodeConvertCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                //db.UpdateAllTextFont();
                ObjectIdCollection ids = ACD.GetSelection()._filterDXF("MTEXT", "TEXT", "MULTILEADER");
                //db._setCurrentStyle(IR.SelectedIds);

                foreach (ObjectId txtId in ids)
                {
                    //db._setLayer(txtId, DE.DEF_LAYER_TEXT);
                    string st = db._getContent(txtId);
                    st = st.UniConvert();

                    for (int i = 1; i < 10; i++)
                    {
                        string tmp = st.Upper();
                        if (tmp.Contains("Ị" + i.ToString()))
                            st = st.Replace("ị", "%%c");
                    }

                    db._setContent(txtId, st);
                    //db._setTextFactor(txtId, 1);
                    //db._setLeaderStandard(txtId);
                }

                ACD.Focus();
            }
        }
    }
}

