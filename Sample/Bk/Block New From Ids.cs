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
    public class BlockNewFromIdsCLS
    {
        static string laXname;
        static ObjectIdCollection selIds;
        static pPos pt;

        static bool _notInLayer(Database db, ObjectId id)
        {
            string st = db._getLayer(id).ToUpper();
            return st == laXname || st == DE.DEFPOINTS.ToUpper();
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection res = new ObjectIdCollection();
                string blockName = ACD.ED.GetInputString("Block", "Input block name");
                ACD.DB.HideAllDimension();

                if (!blockName.empty())
                {
                    //foreach (ObjectId id in selIds)
                    {
                        pPos[] bb = ACD.DB._getBound(selIds);

                        //if (bb != null)
                        {
                            //laXname = ACD.DB._getLayer(id).ToUpper();
                            //ACD.DB.GetEntities(bb, EN_SELECT.AC_ALL);

                            ObjectIdCollection objIds = ACD.DB.FilterNotIds(selIds, _notInLayer);

                            //IInfo it = new IInfo(ACD.DB);
                            //it.GetIdsParams(objIds);
                            //DV.ViewParamData(it.Params);

                            if (pt == null)
                                pt = bb[0];

                            //ObjectIdCollection txtIds = ACD.DB.GetAllTitles(bb);

                            string st = ACD.CurrentDWGFileName;

                            //if (txtIds.Count > 0 && st != null)
                            //{
                            //objIds.Remove(txtIds.First());


                            ObjectIdCollection newIds = ACD.DB.ExplodeAEC(objIds, false);
                            ACD.DB.NewBlock(newIds, blockName, true, false, pt);

                            ObjectIdCollection new_block_ids = ACD.DB.CollectBlock(blockName);

                            res.AddRange(new_block_ids);

                            if (ACD.DB.BlockCount(blockName) == 0)
                                ACD.DB.Insert(blockName, pt + new pPos(0, bb[1].X - bb[0].X));
                            //}
                        }
                    }

                    //if (ckHiddenFurn.Checked)
                    //    _hiddenFurniture(res);
                }

                ACD.Focus();
            }
        }
    }
}

