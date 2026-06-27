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
    public class RegionCLS
    {
        static void _showAreaText(Database db)
        {
            if (Drawing_Cycles != null && Drawing_Cycles.Count() > 0)
                foreach (var cycle in Drawing_Cycles)
                    if (!cycle.Key.empty())
                    {
                        double area = (cycle.Value.Area() / 1000000).roundNumber(2);

                        db.GetEntities(cycle.Value, EN_SELECT.AC_DXF, "MTEXT");
                        ObjectId txtId = ObjectId.Null;

                        pPos txtpt = null;

                        if (IR.SelectedIds.Count > 0)
                        {
                            int index = IR.SelectedIds.FindIndex(id => db._getContent(id, true).Contains("M²"));
                            if (index != -1)
                                txtId = IR.SelectedIds[index];
                            else
                                txtpt = db._getBound(IR.SelectedIds[0])[0] + new pPos(0, -400);
                        }

                        //ACD.WR("SEL_IDS {0} TXTID {1}", IR.SelectedIds.Count, txtId);

                        if (txtpt == null)
                            txtpt = cycle.Value.CenterPoint();

                        if (txtId.IsNull)
                            txtId = db.CreateText("", txtpt, 200, 0, "LAYER=" + DE.DEF_LAYER_TEXT);

                        db._setContent(txtId, "#C" + cycle.Key + "\r\n" + area.ToString() + " M\\U+00B2");
                    }
        }

        static Dictionary<string, pPos[]> Drawing_Cycles;

        static void _dimRegion(IEnumerable<pPos> region)
        {
            List<pPos> pts = region.ToList();
            PosCollection pls = pts.ToCollection();
            pts.AddRange(pts.GetSegment(true).Select(seg => seg.CenterPoint()));

            List<double[]> xys = pts.ExtractPtsXY(1);
            pPos ct = pts.CenterPoint();
            pPos base_vector = new pPos(1, 0);

            for(int axis = 0; axis < 2; axis++)
            {
                int nex = (axis + 1) % 2;
                PosCollection dims = new PosCollection();

                for(int i = 0; i < xys[axis].Length; i++)
                {
                    pPos pt = new pPos(0, 0);
                    pt[axis] = xys[axis][i];
                    pt[nex] = ct[nex];

                    //dims.Add(pls._getMinDimArea(pt, axis, base_vector));
                }

                if (dims.Count > 0)
                {
                    dims = dims.OrderBy(ls => -ls.Length()).ToCollectionSameClosed();
                    pPos[] dim = dims.First();

                    IDimChain.CreateDimension(ACD.DB, dim[0], dim[1], 200, 200);
                }
            }
        }
        
        public static void Main(string[] args)
        {
            ACD.DB.GetEntities(ACD.bRect, EN_SELECT.AC_ALL);
            ObjectIdCollection selIds = IR.SelectedIds;

            if (selIds != null && selIds.Count > 0)
            {
                PosCollection pls = new PosCollection();

                foreach (ObjectId id in selIds)
                    if (ACD.DB._isWall(id))
                    {
                        ObjectId lwpId = ACD.DB.GetWallShape(id);
                        pPos[] ls = ACD.DB._getVertices(lwpId);
                
                        pls.Add(ls);
                    }else if (ACD.DB._getLayer(id).StartsWith("A") 
                        && (ACD.DB._isPolyline(id) || ACD.DB._isLine(id)))
                            pls.Add(ACD.DB._getVertices(id));

                ACD.DB.GetEntities(ACD.bRect, EN_SELECT.AC_ALL);
                ACD.DB.EraseObjects(IR.SelectedIds.ToList().Where(id => !selIds.Contains(id)).ToCollection());
                GraphConsole.Compute(pls);

                double w = 10;
                //w = ACD.ED.GetInputString("Linewidth:").ToNumber();

                ACD.WR("Regions: {0}", GraphConsole.ResultPts.Count);

                foreach (pPos[] reg in GraphConsole.ResultPts)
                {
                    ACD.DB.DrawPolyline(reg, true, "LAYER=" + DE.DEF_LAYER_REGION + "|LINEWIDTH=" + w);
                    //_dimRegion(reg);
                }
            }
            
            ACD.Focus();
        }
    }
}

