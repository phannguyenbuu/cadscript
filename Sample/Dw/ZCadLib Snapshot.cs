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
    public class SnapshotCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection ids = ACD.GetSelection();

                string jpegfile = @"D:\Temp\"
                    + Path.GetFileNameWithoutExtension(ACD.CurrentDWGFileName) + "_"
                    + Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".jpg";

                ids.Snapshot(jpegfile);
                System.Windows.Forms.Clipboard.SetText(jpegfile);
            }
            ACD.Focus();
        }
    }
}

