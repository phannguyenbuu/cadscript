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
    public class BlockInsertCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                PosCollection pls = new PosCollection("<REGION/>");
                
                int bbx_index = (int)"<BBX/>".ToNumber();
                int bby_index = (int)"<BBY/>".ToNumber();
                                
                string blockname = "<NAME/>";
                int fit_index = (int)"<FIT/>".ToNumber();
                double sc = 1;
                foreach (pPos[] pts in pls)
                {
                    pPos[] bb = pts.Boundary();
                    double w = bb.Size()[fit_index];
                    pPos pt = new pPos(bb[bbx_index].X, bb[bby_index].Y);

                    ObjectIdCollection ids = new ObjectIdCollection();

                    if (ACD.DB.HasBlock(blockname).IsNull)
                        ids.AddRange(ACD.DB.Insert(DE.CAD_TEMPLATE_FILE, blockname, new pPos[] { pt }));
                    else
                        ids.Add(ACD.DB.Insert(blockname, pt));

                    foreach (ObjectId id in ids)
                    {
                        ACD.DB._setScale(id, 1, 1);

                        pPos[] bb1 = ACD.DB._getBound(id);
                        sc = w / bb1.Size()[fit_index];
                        ACD.DB._setScale(id, sc, sc);
                    }
                }

                ACD.Focus();
            }
        }
    }
}

