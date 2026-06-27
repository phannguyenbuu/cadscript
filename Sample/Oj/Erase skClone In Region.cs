using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

//fileIn Cls.IReader
//fileIn Cls.Global
//fileIn Cls.MethodEntity
//fileIn Cls.IZone

//fileIn Cls.ACD
//fileIn Cls.IS
//fileIn Cls.IDraw

//fileIn Cls.IAnno
//fileIn Cls.IBlock
//fileIn Cls.AnnotativeTool
//fileIn Cls.IDimChain

namespace AcadScript
{
    public class HatchEraseInRegionCLS
    {
        pPos _getDimTextPos(ObjectId id)
        {
            pPos res = null;
            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);

                if (ent is AlignedDimension)
                {
                    AlignedDimension dim = (AlignedDimension)ent;
                    res = dim.TextPosition.ToPos();
                }
                
                tr.Commit();
            }

            return res;
        }

        static bool _isInside(pPos[] region, ObjectId id)
        {
            pPos pt = null;

            if (ACD.DB._isText(id))
            {
                pt = ACD.DB._getPoint(id);
            }else if (ACD.DB._isDim(id))
            {
                pt = ACD.DB._getDimTextPos(id);
            }

            //ACD.WR("PT {0}", pt);

            return pt.Inside(region);
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.DB.GetEntities(null, EN_SELECT.AC_ALL);
                ACD.DB.EraseObjects(IR.SelectedIds.ToList().Where(objId 
                    => ACD.DB.GetXNotes(objId)._props("skClone").ToBool()).ToCollection());
            }

            ACD.Focus();
        }
    }
}

