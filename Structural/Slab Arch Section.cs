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
    public class SlabArchSectionCLS
    {
        static double BW, BH, SLB;

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                //ACD.WR("<Draw beam>Select region rectangle by CLOSED polyline, LINE is gridline");

                PosCollection pls = new PosCollection("<REGION/>");
                
                if (pls.Count > 0)
                {
                    BW = "<BW/>".ToNumber(800);
                    BH = "<BH/>".ToNumber(300);
                    SLB = "<SLB/>".ToNumber(100);
                    
                    int slab_cote_type = 0;
        
                        pPos[] region = new pPos[0];
                        //double SLB = 100;

                        if (slab_cote_type == 0)
                            region = new pPos[] { new pPos(BW + SLB, 0), new pPos(0, 0), new pPos(0, -BH),
                    new pPos(BW, -BH), new pPos(BW, -SLB), new pPos(BW + SLB, -SLB) };
                        else if (slab_cote_type == 1)
                            region = new pPos[] { new pPos(BW + SLB, -SLB), new pPos(BW, -SLB),
                    new pPos(BW, 0), new pPos(0, 0), new pPos(0, -BH), new pPos(BW + SLB, -BH)};

                    List<double[]> interps = pls.SelfIntersect.ExtractPtsXY();


                    foreach (pPos[] ls in pls)
                    {
                        
                    }

                    //    ACD.DB.DrawPolyline(ls, false, "LAYER=0");

                }

                ACD.Focus();
            }
        }
    }
}

