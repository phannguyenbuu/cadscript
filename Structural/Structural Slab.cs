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
    public class StructualSlabCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    selIds = selIds.Cast<ObjectId>()
                        .OrderBy(id => id.ObjectClass.DxfName)
                        .ThenBy(id => ACD.DB._getBound(id).CenterPoint().X.roundNumber(10))
                        .ThenBy(id => ACD.DB._getBound(id).CenterPoint().X.roundNumber(10)).ToCollection();

                    string content = "";

                    int index = selIds.FindIndex(id => ACD.DB._isOutterBound(id));

                    pPos[] bb = index != -1 ? ACD.DB._getBound(selIds[index]) : ACD.DB._getBound(selIds);

                    for (int i = 0; i < selIds.Count; i++)
                        if (ACD.DB._getLayer(selIds[i]).ToUpper() != "DEFPOINTS"
                            && (ACD.DB._isDXF(selIds[i], "LWPOLYLINE", "HATCH", "LINE")))
                        {
                            content += ACD.DB._getIdInfo(selIds[i]);
                            pPos[] pts = ACD.DB._getVertices(selIds[i]);
                            pPos[] obj_bb = pts.Boundary();

                            double offsetX = obj_bb[0].X - bb[0].X;
                            double offsetY = obj_bb[1].Y - bb[1].Y;

                            if (IR.PolylineClosed)
                            {
                                if (Math.Abs(offsetX) > 5)
                                    content += "|OFFSET=" + offsetX.roundNumber(1);
                                else
                                    content += "|DEPTH=" + (obj_bb[1].X - obj_bb[0].X).roundNumber(1)
                                        + "|MOVE=0," + offsetX.roundNumber(1);
                            }
                            else
                                content += "|MOVE=0," + offsetX.roundNumber(1);

                            content += "\r\n";
                        }

                    System.Windows.Forms.Clipboard.SetText(content);
                }

                ACD.Focus();
            }
        }
    }
}

