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
    class Face3D
    {
        public string Name;
        public pPos[] Verts;
        public List<int[]> Indexes;

        public Face3D(string data)
        {
            Verts = new PosCollection(data)[0];

            string content = Verts[0].Content;
            Indexes = content.filter("()").Select(s 
                => s.filter(",").Select(ss => (int)ss.ToNumber()).ToArray()).ToList();
        }

        public PosCollection Faces
        {
            get
            {
                return Indexes != null && Verts != null ? Indexes.Select(ls 
                    => ls.Where(n => n < Verts.Length).Select(n 
                    => Verts[n]).ToArray()).ToCollectionSameClosed() : null;
            }
        }

        bool IsRectangle(IEnumerable<pPos> pts)
        {
            return pts.Count() == 4
                && Math.Abs(pts.ElementAt(0).DistanceTo(pts.ElementAt(1)) - pts.ElementAt(2).DistanceTo(pts.ElementAt(3))) < 1
                && Math.Abs(pts.ElementAt(0).DistanceTo(pts.ElementAt(3)) - pts.ElementAt(1).DistanceTo(pts.ElementAt(2))) < 1
                && Math.Abs(pts.ElementAt(0).DistanceTo(pts.ElementAt(2)) - pts.ElementAt(1).DistanceTo(pts.ElementAt(3))) < 1;
        }

        public bool IsBox
        {
            get
            {
                bool b = false;

                if(Indexes.Count == 6 && Verts.Length >= 8)
                {
                    pPos[] bb = Verts.Boundary();

                    PosCollection faces = Faces;
                    //faces = faces.OrderBy(f => f.Area()).Reverse().ToCollection();

                    //b = Math.Abs(faces[0].Area() - faces[1].Area()) < 1
                    //    || Math.Abs(faces[2].Area() - faces[3].Area()) < 1
                    //    || Math.Abs(faces[4].Area() - faces[5].Area()) < 1;

                    //if (b)
                    //{
                        b = faces.All(f => IsRectangle(f));
                    //}

                    
                }

                return b;
            }
        }
    }

    public class Draw3DViewCLS
    {
        static double angle = 25 * Math.PI / 180;
        static List<Face3D> faces;

        static void _importFaceData(string[] datas)
        {
            faces = new List<AcadScript.Face3D>();
            //string[] lines = data.filter("\r\n");

            //(0,2,3,1)(4,5,7,6)(0,1,5,4)(1,3,7,5)(3,2,6,7)(2,0,4,6)

            foreach (string line in datas)
            {
                if(line.StartsWith("#FACE"))
                {
                    faces.Add(new Face3D(line._firstProp()));

                }
            }

            ACD.WR("Elements {0}", faces.Count);

            for (int i = 0; i < faces.Count; i++)
            {
                if(!faces[i].IsBox)
                {
                    ACD.WR("Nobox {0}", i);
                }
            }
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                string[] datas = File.ReadAllLines(@"D:\Dropbox\FACE.txt");
                _importFaceData(datas);
                ACD.Focus();
            }
        }
    }
}