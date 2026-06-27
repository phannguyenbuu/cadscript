using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

//fileIn Cls.IReader
//fileIn Cls.Global
//fileIn Cls.MethodEntity
//fileIn Cls.IZone

//fileIn Cls.ACD
//fileIn Cls.IS
//fileIn Cls.IDraw

//fileIn Cls.IBlock
//fileIn Cls.IAnno
//fileIn Cls.IDimChain

namespace AcadScript
{
    public class StretchStructCLS
    {
        static pPos[] AnyParamsToPts(params object[] ls)
        {
            List<pPos> pts = new List<pPos>();

            pPos pt = new pPos(0, 0);
            pPos basept = new pPos(0, 0);
            int number_index = 0;

            foreach (var obj in ls)
                if (obj is string)
                {
                    string st = obj.ToString();
                    if (st.st_("$"))
                        basept = pPos.FromString(st.Substring(1).Replace(" ", ","));
                }

            //string s_number = "0123456789";

            foreach (var obj in ls)
                if (obj is pPos)
                {
                    pts.Add((pPos)obj);
                }
                else if (obj.GetType() == typeof(pPos[]))
                    pts.AddRange((pPos[])obj);
                else if (!(obj is string))
                {
                    double n = obj.ToNumber(double.NaN);

                    if (!double.IsNaN(n))
                    {
                        pt[number_index % 2] = n;

                        if (number_index % 2 == 1)
                            pts.Add(pt.Clone());

                        number_index++;
                    }
                }

            return pts.Move(basept).ToArray();
        }


        static void Strecth2D(ObjectIdCollection ids, params object[] objs)
        {
            string txt_override = "<>";
            double spacing = 0;

            foreach (var obj in objs)
                if (obj is string)
                {
                    string st = obj.ToString();

                    if (st.st_("S"))
                        spacing = st.Substring(1).ToNumber();
                    else if (st.st_("T"))
                        txt_override = st.Substring(1);
                }

            pPos[] pts = AnyParamsToPts(objs);

            
        }

        static void _strethSrc(ObjectIdCollection ids, pPos[] bb)
        {
            ObjectIdCollection lwpIds = ACD.DB.FilterIds(ids, "LWPOLYLINE");
            ObjectId bbId = lwpIds.ToList().FirstOrDefault(__id => ACD.DB._getLineworkWidth(__id) >= 1);
            
            pPos[] _src_bb = ACD.DB._getBound(ids);

            if (bbId != null)
                _src_bb = ACD.DB._getBound(bbId);

            ObjectIdCollection newIds = ACD.DB.CloneObjects(ids);

            ObjectIdCollection validLwpIds = lwpIds.ToList().Where(__id => ACD.DB.GetLinetypeText(__id).et_()).ToCollection();
            //ObjectIdCollection cmdLwpIds = lwpIds.ToList().Where(__id => !ACD.DB.GetLinetypeText(__id).et_()).Select(__id => ACD.DB._getVertices(__id)).ToCollectionSameClosed(true);

            //foreach (ObjectId lwpId in cmdLwpIds)
            {
                //ACD.DB.Stretchs(newIds, ACD.DB._getVertices(lwpId), validLwpIds);
            }
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                
                ObjectIdCollection selIds = ACD.GetSelection();

                if(selIds.Count > 0)
                {
                    ObjectIdCollection blockIds = ACD.DB.FilterIds(selIds, "INSERT");
                    ObjectIdCollection lwpIds = ACD.DB.FilterIds(selIds, "LWPOLYLINE");

                    foreach (ObjectId blockId in blockIds)
                    {
                        ACD.DB.BlockEntitiesAction(blockId, __ids =>
                        {
                        

                            foreach (ObjectId lwpId in lwpIds)
                            {

                                _strethSrc(__ids, ACD.DB._getVertices(lwpId));

                            }
                        });
                    }
                }
            }
        }
    }
}

