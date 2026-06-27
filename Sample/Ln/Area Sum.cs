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
    public class PolylineAreaSumCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {

                double total = 0;
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.ToList().All(id => ACD.DB._isText(id) || ACD.DB._isLeader(id)))
                {
                    foreach (ObjectId txtId in selIds)
                        {
                            string st = ACD.DB._getContent(txtId);
                            int index = st.ToLower().IndexOf("m");

                            if (index == -1)
                                index = st.Length;
                            total += st.Substring(0, index - 1).ToNumber();
                        }
                }else if (selIds.ToList().All(id => ACD.DB._isDim(id)))
                {
                    foreach (ObjectId dimId in selIds)
                        {
                            pPos[] pts = ACD.DB._getDimPoints(dimId);
                            
                            total += pts[0].DistanceTo(pts[1]);
                        }
                }
                else if (selIds.ToList().All(id => ACD.DB._isPolyline(id) || ACD.DB._isHatch(id)))
                {
                    foreach (ObjectId lwpId in selIds)
                    {
                        pPos[] pts = ACD.DB._getVertices(lwpId);
                        total += pts.Area();
                    }
                }

                ACD.WR("Total:" + total);

                ACD.Focus();
            }
        }
    }
}

