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
    public class PolylineFixAreaCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    string st = ACD.ED.GetInputString("Enter new area #Number:", "0");
                    double val = st.filter("#")[0].ToNumber();
                    
                    if (val > 0)
                    {
                        double src_area = 0;

                        foreach(ObjectId id in selIds)
                            if(ACD.DB._isPolyline(id))
                            {
                                src_area = ACD.DB._getVertices(id).Area();
                            }

                        if (src_area != 0)
                        {
                            ACD.DB.CloneObjects(selIds);
                            ACD.DB.Transforms(selIds, new pPos(0, 0), 0,
                                Math.Sqrt(val / src_area), ACD.DB._getBound(selIds).CenterPoint());

                            var bb = ACD.DB._getBound(selIds);
                            var sz = bb.Size();
                            var h = (Math.Min(sz.X, sz.Y)/5).roundNumber();

                            if(st.ct_("#"))
                                ACD.DB.CreateText(st.filter("#")[1].ToString(), bb.CenterPoint() + new pPos(0, h), h);

                            ACD.DB.CreateText(val + " m2", bb.CenterPoint() - new pPos(0, h), h);
                        }
                    }
                }

                ACD.Focus();
            }
        }
    }
}

