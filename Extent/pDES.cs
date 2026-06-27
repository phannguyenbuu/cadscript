using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.IO;
using AcadScript;
namespace AcadScript
{
    public static class pPosExtension
    {

        public static pPos RotationValue(this Matrix3d mat)
        {
            double sinPitch, cosPitch, sinRoll, cosRoll, sinXaw, cosXaw;

            sinPitch = -mat[2, 0];
            cosPitch = Math.Sqrt(1 - sinPitch * sinPitch);

            if (Math.Abs(cosPitch) > double.Epsilon)
            {
                sinRoll = mat[2, 1] / cosPitch;
                cosRoll = mat[2, 2] / cosPitch;
                sinXaw = mat[1, 0] / cosPitch;
                cosXaw = mat[0, 0] / cosPitch;
            }
            else
            {
                sinRoll = -mat[1, 2];
                cosRoll = mat[1, 1];
                sinXaw = 0;
                cosXaw = 1;
            }

            return new pPos(Math.Atan2(sinXaw, cosXaw),
                Math.Atan2(sinPitch, cosPitch), Math.Atan2(sinRoll, cosRoll));
        }

        public static pPos Transform(this pPos pt, double[] tm)
        {
            pPos rot = new Matrix3d(tm).RotationValue();
            Point3d basept = new Point3d(0, 0, 0);

            Matrix3d rotationX = Matrix3d.Rotation(rot.X, Vector3d.XAxis, basept);
            Matrix3d rotationY = Matrix3d.Rotation(rot.Y, Vector3d.YAxis, basept);
            Matrix3d rotationZ = Matrix3d.Rotation(rot.Z, Vector3d.ZAxis, basept);

            return pt.ToPoint3().TransformBy(Matrix3d.Identity.PreMultiplyBy(rotationZ)
                .PreMultiplyBy(rotationX).PreMultiplyBy(rotationX)).ToPos();
        }

        public static pPos Transform(this pPos pt, pPos movept, double rot, double scale, pPos basept = null)
        {
            return pt.ToPoint3().TransformBy(TransformMat(movept, rot, scale, basept)).ToPos();
        }

        public static Matrix3d TransformMat(pPos movept, double rot, double scale, pPos basept = null)
        {
            if (basept == null)
                basept = new pPos(0, 0, 0);

            Matrix3d moveMat = Matrix3d.Displacement(new Vector3d(movept.X, movept.Y, movept.Z));
            Matrix3d rotationMat = Matrix3d.Rotation(Math.PI * rot / 180, Vector3d.ZAxis, basept.ToPoint3());
            Matrix3d scaleMat = Matrix3d.Scaling(scale, basept.ToPoint3());
            return scaleMat.PreMultiplyBy(rotationMat).PreMultiplyBy(moveMat);
        }

        public static Matrix3d MirrorMat(this pPos pt1, pPos pt2)
        {
            return Matrix3d.Mirroring(new Line3d(pt1.ToPoint3(), pt2.ToPoint3()));
        }

        public static Matrix3d MirrorMat(this pPos[] pts)
        {
            return Matrix3d.Mirroring(new Line3d(pts[0].ToPoint3(), pts[1].ToPoint3()));
        }

        public static pPos[] Mirror(pPos[] pts, pPos pt1, pPos pt2)
        {
            Matrix3d mirrMat = Matrix3d.Mirroring(new Line3d(pt1.ToPoint3(), pt2.ToPoint3()));

            pPos[] res = new pPos[pts.Length];
            for (int i = 0; i < pts.Length; i++)
                res[i] = pts[i].ToPoint3().TransformBy(mirrMat).ToPos();
            return res;
        }

        public static pPos[] Transform(this IEnumerable<pPos> pts, pPos movept,
            double rot, double scale, pPos basept = null)
        {
            pPos[] res = new pPos[pts.Count()];
            for (int i = 0; i < pts.Count(); i++)
                res[i] = pts.ElementAt(i).Transform(movept, rot, scale, basept);
            return res;
        }

