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
    public class StructColumnDetailCLS
    {
        public static string main_prams = "</PARAMS>";
        
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                string block_grid_name = main_prams._prop("Block");

                ObjectId block_grid_Id = ACD.DB.HasBlock(block_grid_name);
                if(!block_grid_Id.IsNull)
                {
                    ObjectIdCollection subIds = ACD.DB.GetEntInBlockByName(block_grid_name);
                    string[] hatchInfors = subIds.ToList().Where(id => ACD.DB._isHatch(id))
                        .Select(id => ACD.DB._getIdInfo(id)).Distinct().ToArray();

                    Dictionary<string, PosCollection> dicts = subIds.ToList().Where(id => ACD.DB._isHatch(id))
                        .Select(id => ACD.DB._getIdInfo(id)).Distinct()
                        .ToDictionary(s => s, s=> new PosCollection());
                    //PosCollection pls = new PosCollection();
                    //foreach(string hInfo in hatchInfors)

                    foreach (ObjectId objId in subIds)
                        if(ACD.DB._isHatch(objId))
                        {
                            string hInfo = ACD.DB._getIdInfo(objId);

                            if (dicts.ContainsKey(hInfo))
                            {
                                PosCollection hpls = dicts[hInfo];
                                hpls.AddRange(ACD.DB._getHatch(objId));
                            }
                        }

                    //ACD.DB.EraseObject(block_grid_Id);
                }
            }
        }
    }
}

