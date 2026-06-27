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
    public class DrawingCircleCLS
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

        static ObjectId CreateAlignDimension(Database db, pPos p1, pPos p2,
            double spacingX, pPos[] bounds = null)
        {
            ObjectId res = ObjectId.Null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                DimStyleTableRecord dst = tr.GetObject(db.Dimstyle, OpenMode.ForWrite) as DimStyleTableRecord;

                db.SetDimstyleData(dst);

                //double rot = Math.Abs(p1.X - p2.X) > Math.Abs(p1.Y - p2.Y) ? 0 : Math.PI / 2;
                double ang = (p1 - p2).Angle();
                pPos ext = new pPos(spacingX * Math.Sin(ang), spacingX * Math.Cos(ang));

                pPos p3 = p1 + ext;

                if (bounds != null && ((p1 + ext).Inside(bounds) || (p2 + ext).Inside(bounds)))
                    p3 = p1 - ext;
                else
                    p3 = p1 + ext;

                AlignedDimension acRotDim = new AlignedDimension(p1.ToPoint3(),
                    p2.ToPoint3(), p3.ToPoint3(), null, db.Dimstyle);

                btr.AppendEntity(acRotDim);
                tr.AddNewlyCreatedDBObject(acRotDim, true);

                res = acRotDim.ObjectId;

                db.AddCurrentAnnotative(acRotDim.ObjectId);
                //_applyDST(dst, acRotDim);

                db._setLayer(acRotDim.ObjectId, pString.INI_String("DIM_LAYER"));

                tr.Commit();
            }
            return res;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                
                ObjectIdCollection selIds = ACD.GetSelection();
                ObjectIdCollection blockIds = selIds.FilterIds("INSERT");

                pPos[] pointIds = blockIds.ToList().Select(id =>
                    {
                        pPos p = ACD.DB._getPoint(id);
                        ACD.DB._setRotation(ACD.DB.Insert("P.Lokhoet", p), ACD.DB._getRotation(id));
                        return p;
                    }).ToArray();


                foreach(ObjectId id in selIds)
                {
                    if (ACD.DB._isVertice(id))
                    {
                        pPos[] verts = ACD.DB._getVertices(id);
                        PosCollection pls = verts.GetSegment().OrderBy(l => -l.Length(false)).ToCollectionSameClosed(false);
                        pPos[] ls = pls.First();

                        //DRAW CIRLCE
                        //ACD.DB.DrawCircle(ls.CenterPoint(), ls.Length(false) / 2);

                        //DRAW DIMENSION
                        for (int i = 0; i < verts.Length; i++)
                        {
                            pPos[] pts = pointIds.Where(pt => pt.Inside(verts)).ToArray();

                            pPos p1 = verts[i], p2 = verts[(i + 1) % verts.Length];
                            

                            List<pPos> midpts = new List<pPos>() { p1 };
                            midpts.AddRange(pts.Select(p => p.ProjectLine(p1, p2))
                                .Where(p => p.IsBetween(p1,p2) && !p._isVeryClosed(p1) && !p._isVeryClosed(p2))
                                .OrderBy(p => p.DistanceTo(p1)).ToArray());
                            midpts.Add(p2);


                            for (int j = 0; j < midpts.Count - 1; j++)
                                if(!midpts[j]._isVeryClosed(midpts[j+1]))
                                    CreateAlignDimension(ACD.DB, midpts[j], midpts[j + 1], 500, verts);

                            CreateAlignDimension(ACD.DB, p1, p2, 500 * 2, verts);
                        }
                    }else if(ACD.DB._isCircle(id))
                    {
                        double r = ACD.DB._getRadius(id);
                        pPos p = ACD.DB._getPoint(id);
                        pPos ext = new pPos(r * Math.Sin(10), r * Math.Cos(10));
                        CreateAlignDimension(ACD.DB, p - ext, p + ext, 0);
                    }
                }

                ACD.DB.EraseObjects(blockIds);
                ACD.Focus();
            }
        }
    }
}