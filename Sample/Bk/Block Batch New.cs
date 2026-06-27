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
    public class BlockBatchNewCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                List<ObjectIdCollection> allIds = new List<ObjectIdCollection>();
                List<ObjectIdCollection> dimIds = new List<ObjectIdCollection>();

                while (true)
                {
                    ObjectIdCollection selIds = ACD.GetSelection();

                    if (selIds.Count == 0)
                        break;
                    else
                    {
                        allIds.Add(selIds);//.Cast<ObjectId>().Where(id => !ACD.DB._isDim(id)).ToCollection());
                        //dimIds.Add(selIds.Cast<ObjectId>().Where(id => ACD.DB._isDim(id)).ToCollection());
                    }
                }

                ACD.WR("Select block names ...");
                string[] blocknames = ACD.GetSelection().Cast<ObjectId>().Select(id => ACD.DB._getContent(id)).ToArray();

                if (allIds.Count > 0)
                    for (int i = 0; i < allIds.Count; i++)
                    {
                        string blockname = ACD.DB.uniqueBlockName(blocknames.Length > i ? blocknames[i] : "NewBlock");
                        pPos pt = ACD.DB._getBound(allIds[i])[0];
                        ACD.DB.NewBlock(allIds[i], blockname, true, false, pt);
                        ObjectId blockId = ACD.DB.Insert(blockname, pt);
                        //ACD.DB.SetEntInBlock(blockId, dimIds[i], true);
                    }
            }
            ACD.Focus();
        }
    }
}

