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
    public class StructInforCLS
    {
        static List<double[]> gridXY;

        static ObjectIdCollection _extractBeams(ObjectIdCollection selIds)
        {
            PosCollection beamPls = selIds._filterDXF("AEC_WALL").ToList().Select(id
                        => ACD.DB._getVertices(id)).ToCollectionSameClosed(false);

            pPos[] namelist = ACD.DB.GetBlockContents(selIds.ToList().Where(id => !ACD.DB._isGrid(id)).ToCollection()).ToArray();

            ObjectIdCollection res = new ObjectIdCollection();

            ACD.WR("Beams {0} NameList {1}", beamPls.Count, namelist.Select(p => p.Content).ToTextStr(","));

            foreach (pPos[] ls in beamPls)
            {
                pPos basept = ls.Boundary()[0];

                int axis = ls[0].AngleAxisVsVector(ls[1]);

                ACD.WR("Axis {0}", axis);

                double a = Math.Min(ls[0][axis], ls[1][axis]), b = Math.Max(ls[0][axis], ls[1][axis]);
                List<double> vals = new List<double>();

                vals.Add(a);
                vals.AddRange(gridXY[axis].Where(v => v >= a && v <= b));

                vals.Add(b);
                vals = vals.Distinct().ToList();

                ObjectIdCollection ids = new ObjectIdCollection();
                
                for (int i = 0; i < vals.Count; i++)
                {
                    double v = vals[i];
                    string gridname = IR.GetXYName(axis, v);

                    v -= basept[axis];

                    if (!gridname.empty())
                    {
                        ids.AddRange(ACD.DB.DrawCircle(new pPos(0, - 2000) + new pPos(v, 0), 80));
                        ids.Add(ACD.DB.CreateText("#M" + gridname,
                            new pPos(0, -2000) + new pPos(v, 0)));
                    }

                    if (i < vals.Count - 1)
                    {
                        ids.Add(ACD.DB.DrawPolyline(new pPos[] { new pPos(vals[i] - basept[axis], 0), 
                            new pPos(vals[i + 1] - basept[axis], 0) }, false));
                    }
                }

                var _namelist = namelist.Where(p => a <= p[axis] && p[axis] <= b && p.DistanceToPts(ls) <= 1000).ToArray();

                if (_namelist.Length > 0)
                {
                    pPos ptname = _namelist.OrderBy(p => - p.Content.Length).First();

                    ACD.WR("PTNAME {0}", ptname.Content);

                    ids.Add(ACD.DB.CreateText("#L" + ptname.Content, new pPos(0, 1000), 4));
                }

                string blockname = ACD.DB.uniqueBlockName("beam.");
                ACD.DB.NewBlock(ids, blockname, true, false, new pPos(0,0));

                ACD.WR("New block {0}", blockname);
                res.Add(ACD.DB.Insert(blockname, basept));
            }

            return res;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                //int mode = (int)ACD.ED.GetInputString("Select mode: 0. Legends from plans, 1. Object details from data, 2. Schedule from data").ToNumber();

                //if (mode == 0)
                {
                    ObjectIdCollection selIds = ACD.GetSelection();
                    gridXY = ACD.DB.GetGridXY(selIds);

                    ObjectIdCollection beamResult = _extractBeams(selIds);

                    ACD.DB.CreateLayer("B-BeamDetails,6");
                    ACD.DB._setLayer(beamResult, "B-BeamDetails");

                    ACD.WR("Result {0} beams", beamResult.Count);
                }
               
            }
        }
    }
}

