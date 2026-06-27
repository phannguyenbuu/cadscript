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
   
    public class StraightenCLS
    {
        static int ROUND = 50;
        
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection ids = ACD.GetSelection();
                if(ids.Count > 0)
                {
                    ids = ids.ToList().Where(id => ACD.DB._isVertice(id)).ToCollection();

                    PosCollection pls = ACD.DB._getAllVertices(ids);
                    pls = pls.StraightenByRound(ROUND);

                    foreach(pPos[] pts in pls)
                        ACD.DB.DrawPolyline(pts, "LAYER=A-FIN");

                    ACD.DB.EraseObjects(ids);
                }
                
            }
            ACD.Focus();
        }
    }
}

