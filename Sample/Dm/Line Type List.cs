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
    public class LineTypeListCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.DB.GetEntities(null, EN_SELECT.AC_ALL);

                var ls = IR.SelectedIds.ToList().Select(__id 
                    => ACD.DB.GetLinetypeText(__id).Upper()).Distinct().OrderBy(__s => __s).ToArray();

                string st = "";

                foreach (string __st in ls)
                    st += "@" + __st + ",";

                Clipboard.SetText(st);

                ACD.Focus();
            }
        }
    }
}

