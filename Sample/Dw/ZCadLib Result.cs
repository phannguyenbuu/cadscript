using System;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;

namespace AcadScript
{
    public class ArchResult
    {

        static void _applyDST(DimStyleTableRecord dst, Dimension dim)
        {

            dim.Dimadec = dst.Dimadec;
            dim.Dimalt = dst.Dimalt;
            dim.Dimaltd = dst.Dimaltd;
            dim.Dimaltf = dst.Dimaltf;
            //dim.Dimaltmzf = dst.Dimaltmzf;
            //dim.Dimaltmzs = dst.Dimaltmzs;
            dim.Dimaltrnd = dst.Dimaltrnd;
            dim.Dimalttd = dst.Dimalttd;
            dim.Dimalttz = dst.Dimalttz;
            dim.Dimaltu = dst.Dimaltu;
            dim.Dimaltz = dst.Dimaltz;
            dim.Dimapost = dst.Dimapost;
            dim.Dimarcsym = dst.Dimarcsym;
            dim.Dimasz = dst.Dimasz;
            dim.Dimatfit = dst.Dimatfit;
            dim.Dimaunit = dst.Dimaunit;
            dim.Dimazin = dst.Dimazin;
            dim.Dimblk = dst.Dimblk;
            dim.Dimblk1 = dst.Dimblk1;
            dim.Dimblk2 = dst.Dimblk2;
            dim.Dimcen = dst.Dimcen;
            dim.Dimclrd = dst.Dimclrd;
            dim.Dimclre = dst.Dimclre;
            dim.Dimclrt = dst.Dimclrt;
            dim.Dimdec = dst.Dimdec;
            dim.Dimdle = dst.Dimdle;
            dim.Dimdli = dst.Dimdli;
            dim.Dimdsep = dst.Dimdsep;
            dim.Dimexe = dst.Dimexe;
            dim.Dimexo = dst.Dimexo;
            dim.Dimfrac = dst.Dimfrac;
            dim.Dimfxlen = dst.Dimfxlen;
            dim.DimfxlenOn = dst.DimfxlenOn;
            dim.Dimgap = dst.Dimgap;
            dim.Dimjogang = dst.Dimjogang;
            dim.Dimjust = dst.Dimjust;
            dim.Dimldrblk = dst.Dimldrblk;
            //dim.Dimlfac = dst.Dimlfac;
            dim.Dimlim = dst.Dimlim;
            dim.Dimltex1 = dst.Dimltex1;
            dim.Dimltex2 = dst.Dimltex2;
            dim.Dimltype = dst.Dimltype;
            dim.Dimlunit = dst.Dimlunit;
            dim.Dimlwd = dst.Dimlwd;
            dim.Dimlwe = dst.Dimlwe;
            //dim.Dimmzf = dst.Dimmzf;
            //dim.Dimmzs = dst.Dimmzs;
            dim.Dimpost = dst.Dimpost;
            dim.Dimrnd = dst.Dimrnd;
            dim.Dimsah = dst.Dimsah;

            //dim.Dimscale = dst.Dimscale;
            dim.Dimsd1 = dst.Dimsd1;
            dim.Dimsd2 = dst.Dimsd2;
            dim.Dimse1 = dst.Dimse1;
            dim.Dimse2 = dst.Dimse2;
            dim.Dimsoxd = dst.Dimsoxd;
            dim.Dimtad = dst.Dimtad;
            dim.Dimtdec = dst.Dimtdec;
            //dim.Dimtfac = dst.Dimtfac;
            dim.Dimtfill = dst.Dimtfill;
            dim.Dimtfillclr = dst.Dimtfillclr;
            dim.Dimtih = dst.Dimtih;
            dim.Dimtix = dst.Dimtix;
            dim.Dimtm = dst.Dimtm;
            dim.Dimtmove = dst.Dimtmove;
            dim.Dimtofl = dst.Dimtofl;
            dim.Dimtoh = dst.Dimtoh;
            dim.Dimtol = dst.Dimtol;
            dim.Dimtolj = dst.Dimtolj;
            dim.Dimtp = dst.Dimtp;
            dim.Dimtsz = dst.Dimtsz;
            dim.Dimtvp = dst.Dimtvp;
            dim.Dimtxt = dst.Dimtxt;
            dim.Dimtxtdirection = dst.Dimtxtdirection;
            dim.Dimtzin = dst.Dimtzin;
            dim.Dimupt = dst.Dimupt;
            dim.Dimzin = dst.Dimzin;
        }


        static void createRDimension(Database db, pPos pt, double radius)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                DimStyleTableRecord dst = tr.GetObject(db.Dimstyle, OpenMode.ForWrite) as DimStyleTableRecord;

                // Create an angular dimension
                using (RadialDimension acRadDim = new RadialDimension())
                {
                    acRadDim.Center = pt.ToPoint3();
                    acRadDim.ChordPoint = new Point3d(pt.X + radius * Math.Sin(Math.PI / 4), pt.Y + radius * Math.Sin(Math.PI / 4), 0);
                    acRadDim.LeaderLength = 5;
                    acRadDim.DimensionStyle = db.Dimstyle;

                    // Add the new object to Model space and the transaction
                    btr.AppendEntity(acRadDim);
                    tr.AddNewlyCreatedDBObject(acRadDim, true);

                    db.AddCurrentAnnotative(acRadDim.ObjectId);
                    _applyDST(dst, acRadDim);
                    db._setLayer(acRadDim.ObjectId, pString.INI_String("DIM_LAYER"));
                }

