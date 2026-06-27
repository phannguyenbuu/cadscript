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
    public class BeamInfoCLS
    {
        public double[] Distance;
        public double Width, Height;
        public List<double[]> ColumnRelations;
        public pPos StartPoint, EndPoint;
        public pPos[] NameTextBound;
        public double StartDistance, EndDistance;
        public int BeamAxis;
        public string Name, IndexName;
        pPos[] wallshape;
        double gridsize;

        public BeamInfoCLS(ObjectId beamId, List<double[]> gridxys)
        {
            pPos[] ls = ACD.DB._getVertices(beamId).OrderBy(p => p.X.roundNumber(100)).ThenBy(p => p.Y).ToArray();
            StartPoint = ls[0];
            EndPoint = ls[1];

            Name = ACD.DB._getIdName(beamId);
            Width = Name.filter("Dx").First().ToNumber() * 10;
            Height = Name.filter("Dx").Last().ToNumber() * 10;

            double a = Math.Abs((EndPoint - StartPoint).AngleAxisVsVector(new pPos(1, 0))) % 180;
            BeamAxis = a <= 45 || a >= 135 ? 0 : 1;

            ObjectId tmp = ACD.DB.GetWallShape(beamId);
            wallshape = ACD.DB._getVertices(tmp);
            ACD.DB.EraseObject(tmp);

            pPos[] bb = wallshape.Boundary();

            ColumnRelations = gridxys[BeamAxis].Select(n 
                => new double[] { StartPoint[BeamAxis] - n, EndPoint[BeamAxis] - n }).ToList();

            StartDistance = StartPoint[BeamAxis] - gridxys[BeamAxis].First();
            EndDistance = EndPoint[BeamAxis] - gridxys[BeamAxis].Last();

            gridsize = gridxys[BeamAxis].Max() - gridxys[BeamAxis].Min();
            //int nex = (BeamAxis + 1) % 2;

            //ACD.WR(this.ToString() + "Cols:" + gridxys[BeamAxis].Length + ";" + gridxys[(BeamAxis + 1)%2].Length);
        }

        public override string ToString()
        {
            string res = String.Format("BeamAxis:{0} Size:{1}x{2} Start:{3} Columns:{4}", 
                BeamAxis, Width, Height, StartPoint[BeamAxis].roundNumber((int)Width), ColumnRelations.Count);

            res += "(";
            foreach (double[] ds in ColumnRelations)
                res += ds.Select(d => d.roundNumber(10)).ToTextDouble(",") + ";";
            res += ")";
            return res;
        }

        public double Length
        {
            get
            {
                return StartPoint.DistanceTo(EndPoint);
            }
        }

        public pPos NameTextPoint
        {
            get
            {
                pPos pt = StartPoint.Parallel(EndPoint, Width/2).CenterPoint();

                if(pt.Inside(wallshape))
                    pt = StartPoint.Parallel(EndPoint, -Width / 2).CenterPoint();

                return pt;
            }
        }

        public string Code
        {
            get
            {
                string res = "";
                res = "Chapter=" + IndexName + "|No=";
                string dist = "";
                string type = "";

                for (int i = 0; i < ColumnRelations.Count; i++)
                    if (!(Math.Abs(ColumnRelations[i][0]) > gridsize
                        + 100 && Math.Abs(ColumnRelations[i][1]) > gridsize + 100))
                    {
                        if (i == 0)
                        {
                            res += (BeamAxis == 0 ? (i + 1).ToString() : ((char)(65 + i)).ToString()) + ";";
                            dist += StartDistance.roundNumber(10) + ";";
                            
                            if (StartDistance.roundNumber(10) < 0)
                                type += "Extent;";
                            else if(StartDistance.roundNumber(10) == 0)
                                type += "Column Fit;";
                            else
                                type += "Column Middle;";
                        }
                        else if (i == ColumnRelations.Count - 1)
                        {
                            res += (BeamAxis == 0 ? (i + 1).ToString() : ((char)(65 + i)).ToString()) + ";";
                            dist += EndDistance.roundNumber(10) + ";";

                            if(EndDistance.roundNumber(10) > 0)
                                type += "Extent;";
                            else if (EndDistance.roundNumber(10) == 0)
                                type += "Column Fit;";
                            else
                                type += "Column Middle;";
                        }
                        //else
                            //dist += "0;";
                        //dist += Math.Abs(i == 0 ? 0 : ColumnRelations[i][0] - ColumnRelations[i - 1][0]).roundNumber(10) + ";";
                        //type += "Column Middle;";
                    }


                res = res + "|Distance=" + dist + "|Type=" + type;

                return res;
            }
        }

        public static string[] CreateBeamSizeTextPoints(IEnumerable<BeamInfoCLS> _beamlist, double dist_size = 3000)
        {
            string[] res = null;
            List <BeamInfoCLS> beamlist = _beamlist.OrderBy(beam => beam.Name)
                        .ThenBy(beam => -beam.Width)
                        .ThenBy(beam => -beam.Height)
                        .ThenBy(beam => -beam.Length)
                        .ThenBy(beam => beam.StartPoint.X.roundNumber(100))
                        .ThenBy(beam => beam.StartPoint.Y).ToList();

            string[] infors = beamlist.Select(beam => beam.ToString()).Distinct().ToArray();
            double text_size = 2 * ACD.DB.Cannoscale.Name.filter(":").Last().ToNumber();

            List<string> clip_contents = new List<string>();
            int[] indexes = new int[] { 0, 0 };

            for (int i = 0; i < infors.Length; i++)
            {
                BeamInfoCLS[] sel_beams = beamlist.Where(beam => beam.ToString() == infors[i]).ToArray();

                if (sel_beams.Length > 0)
                {
                    foreach (BeamInfoCLS beam in sel_beams)
                    {
                        beam.IndexName = "D" + (beam.BeamAxis == 0 ? "X" : "Y") + (indexes[beam.BeamAxis] + 1);
                        beam.IndexName += " " + beam.Width + "x" + beam.Height;

                        ObjectId txtId = ACD.DB.CreateText(beam.IndexName, beam.NameTextPoint,
                            text_size, 0, "ANNO=" + ACD.DB.Cannoscale.Name);
                        
                        if (beam.BeamAxis == 1)
                            ACD.DB._setRotation(txtId, 90);

                        beam.NameTextBound = ACD.DB._getBound(txtId);
                    }

                    indexes[sel_beams.First().BeamAxis]++;
                    clip_contents.Add(sel_beams.First().Code);
                }
            }

            res = clip_contents.OrderBy(s => s._firstProp()).ToArray();

            PosCollection pls = beamlist.Select(beam => new pPos[] { beam.StartPoint, beam.EndPoint }).ToCollectionSameClosed();
            pls.Closed = pls.Select(ls => false).ToArray();

            List<pPos> interps = new List<pPos>();
            interps.AddRange(pls.Select(ls => ls.ExtentLine(1000)).ToCollectionSameClosed().SelfIntersect);
            interps.AddRange(pls.AllPoints);

            foreach (BeamInfoCLS beam in beamlist)
            {
                List<pPos> tmps = new List<pPos> { beam.StartPoint, beam.EndPoint };
                tmps.AddRange(interps.Where(p
                    => p.DistanceTo(beam.StartPoint, beam.EndPoint) <= 2 * beam.Width));
                List<double[]> xys = tmps.ExtractPtsXY();
                //ACD.WR("XYS {0}", xys[beam.BeamAxis].ToText(";"));

                for (int i = 0; i < xys[beam.BeamAxis].Length - 1; i++)
                {
                    pPos p = beam.NameTextPoint;
                    double v = p[beam.BeamAxis];
                    p[beam.BeamAxis] = (xys[beam.BeamAxis][i] + xys[beam.BeamAxis][i + 1])/2;

                    ObjectId txtId = ACD.DB.CreateText("#C" + beam.Width + "x" + beam.Height, p,
                        text_size, 0, "ANNO=" + ACD.DB.Cannoscale.Name);
                    if (beam.BeamAxis == 1)
                        ACD.DB._setRotation(txtId, 90);

                    if (beam.NameTextBound.IntersectBounding(ACD.DB._getBound(txtId)))
                        ACD.DB.EraseObject(txtId);
                }

                //ACD.WR("Beam {0} is {1}", beam.IndexName, beam.ToString());
            }

            return res;
        }
    
        
    }
}

