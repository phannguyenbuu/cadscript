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
                ObjectIdCollection selIds = ACD.GetSelection();
                ObjectIdCollection resIds = new ObjectIdCollection();
                
                ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "DIMENSION", "MTEXT", "TEXT");

                ObjectIdCollection validIds = IR.SelectedIds.ToList().ToCollection();

                //ACD.WR("IDS {0}", validIds.Count);

                foreach (ObjectId objId in selIds)
                    if(ACD.DB._isHatch(objId))
                    {
                        PosCollection pls = ACD.DB._getHatch(objId);
                        GraphConsole.Compute(pls);
                        //ACD.WR("Hatch {0}", pls.ToText(";"));
                        foreach (pPos[] pts in GraphConsole.ResultBorders)
                        {
                            ObjectIdCollection ids = validIds.ToList().Where(id
                                => _isInside(pts.Offset(1), id)).ToCollection();
                            resIds.AddRange(ids);
                        }
                        //ACD.WR("OK1.2");
                    }
                //ACD.WR("E_IDS {0}", resIds.ToList().Distinct().ToCollection().Count);
                ACD.DB.EraseObjects(resIds.ToList().Distinct().ToCollection());
                //ACD.WR("OK3");
            }

            ACD.Focus();
        }
    }
}

