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
    public class CeilHookCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                PosCollection pls = new PosCollection("<REGION/>");
                double a = "<A/>".ToNumber();
                double b = "<B/>".ToNumber();
                double c = "<C/>".ToNumber();
                double d = "<D/>".ToNumber();
                double e = "<E/>".ToNumber();

                string block_top = "<BTOP/>";
                string block_mid = "<BMID/>";
                string block_bottom = "<BBTM/>";
                string layer = "<LAYER/>";
                string slab_action = "<ACT_SLAB/>";

                string type = "<TYPE/>";

                ObjectIdCollection ids = new ObjectIdCollection();

                foreach (pPos[] pts in pls)
                {
                    pPos[] bb = pts.Boundary();
                    
                    double w = bb.Size().X;
                    double h = bb.Size().Y;

                    //if (type == "TRUNKING")
                    //{
                    //    pPos[] ls = new pPos[] {new pPos(bb[0].X + 50)}
                    //    ids.Add(ACD.DB.DrawPolyline(new pPos()))
                    //}
                    //else
                    {
                        List<pPos> ls = new List<pPos>();

                        if (w < c)
                        {
                            //Without slab
                            ls = new List<pPos> { new pPos((bb[0].X + bb[1].X) / 2, bb[0].Y) };
                        }
                        else
                        {
                            //With slab
                            ACD.DB.HatchKey(bb[0].RectToPoint(new pPos(bb[1].X, bb[0].Y + e)).ToCollection(), slab_action);
                            bb[0].Y += e;
                            h = bb.Size().Y;

                            if (w < d)
                                ls = new List<pPos> { new pPos(bb[0].X + 50, bb[0].Y), new pPos(bb[1].X - 50, bb[0].Y) };
                            else
                            {
                                double x = 0;
                                while (x < w)
                                {
                                    ls.Add(new pPos(bb[0].X + x, bb[0].Y));
                                    x += d;
                                }

                                if (x - d < w - 50)
                                    ls.Add(new pPos(bb[1].X - 50, bb[0].Y));
                                else
                                    ls[ls.Count - 1] = new pPos(bb[1].X - 50, bb[0].Y);
                            }
                        }

                        foreach (pPos pt in ls)
                        {
                            if (ACD.DB.HasBlock(block_top).IsNull)
                                ids.AddRange(ACD.DB.Insert(DE.CAD_TEMPLATE_FILE, block_top, new pPos[] { pt + new pPos(0, h) }));
                            else
                                ids.Add(ACD.DB.Insert(block_top, pt + new pPos(0, h)));

                            if (ACD.DB.HasBlock(block_mid).IsNull)
                                ids.AddRange(ACD.DB.Insert(DE.CAD_TEMPLATE_FILE, block_mid, new pPos[] { pt + new pPos(0, h / 2) }));
                            else
                                ids.Add(ACD.DB.Insert(block_mid, pt + new pPos(0, h / 2)));

                            if (ACD.DB.HasBlock(block_bottom).IsNull)
                                ids.AddRange(ACD.DB.Insert(DE.CAD_TEMPLATE_FILE, block_bottom, new pPos[] { pt + new pPos(b * 2, 0) }));
                            else
                                ids.Add(ACD.DB.Insert(block_bottom, pt + new pPos(b * 2, 0)));

                            ids.Add(ACD.DB.DrawPolyline(new pPos[] { pt + new pPos(2 * b, a), pt + new pPos(2 * b, h / 2 - a / 2) }, false));
                            ids.Add(ACD.DB.DrawPolyline(new pPos[] { pt + new pPos(3 * b, a), pt + new pPos(3 * b, h / 2 - a / 2) }, false));
                            ids.Add(ACD.DB.DrawPolyline(new pPos[] { pt + new pPos(0, h / 2 + a / 2), pt + new pPos(0, h - a) }, false));
                            ids.Add(ACD.DB.DrawPolyline(new pPos[] { pt + new pPos(b, h / 2 + a / 2), pt + new pPos(b, h - a) }, false));
                        }
                    }
                }

                if(!layer.empty())
                    ACD.DB._setLayer(ids, layer);

                ACD.Focus();
            }
        }
    }
}