        public static pPos[] Transform(this IEnumerable<pPos> pts, double[] tm)
        {
            return pts.Select(p => p.Transform(tm)).ToArray();
        }


        public static pPos[] Transform(this IEnumerable<pPos> pts, Matrix3d tm)
        {
            return pts.Select(p => p.ToPoint3().TransformBy(tm).ToPos()).ToArray();
        }

        /*public static Matrix3d _posibleMatrix(this Matrix3d base_mat, ROOM_CLS itm1, ROOM_CLS itm2)
        {
            pPos tmp = new pPos(double.PositiveInfinitX, double.PositiveInfinitX);
            Matrix3d res = base_mat.PreMultiplyBy(Matrix3d.Identity);

            for (int i = 0; i < 4; i++)
            {
                switch (i)
                {
                    case 0:
                        break;
                    case 1:
                        base_mat = base_mat.PreMultiplyBy(MirrorMat(itm2.StartOpening, itm2.EndOpening));
                        break;
                    case 2:
                        base_mat = base_mat.PreMultiplyBy(MirrorMat(itm2.CenterOpeningLine));
                        break;
                    case 3:
                        base_mat = base_mat.PreMultiplyBy(MirrorMat(itm2.StartOpening, itm2.EndOpening));
                        break;
                }
                pPos[] ls = itm1.Pts.Transform(base_mat);
                pPos bb = ls._isNearBounding(itm2.Pts);

                if (tmp.X > bb.X && tmp.X > bb.X)
                {
                    tmp = bb;
                    res = base_mat.PreMultiplyBy(Matrix3d.Identity);
                }
            }
            return res;
        }*/
    }

    public static class pDES
    {

        

        //public static string CAD_TEMPLATE_DIR = @"D:\Dropbox\CADLib\";
        public static string CAD_TEMPLATE_DIR_TEMP = @"D:\Temp\";
        //public static string CAD_TEMPLATE_FILE = CAD_TEMPLATE_DIR + "Template.dwg";
        //public static string CAD_TEMPLATE_ROOM_FILE = CAD_TEMPLATE_DIR + "Template_Room.dwg";
        //public static string CAD_TEMPLATE_LAYOUT = CAD_TEMPLATE_DIR + "Template_Layout.dwg";
        //public static string CAD_TEMPLATE_PREVIEW = CAD_TEMPLATE_DIR + @"_preview\";

        //public static string CADLIB_ELEMENT_DESC = CADLIB_ELEMENT + @"Description.xlsx";
        //public static string CADLIB_ELEMENT_DESC_LANDSCAPE = "Lands";
        //public static string CADLIB_ELEMENT_DESC_CEILING = "Ceiling";
        //public static string CADLIB_ELEMENT_LEGEND_COTEBLOCK = "G_Cote";

        //public static string CADLIB_ELEMENT_LEGEND = CADLIB + @"Elements\Legend.dwg";
        //public static string CADLIB_ELEMENT_FURNITURE = CADLIB + @"Elements\Furniture.dwg";

        //public static string CADLIB_SAMPLES = CADLIB + @"Samples\";
        //public static string CADLIB_PDF = CADLIB + @"PDF\";
        //public static string CADLIB_SAMPLES_ROOM = CADLIB_SAMPLES + @"Room\";
        //public static string CADLIB_SAMPLES_BLOCK = CADLIB_SAMPLES + @"Block\";
        //public static string CADLIB_SAMPLES_DOOR = CADLIB_SAMPLES + @"Door\";
        //public static string CADLIB_SAMPLES_WALL = CADLIB_SAMPLES + @"Wall\";
        //public static string CADLIB_SAMPLES_DETAIL = CADLIB_SAMPLES + @"Detail\";


        public static void EraseObjects(this Database db, ObjectIdCollection ids)
        {
            foreach (ObjectId id in ids)
                db.EraseObject(id);
        }


