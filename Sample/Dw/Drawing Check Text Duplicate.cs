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



    public class DrawingCheckTextDuplicateCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "MTEXT", "TEXT");

                string[] contents = IR.SelectedIds.ToList().Select(id 
                    => ACD.DB._getContent(id)).Distinct().OrderBy(s => s).ToArray();

                foreach(string content in contents)
                {
                    ObjectIdCollection ids = IR.SelectedIds.ToList().Where(id
                        => ACD.DB._getContent(id) == content).OrderBy(id => ACD.DB._getPoint(id).X.roundNumber())
                        .ThenBy(id => ACD.DB._getPoint(id).Y.roundNumber()).ToCollection();

                    string[] ar = ids.ToList().Select(id => ACD.DB._getPoint(id).X.roundNumber()
                        + ";" + ACD.DB._getPoint(id).Y.roundNumber()).Distinct().ToArray();

                    ACD.WR("{0}:{1}", content, ar.Length);

                    foreach(string s in ar)
                    {
                        ObjectIdCollection sels = ids.ToList().Where(id 
                            => s == ACD.DB._getPoint(id).X.roundNumber()
                            + ";" + ACD.DB._getPoint(id).Y.roundNumber()).ToCollection();

                        if(sels.Count > 1)
                        {
                            ACD.DB.DrawCircle(ACD.DB._getBound(sels).CenterPoint(), 500);
                        }
                    }
                }
            }

            ACD.Focus();
        }
    }
}

