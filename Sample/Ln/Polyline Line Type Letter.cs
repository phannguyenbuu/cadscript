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
    public class LineTXpeLetterCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                string type = ACD.ED.GetInputString("Letter", "");
                ObjectIdCollection selIds = ACD.GetSelection();

                string default_value = "";

                if (selIds.Count > 0)
                    default_value = selIds.ToList().Select(_id => ACD.DB.GetLinetypeText(_id)).FirstOrDefault(_s => !_s.et_());

                

                if (!type.empty())
                {
                    type = type.Trim();
                    
                    ACD.DB.CreateComplexLinetype("Name=" + type + "|Value=" + type, "------ ");
                        //2, 3, 2, 0, 0);

                    foreach (ObjectId id in selIds)
                        if (ACD.DB._isPolyline(id) || ACD.DB._isLine(id) || ACD.DB._isArc(id))
                            ACD.DB._setLineworkLineType(id, type);
                }

                //foreach (ObjectId lwpId in selIds)
                //{
                //    pPos[] pts = ACD.DB._getVertices(lwpId);
                //    ACD.DB.DrawPolyline(pts, true, "LAYER=DEFPOINTS|LTYPE="
                //        + DE.DEF_LTXPE_AROUND_BORDER._prop("Name") + "|LSCALE=20|LWIDTH=100");
                //}
            }
            ACD.Focus();
        }
    }
}

