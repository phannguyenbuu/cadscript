using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using APP = Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;


//using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class TextstepCLS
    {
        
        static AttachmentPoint _setTextAlignment(Database db, ObjectId id, ObjectId srcId)
        {
            AttachmentPoint res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                res = srcId.ObjectClass.DxfName == "TEXT" ?
                    ((DBText)tr.GetObject(srcId, OpenMode.ForRead)).Justify
                    : ((MText)tr.GetObject(srcId, OpenMode.ForRead)).Attachment;
                
                if (id.ObjectClass.DxfName == "TEXT")
                {
                    DBText txt = (DBText)tr.GetObject(id, OpenMode.ForWrite);
                    txt.Justify = res;
                }
                else
                {
                    MText txt = (MText)tr.GetObject(id, OpenMode.ForWrite);
                    txt.Attachment = res;
                }

                tr.Commit();
            }
            return res;
        }

        static void _setTextValue(ObjectId objId, double start)
        {
            string txt = ACD.DB._getContent(objId);
            string number = "0123456789.";

            string txt1 = "", txt2 = "";
            bool br = false;

            for (int i = 0; i < txt.Length; i++)
            {
                char ch = txt[txt.Length - 1 - i];

                if (number.Contains(ch))
                {
                    if (!br)
                        txt2 = ch + txt2;
                    else
                        txt1 = ch + txt1;
                }
                else
                {
                    br = true;
                    txt1 = ch + txt1;
                }
            }

            ACD.DB._setContent(objId, txt1 + start);
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                //stepForm frm = new stepForm();
                //frm.Show();

                
                //double start = ACD.ED.GetInputString("Enter start", "1").ToNumber();
                ACD.WR("Default step is 1, if you want to change, please change value of MAXSORT");
                int nMaxSort = System.Convert.ToInt32(APP.Application.GetSystemVariable("MAXSORT"));
                // Set system variable to new value
                //Application.GetSystemVariable("MAXSORT", 100);

                double step = 1; // ACD.ED.GetInputString("Enter step", "1").ToNumber(1);
                if (nMaxSort != 1000)
                    step = nMaxSort;

                //ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "MTEXT", "TEXT");
                ObjectIdCollection txtIds = ACD.GetSelection();
                //ObjectIdCollection hisIds = new ObjectIdCollection();
                //pPos lastpt = null;

                if (txtIds.Count > 0)
                {
                    string content = ACD.DB._getContent(txtIds.ToList().FirstOrDefault(id 
                        => ACD.DB._isText(id) || ACD.DB._isLeader(id)));

                    double count = content._getNumberInString().ToNumber();
                    int index = DE.NumericArray(0, content.Length - 1)
                        .FirstOrDefault(n => "0123456789".Contains(content[n]));

                    if(index != -1)
                    {
                        content = content.Substring(0, index);
                    }

                    //while (true)
                    {
                        //ACD.WR("OK1");
                        //ObjectIdCollection selIds = ACD.GetSelection();

                        //if (ACD.EscapePressed)
                        //    break;

                        if (txtIds.Count > 0)
                        {
                            //count += step;

                            //ObjectIdCollection newIds = ACD.DB.CloneObjects(txtIds);
                            
                            foreach (ObjectId txtId in txtIds)
                                if (ACD.DB._isText(txtId) || ACD.DB._isLeader(txtId))
                                {
                                    ACD.DB._setContent(txtId, content + count);
                                    //ACD.DB.MoveObject(txtId, pt - ACD.DB._getPoint(txtId));
                                    //_setTextAlignment(ACD.DB, txtId, txtIds.First());
                                    count += step;
                                }

                            //ObjectId objId = ObjectId.Null;

                            //try
                            //{
                            //    objId = txtIds.ToList().First(id =>
                            //    {
                            //        //pPos[] bb = ACD.DB._getBound(id);
                            //        return pt._isVeryClosed(ACD.DB._getPoint(id),5);
                            //    });
                            //}
                            //catch (System.Exception ex) { };

                            //if (!objId.IsNull)
                            //{
                            //    if (lastpt == null)
                            //    {
                            //        hisIds.Add(objId);
                            //        _setTextValue(objId, start);

                            //        start += step;
                            //    }
                            //    else
                            //    {
                            //        ObjectIdCollection ids = txtIds.ToList().Where(id
                            //            =>
                            //        {
                            //            if (!hisIds.Contains(id))
                            //            {
                            //                pPos ct = ACD.DB._getPoint(id);//.CenterPoint();
                            //                if (ct.IsBetween(pt, lastpt, 5) && ct.DistanceTo(lastpt, pt) < 8)
                            //                    return true;
                            //            }
                            //            return false;
                            //        }).OrderBy(id => ACD.DB._getPoint(id).DistanceTo(lastpt))
                            //            .ToCollection();

                            //        foreach (ObjectId id in ids)
                            //        {
                            //            hisIds.Add(id);
                            //            _setTextValue(id, start);
                            //            start += step;
                            //        }
                            //    }
                            //}
                            //else
                            //    ACD.WR("Null object!");

                            //lastpt = pt;
                        }
                        //else
                        //    break;
                    }
                }

                ACD.Focus();
            }
        }
    }
}