                // Commit the changes and dispose of the transaction
                tr.Commit();
            }
        }

        static double[] levelUps(params double[] cote_params)
        {
            List<double> res = new List<double>();
            double curY = 0;

            foreach (double cote in cote_params)
            {
                double n = Math.Floor(cote / 100);

                for(int i = 0; i < n; i++)
                {
                    curY += cote / n;
                    res.Add((int)curY);
                }
            }

            return res.Distinct().ToArray();
        }

        static void _drawXBox(pPos p)
        {
            ACD.DB.Draw2D(new pPos(p.X, p.Y - 5).Rect(10, 10), 'c');
            ACD.DB.Draw2D(p.X, p.Y - 5, p.X + 10, p.Y + 5);
            ACD.DB.Draw2D(p.X + 10, p.Y - 5, p.X, p.Y + 5);
        }

        static void _drawCircleFrame(double[] ar, pPos[] bb, double[] radius)
        {
            double detail_x = bb[0].X + 1800;
            double detail_y = bb[1].Y - 1250;
            // Draw grid
            for (int i = 0; i < ar.Length - 1; i++)
                if(i < radius.Length)
                {
                    double y = ar[i];
                    ACD.DB.Draw2D(bb[0].X, bb[0].Y + y, bb[1].X, bb[0].Y + y, "LAYER=A-GRID");
                    ACD.DB.CreateText((i + 1).ToString(), new pPos(bb[0].X - 200, bb[0].Y + y + 50), 2);

                    pPos ct = new pPos(detail_x + 1000, detail_y + 1000);
                    double r = radius[i];

                    //ACD.DB.DrawCircle(ct, r);
                    //ACD.DB.DrawCircle(ct, r - 10);
                    //ACD.DB.Draw2D(ct.X - 500, ct.Y, ct.X + 500, ct.Y, "LAYER=A-GRID");
                    //ACD.DB.Draw2D(ct.X, ct.Y - 500, ct.X, ct.Y + 500, "LAYER=A-GRID");

                    //createRDimension(ACD.DB, ct, r);

                    //ACD.DB.CreateText((i + 1).ToString(), ct - new pPos(0, r + 100), 4);

                    //detail_y -= 1000;
                    //if(detail_y < bb[1].Y - 6000)
                    //{
                    //    detail_y = bb[1].Y - 1250;
                    //    detail_x += 1000;
                    //}
                }

            for (int i = 0; i < ar.Length - 1; i++)
            {
                ACD.DB.Dim2D(bb[0].X, bb[0].Y + ar[i], bb[0].X, bb[0].Y + ar[i + 1]);
            }
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                double mvx = 1500;

                if(selIds.Count > 0)
                {
                    PosCollection pls = ACD.DB._getAllVertices(selIds, 6);
                    ObjectIdCollection newIds = ACD.DB.CloneObjects(selIds);
                    ACD.DB.MoveObject(newIds, new pPos(mvx,0));

                    Dictionary<double, List<double>> dicts = new Dictionary<double, List<double>>();

                    
                    pPos[] bb = pls.Boundary;

                    List<double> ar = new List<double>();

                    for (int i = 0; i < Math.Floor((bb[1].Y - bb[0].Y) / 100); i++)
                        ar.Add(i * 100);

                    ACD.DB.Dim2D(mvx + bb[0].X, bb[0].Y, mvx + bb[1].X, bb[0].Y);
                    ACD.DB.Dim2D(mvx + bb[0].X, bb[0].Y, mvx + bb[0].X, bb[1].Y);




                    double mdx = (bb[0].X + bb[1].X) / 2;
                    ACD.DB.Draw2D(new pPos(mdx - 20, bb[0].Y).Rect(40, bb[1].Y - bb[0].Y),"c");

                    PosCollection segs = pls.GetSegment();
                    PosCollection res = new PosCollection();
                    List<double> radius = new List<double>();

                    foreach (pPos[] pts in pls)
                    {
                        ACD.DB.Draw2D(pts.Offset(-10), 'c');

                        foreach (double y in ar)
                        {
                            pPos[] ls = new pPos[] { new pPos(bb[0].X - 500, bb[0].Y + y), new pPos(bb[1].X + 500, bb[0].Y + y) };
                        
                        
                            // Offset border 10mm
                            
                            
                            var __tls = pts.Intersect(ls[0], ls[1], true);
                            if (__tls.Length > 1)
                            {
                                ACD.DB.Draw2D(__tls);
                                //ACD.DB.Draw2D(new pPos(__tls[0].X, __tls[0].Y - 5).Rect(__tls[1].X - __tls[0].X, 10), "c");

                                ACD.DB.Dim2D(__tls[0].X, __tls[0].Y, __tls[1].X, __tls[0].Y);

                                radius.Add((__tls[1].X - __tls[0].X)/2);

                                //_drawXBox(__tls[0]);
                                //_drawXBox(__tls[1] - new pPos(10,0));

                                res.Add(__tls);
                            }
                        }
                    }

                    _drawCircleFrame(ar.ToArray(), bb, radius.ToArray());


                    ACD.WR("PLS {0}, {1}", res.Count, res.AllPoints.Length);
                }
            }
        }
    }
}
