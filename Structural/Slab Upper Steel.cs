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
    public class SlabUpperSteelCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                
                PosCollection pls = new PosCollection("<REGION/>");
                //ACD.WR("PTS {0}", pls.Count);

                if (pls.Count > 0)
                {
                    pls = pls.Select(ls => ls.OrderBy(p => p.X).ThenBy(p => p.Y).ToArray()).ToCollectionSameClosed();

                    foreach (pPos[] ls in pls)
                   if(ls.Length > 1)
                       {
                        pPos[] bb = ls.Boundary();
                        
                        List<pPos> pts = new List<pPos>();
                        pts.Add(new pPos(ls.First().X, ls.First().Y - 100));

                            if (ls.First()._isVeryClosed(ls.Last(), 10))
                            {
                                for (int i = 0; i < ls.Length - 1; i++)
                                    pts.Add(ls[i]);

                                pts.Add(new pPos(ls[ls.Length - 2].X, ls[ls.Length - 2].Y - 100));
                            }else
                            {
                                for (int i = 0; i < ls.Length; i++)
                                    pts.Add(ls[i]);

                                pts.Add(new pPos(ls[ls.Length - 1].X, ls[ls.Length - 1].Y - 100));
                            }

                            ACD.DB.DrawPolyline(pts, false, "LAYER=B-Steel");
                            //ACD.DB.DrawStylePolyline("USTEEL", ls, "LAYER=B-Steel");
                            ACD.DB.CreateText("#C%%C10a200", 
                            new pPos(bb.CenterPoint().X, bb[1].Y + 100), 2, 0, "LAYER=B-Steel");
                        
                    }
                }

                ACD.Focus();
            }
        }
    }
}

