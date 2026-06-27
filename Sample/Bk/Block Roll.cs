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
    public class BlockRollCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                PosCollection pls = new PosCollection("<REGION/>");
                
                double a = "<A/>".ToNumber();
                //double sz = "<SZ/>".ToNumber();
                string blockname = "<NAME/>";

                foreach (pPos[] pts in pls)
                {
                    pPos[] bb = pts.Boundary();
                    double w = bb.Size()[0];
                    pPos pt = bb.CenterPoint();

                    ObjectIdCollection ids = new ObjectIdCollection();

                    if (ACD.DB.HasBlock(blockname).IsNull)
                        ids.AddRange(ACD.DB.Insert(DE.CAD_TEMPLATE_FILE, blockname, new pPos[] { pt }));
                    else
                        ids.Add(ACD.DB.Insert(blockname, pt));
                    
                    foreach (ObjectId id in ids)
                    {
                        ACD.DB._setScale(id, 1, 1);
                        pPos[] bb1 = ACD.DB._getBound(id);
                        double sc = w / bb1.Size()[0];
                        ACD.DB._setScale(id, sc, sc);
                    }

                    ACD.DB.DrawPolyline(new pPos[] { pt - new pPos(w / 2 + a, 0), pt + new pPos(w / 2 + a, 0) }, false, "LAYER=A-Hidden");
                    ACD.DB.DrawPolyline(new pPos[] { pt - new pPos(0, w/2 + a), pt + new pPos( 0, w / 2 + a) }, false, "LAYER=A-Hidden");
                }

                ACD.Focus();
            }
        }
    }
}

