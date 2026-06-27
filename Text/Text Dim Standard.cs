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


using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class TextSizeCLS
    {
        static void _setTextSize(string key)
        {
            bool control_mode = ACD.ControlHold;
            ObjectIdCollection selIds = ACD.GetSelection();
            double sz = pString.INI_Value(key);

            foreach (ObjectId txtId in selIds)
                if (ACD.DB._isText(txtId))
                {
                    ACD.DB._setTextSize(txtId, sz);
                }
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectId dim_style = ACD.DB.Dimstyle;
                ObjectId text_style = ACD.DB.Textstyle;
                
                using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
                {
                    ACD.DB.GetEntities(ACD.bRect, EN_SELECT.AC_DXF, "DIMENSION");
                    ACD.DB._setLayer(IR.SelectedIds, "G-Dim");
                    foreach (ObjectId id in IR.SelectedIds)
                    {
                        Dimension dim = (Dimension)tr.GetObject(id, OpenMode.ForWrite);
                        dim.DimensionStyle = dim_style;
                    }

                    ACD.DB.GetEntities(ACD.bRect, EN_SELECT.AC_DXF, "TEXT", "MTEXT");
                    ACD.DB._setLayer(IR.SelectedIds, "G-Text");
                    foreach (ObjectId id in IR.SelectedIds)
                    {
                        if (id.ObjectClass.DxfName == "TEXT")
                        {
                            DBText txt = (DBText)tr.GetObject(id, OpenMode.ForWrite);
                            txt.TextStyleId = text_style;
                        }else
                        {
                            MText txt = (MText)tr.GetObject(id, OpenMode.ForWrite);
                            txt.TextStyleId = text_style;
                        }
                    }

                    tr.Commit();
                }

                ACD.Focus();
            }
        }
    }
}

