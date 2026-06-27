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

//
using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class AlignBlockAndTextCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                bool ctrl = ACD.ControlHold;

                ACD.WR("[CTRL] Block Align Midpoint [NONE] Block Align All");

                if (ctrl)
                    ACD.WR("Block Align Midpoint");
                else
                    ACD.WR("Block Align All");

                pPos pt = null;
                int axis = -1;

                ObjectIdCollection selIds = ACD.GetSelection();
                if (selIds.Count > 0)
                {
                    pPos[] bb = selIds.ToList().Where(id => ACD.DB._isPoint(id))
                            .Select(id => ACD.DB._isPoint(id)? ACD.DB._getPoint(id)
                            : ACD.DB._getBound(id).CenterPoint()).ToArray();

                    pPos sz = bb.Size();
                    
                    if (ctrl)
                    {
                        pPos ct = bb.CenterPoint();
                        ACD.Get2Points();

                        if (ACD.FirstPoint != null && ACD.LastPoint != null)
                        {
                            bb = new pPos[] { ACD.FirstPoint, ACD.LastPoint };
                            pt = (ACD.FirstPoint + ACD.LastPoint) / 2;
                            axis = Math.Abs(ct.X - pt.X) < Math.Abs(ct.Y - pt.Y) ? 0 : 1;
                        }
                    }
                    else
                    {
                        pt = ACD.GetPoint();
                        axis = sz.X < sz.Y ? 0 : 1;
                    }
                }

                if(pt != null && axis != -1)
                    foreach (ObjectId blockId in selIds)
                    {
                        pPos p = ACD.DB._isPoint(blockId) ? ACD.DB._getPoint(blockId) : ACD.DB._getBound(blockId).CenterPoint();

                        pPos newp = p.Clone();
                        newp[axis] = pt[axis];
                                                
                        ACD.DB.MoveObject(blockId, newp - p);
                    }
            }
            ACD.Focus();
        }
    }
}

