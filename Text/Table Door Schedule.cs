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
    public class TableDoorScheduleCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                string[] prams = ACD.DB.ListDoor(selIds);

                //DV.ViewParamData(prams);

                string content = "";

                foreach (string st in prams)
                    content += st + "\r\n";

                System.Windows.Forms.Clipboard.SetText(content);
                //ACD.DB.CreateSchedule("DOOR SCHEDULE", str, DE.DEF_TABLE_DOOR_POS);
                ACD.Focus();
            }
        }
    }
}

