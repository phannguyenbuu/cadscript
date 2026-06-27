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

namespace AcadScript
{
    public class PolylineClosedCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                PosCollection pls = new PosCollection();

                foreach (ObjectId lwpId in selIds)
                    if (ACD.DB._isPolyline(lwpId))
                    {
                        pPos[] pts = ACD.DB._getVertices(lwpId);

                        if(pts.Length > 2)
                            ACD.DB._setPolylineClosed(lwpId, true);
                        else
                        {
                            pts[0].Content = lwpId.ToString();
                            pls.Add(pts);
                        }
                        //ACD.WR("Obj {0} data {1}", lwpId, ACD.DB._getVertices(lwpId).ToText());
                    }
                    else if (ACD.DB._isHatch(lwpId))
                    {
                        ACD.DB.DrawHatchs(ACD.DB._getVertices(lwpId).Straighten(ACD.DB._isPolylineClosed(lwpId)).ToCollection(), lwpId);
                        ACD.DB.EraseObject(lwpId);
                    }

                ACD.Focus();
            }
        }
    }
}

