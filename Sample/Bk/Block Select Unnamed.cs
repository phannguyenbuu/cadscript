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
    public class BlockSelectUnnamedCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "INSERT");

                ObjectIdCollection ids = IR.SelectedIds.Cast<ObjectId>()
                    .Where(id => ACD.DB._isDynamicBlock(id)
                    && ACD.DB._getIdName(id).StartsWith("*U")).Select(id => id).ToCollection();

                ACD.DB.EraseObjects(ids);

                ACD.Focus();
            }
        }
    }
}

