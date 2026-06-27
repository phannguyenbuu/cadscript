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
    public class PolylineTextLengthCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection selIds = ACD.GetSelection();

                //pPos pt = ACD.bRect.CenterPoint();

                //ACD.WR("OK1");
                Dictionary<string, double> values = selIds.ToList().Select(id 
                    => db._isWall(id) ? db._getIdName(id) : db._getLayer(id)).Distinct().ToDictionary(s => s, s => 0.0);
                //ACD.WR("OK2");
                //Dictionary<string, double> values = infors

                foreach (ObjectId lwpId in selIds)
                    if (db._isPolyline(lwpId) || db._isWall(lwpId))
                    {
                        pPos[] pts = db._getVertices(lwpId);
                        double val = db._getLength(lwpId);
                        //ACD.WR("OK3");

                        //pPos midpt = pts.CenterPoint();

                        //if (pts.Length > 2)
                        //{
                        //    int mid = (int)(pts.Length / 2);
                        //    midpt = (pts[mid] + pts[mid + 1]) / 2;
                        //}

                        //ACD.WR("OK4.1");
                        //db.CreateText(Math.Round((val + DE.DEF_TRACK_VALUE) / 1000, 2).ToString() + "m", midpt, 2,0, "ANNO=TRUE");
                        //ACD.WR("OK4.2");
                        string style = db._isWall(lwpId) ? db._getIdName(lwpId) : db._getLayer(lwpId);
                        values[style] += val;
                        //ACD.WR("OK4.3");
                    }
                //ACD.WR("OK5");
                string content = "";
                foreach (string key in values.Keys)
                    content += key + "=" + Math.Round((values[key] + DE.DEF_TRACK_VALUE) / 1000, 2).ToString() + "m\r\n";
                //ACD.WR("OK6");
                ACD.WR(content);

                //if (pt != null)
                    //ACD.DB.CreateText(content, pt, 2.5);
            }
        }
    }
}

