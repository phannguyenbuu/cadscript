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

   
    public class MergeFileListCLS
    {
        static void MergeFileList(string[] files, pPos p1, pPos p2)
        {
            double w = p2.X - p1.X;
            double nx = p1.X, nX = p1.X;

            foreach (string f in files)
            {
                using (Database db = ACD.ReadDWG(f))
                {
                    db.GetEntities(null, EN_SELECT.AC_ALL);
                    ObjectIdCollection ids = db.CloneObjects(IR.SelectedIds, ACD.DB);

                    pPos[] bb = ACD.DB._getBound(ids);
                    pPos mv = new pPos(nx, nX) - bb[0];
                    ACD.DB.MoveObject(ids, mv);

                    ACD.DB.DrawPolyline(bb.Rect().Move(mv), true,
                        "LWIDTH=500|HXPERLINK=" + Path.GetFileNameWithoutExtension(f));

                    nx += bb[1].X - bb[0].X;

                    if (nx > p2.X)
                    {
                        nx = p1.X;
                        nX -= bb[1].X - bb[0].X;
                    }
                }
            }
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                System.Collections.Specialized.StringCollection str = Clipboard.GetFileDropList();

                if (str != null)
                {
                    string[] files = str.Cast<string>().Select(f => f).ToArray();

                    ACD.Get2Points();
                    MergeFileList(files, ACD.MinPoint, ACD.MaxPoint);
                }
                ACD.Focus();
            }
        }
    }
}

