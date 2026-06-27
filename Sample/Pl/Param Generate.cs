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
using Autodesk.AEC.Interop.ArchBase;


using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class ParamGenerateCLS
    {
        static string _relativePList(IEnumerable<pPos> ls, IEnumerable<pPos> r, bool isScale = false)
        {
            //ACD.WR("L1");
            string res = "new pPos[]{";
            foreach (pPos pt in ls)
                res += _relativePoint(pt, r , isScale) + ",";

            if (res.EndsWith(","))
                res = res.Substring(0, res.Length - 1);

            //ACD.WR("L2");
            res += "}";

            return res;
        }

        static string _relativeVal(double v, IEnumerable<pPos> r, int axis, bool isScale = false)
        {
            string caxis = axis == 0 ? "X" : "Y";
            return isScale ?
                 "R[0]." + caxis + "+(R[1]." + caxis + "-R[0]." + caxis + ")*" + ((v - r.ElementAt(0)[axis]) 
                 / (r.ElementAt(1)[axis] - r.ElementAt(0)[axis])).roundNumber(0.01)
                   : "R[0]." + caxis + "+" + (v - r.ElementAt(0)[axis]).roundNumber(0.01);
        }

        static string _relativePoint(pPos pt, IEnumerable<pPos> r, bool isScale = false)
        {
            return "new pPos(" +  _relativeVal(pt.X, r, 0, isScale) + "," + _relativeVal(pt.Y, r, 1, isScale) + ")";
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;

                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    ACD.WR("Select rect-region by 2 points");
                    ACD.Get2Points();
                    bool isScale = ACD.ED.GetInputString("Input Scale?", "S").ToUpper().StartsWith("S");
                    pPos[] r = new pPos[] { ACD.MinPoint, ACD.MaxPoint };

                    string content = "";

                    foreach (ObjectId selId in selIds)
                    {
                        //ACD.WR("LOK1 {0}", selId.ObjectClass.DxfName);
                        string layer = ACD.DB._getLayer(selId);
                                                
                        if (ACD.DB._isCircle(selId))
                            content += "db.DrawCircle(" + _relativePoint(ACD.DB._getPoint(selId), r, isScale)
                                 + "," + ACD.DB._getRadius(selId) + ",\"LAYER=" + layer + "\");";
                        else if (ACD.DB._isPolyline(selId) || ACD.DB._isLine(selId))
                            content += "db.DrawPolyline(" + _relativePList(ACD.DB._getVertices(selId), r, isScale)
                                + "," + (ACD.DB._isPolylineClosed(selId) ? "true" : "false") + ",\"LAYER=" + layer + "\");";
                       // ACD.WR("LOK2");

                        content += "\r\n";
                    }

                    ACD.WR("Content: {0}", content);
                    Clipboard.SetText(content);
                    //ACD.WR("LOK3");
                }
                
                ACD.Focus();
            }
        }
    }
}

