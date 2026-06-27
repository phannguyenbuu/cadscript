using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace AcadScript
{
    public class PolylineUnionCLS
    {

        static ObjectId JoinPolylines(Database db, ObjectIdCollection ids)
        {
            ObjectId res = ObjectId.Null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                Entity[] ents = ids.ToList().Select(id
                    => trans.GetObject(id, OpenMode.ForRead) as Entity).ToArray();

                for (int i = 1; i < ents.Length; i++)
                {
                    ents[0].UpgradeOpen();
                    ents[0].JoinEntity(ents[i]);

                    ents[i].UpgradeOpen();
                    ents[i].Erase();
                }
                res = ents[0].ObjectId;
                trans.Commit();
            }
            return res;
        }

        static void ProcessGroup(ObjectIdCollection selIds)
        {
            int splitarc = (int)pString.INI_String("SPLITARC").ToNumber();

            PosCollection pls = selIds.Cast<ObjectId>()
                .Select(id => ACD.DB._getVertices(id, splitarc, true, true)).ToCollectionSameClosed();

            GraphConsole.Compute(pls);

            if (GraphConsole.ResultBorders.Count == 0)
            {
                JoinPolylines(ACD.DB, selIds);
            }
            else
                for (int i = 0; i < GraphConsole.ResultBorders.Count; i++)
                {
                    pPos[] ls = GraphConsole.ResultBorders[i].Straighten(true);
                    ACD.DB.DrawPolyline(ls, true, selIds.First());
                }

            ACD.DB.EraseObjects(selIds);
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                string[] validId = new string[] { "LWPOLYLINE", "HATCH", "LINE", "AEC_WALL" };

                // Thu thập nhiều cụm lựa chọn liên tiếp.
                // - Chọn đối tượng → Enter → ghi nhận cụm, tiếp tục vòng lặp
                // - Enter ngay (không chọn gì) → kết thúc nhập, bắt đầu xử lý
                var groups = new List<ObjectIdCollection>();

                while (true)
                {
                    ObjectIdCollection sel = ACD.GetSelection()
                        .ToList()
                        .Where(id => validId.Contains(id.ObjectClass.DxfName))
                        .ToCollection();

                    if (sel == null || sel.Count == 0)
                        break;   // Enter trắng → thoát vòng lặp, xử lý

                    groups.Add(sel);
                    ACD.WR("Cụm #{0}: {1} đối tượng. Chọn tiếp hoặc Enter để xử lý.", groups.Count, sel.Count);
                }

                if (groups.Count == 0)
                {
                    ACD.WR("Không có cụm nào được chọn.");
                }
                else
                {
                    foreach (var group in groups)
                        ProcessGroup(group);

                    ACD.WR("Hoàn thành! Đã xử lý {0} cụm.", groups.Count);
                }

                ACD.Focus();
            }
        }
    }
}

