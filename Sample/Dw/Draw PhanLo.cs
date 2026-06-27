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


    public class Draw3DViewCLS
    {
        
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if(selIds.Count > 0 && selIds.ToList().Any(__id => ACD.DB._isPolyline(__id) ))
                {

                    ACD.Get2Points();
                    pPos p1 = ACD.FirstPoint();
                    pPos p2 = ACD.LastPoint();

                    if(p1 != null && p2 != null)
                    {


                    }
                }

                ACD.Focus();
            }
        }
    }
}