        public static void AddRange(this ObjectIdCollection ids, ObjectIdCollection ext_ids)
        {
            foreach (ObjectId id in ext_ids) ids.Add(id);
        }

        public static void EraseObject(this Database db, ObjectId id)
        {
            //if (db.ValidId(id))
            //{
            if(id.ObjectClass.DxfName != "ACDB_TEXTOBJECTCONTEXTDATA_CLASS" 
                && id.ObjectClass.DxfName != "DICTIONARY")
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    DBObject ent = (DBObject)tr.GetObject(id, OpenMode.ForWrite);
                    if (ent != null && !ent.IsErased)
                        ent.Erase();

                    tr.Commit();
                }
            }
            //}
        }

        public static ObjectId[] ToArray(this ObjectIdCollection ids)
        {
            List<ObjectId> res = new List<ObjectId>();
            foreach (ObjectId id in ids)
                res.Add(id);
            return res.ToArray();
        }

        public static ObjectId Last(this ObjectIdCollection ids)
        {
            return ids[ids.Count - 1];
        }

        public static ObjectId First(this ObjectIdCollection ids)
        {
            return ids[0];
        }

        public static List<ObjectId> ToList(this ObjectIdCollection ids)
        {
            return ids.ToArray().ToList();
        }

        public static int FindIndex(this ObjectIdCollection ids, Func<ObjectId, bool> fn)
        {
            return ids.FindIndex(id => fn(id));
        }

        public static bool Contains(this ObjectIdCollection ids, Func<ObjectId, bool> fn)
        {
            return ids.FindIndex(id => fn(id)) != -1;
        }



        public static Point2dCollection ToPoint2dCollection(this Database db, ObjectId id, pPos[] pts)
        {
            Point2dCollection res = new Point2dCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference bref = tr.GetObject(id, OpenMode.ForWrite) as BlockReference;

                foreach (pPos pt in pts)
                {
                    Point3d pt3d = pt.ToPoint3().TransformBy(bref.BlockTransform.Inverse());
                    res.Add(new Point2d(pt3d.X, pt3d.Y));
                }

                tr.Commit();
            }

            //WR("Calc Region {0} Col {1}", pts.Length, res.Count);
            return res;
        }




        public static List<T> CreateList<T>(int capacity)
        {
            List<T> res = Enumerable.Repeat(default(T), capacity).ToList();
            //for (int i = 0; i < capacity; i++) res[i] = default(T);
            return res;
        }

        public static pPos ToPos2d(this Point2d pt)
        {
            return new pPos(pt.X, pt.Y);
        }

        public static pPos ToPos(this Point3d pt)
        {
            return new pPos(pt.X, pt.Y, pt.Z);
        }

        public static pPos ToPosScale(this Scale3d pt)
        {
            return new pPos(pt.X, pt.Y);
        }

        public static Point3d ToPoint3(this pPos pt)
        {
            //db.WR("Point {0}", pt);
            return new Point3d(pt.X, pt.Y, pt.Z);
        }

        public static Point2d ToPoint2(this pPos pt)
        {
            return new Point2d(pt.X, pt.Y);
        }


        public static ObjectIdCollection ToCollection(this IEnumerable<ObjectId> ids)
        {
            ObjectIdCollection res = new ObjectIdCollection();

            if (ids != null && ids.Count() > 0)
                foreach (ObjectId id in ids)
                    res.Add(id);

            return res;
        }


        public static bool _isVisible(this Database db, ObjectId objId)
        {
            bool res = true;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                res = ent.Visible;
                tr.Commit();
            }
            return res;
        }

        public static bool ValidId(this Database db, ObjectId id)
        {
            bool res = !id.IsNull && !id.IsErased && !id.IsEffectivelyErased
                && id.IsValid && !id.IsEffectivelyErased && db._isVisible(id);

            return res;
        }
    }
}
