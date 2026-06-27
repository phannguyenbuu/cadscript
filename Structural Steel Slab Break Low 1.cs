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
    public class StructuralSteelSlabBreakLow1
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                bool ctrl = ACD.ControlHold;
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    //int mode = (int)ACD.ED.GetInputString("Enter mode").ToNumber();
                    double range = ACD.ED.GetInputString("Enter range","100").ToNumber();
                    pPos pt = ACD.GetPoint();
                    
                    for (int i = 0; i < selIds.Count; i++)
                    {
                        pPos[] pts = ACD.DB._getPolylineVerts(selIds[i]);
                        pPos[] bb = ACD.DB._getBound(selIds[i]);
                        pPos sz = bb.Size();
                        pt.DistanceToPts(pts);

                        int axis = sz.X > sz.Y ? 0 : 1;
                        pPos[] r2 = bb[0].RectToPoint(axis == 0 ? 
                            new pPos(pt.X, bb[1].Y) : new pPos(bb[1].X, pt.Y));

                        double d = ctrl ? -Math.Abs(range) : 0;
                        pPos[] r1 = bb[1].RectToPoint(axis == 0 ? new pPos(pt.X + d, bb[0].Y) 
                            : new pPos(bb[0].X, pt.Y + d));

                        //ACD.DB.DrawPolyline(r1);
                        //ACD.DB.DrawPolyline(r2);

                        r1 = ACD.DB.TrimPolyline(selIds[i], r1);
                        r2 = ACD.DB.TrimPolyline(selIds[i], r2);

                        List<pPos> new_pts = new List<pPos>();
                        new_pts.AddRange(r1);

                        pPos mv = new pPos(0, 0);
                        mv[(axis + 1) % 2] += range;

                        new_pts.AddRange(r2.Move(mv));
                        ObjectId newId = ACD.DB.DrawPolyline(new_pts);

                        ACD.DB._setLayer(newId, ACD.DB._getLayer(selIds[i]));
                        ACD.DB.EraseObject(selIds[i]);
                    }
                }

                ACD.Focus();
            }
        }
    }
}

