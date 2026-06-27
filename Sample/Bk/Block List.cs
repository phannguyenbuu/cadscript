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
    public class BlockListCLS
    {
        
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                string[] ls = ACD.DB.ListBlock().OrderBy(s => s).ToArray();
                ACD.WR("List:\r\n{0}", ls.ToTextStr());
                string res = "";

                foreach (string s in ls)
                    res += s + "=\r\n";

                Clipboard.SetText(res);
            }
        }
    }
}

