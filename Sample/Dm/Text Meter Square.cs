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
    public class MeterSquareCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection()._filterDXF("MTEXT", "TEXT");
                Database db = ACD.DB;

                Dictionary<string, double> dicts = new Dictionary<string, double>();

                double total = 0;

                foreach (ObjectId txtId in selIds)
                    if (ACD.DB._getContent(txtId).Upper().EndsWith("M2"))
                    {
                        ObjectIdCollection ids = selIds.Cast<ObjectId>()
                            .Where(id => id != txtId && !ACD.DB._getContent(id).Upper().EndsWith("M2"))
                            .Select(id => id).OrderBy(id =>
                            ACD.DB._getPoint(id).DistanceTo(ACD.DB._getPoint(txtId))).ToCollection();

                        if (ids.Count > 0)
                        {
                            string STYLE = ACD.DB._getContent(ids.First());
                            int index = dicts.Keys.ToList().FindIndex(s => s == STYLE);

                            string st = ACD.DB._getContent(txtId).Upper().Replace("M2", "");


                            double n = st.ToNumber();
                            //ACD.WR("ST {0} value {1}", st, n);


                            if (index == -1)
                            {
                                dicts.Add(STYLE, n);
                            }
                            else
                                dicts[STYLE] += n;
                        }
                    }
                    else
                    {
                        total += ACD.DB._getContent(txtId).ToNumber();
                    }

                string content = "";

                foreach (var itm in dicts)
                {
                    content += String.Format("{0} with {1} m2\r\n", itm.Key, itm.Value);
                }


                content = "\r\n" + total;

                ACD.WR(content);
                System.Windows.Forms.Clipboard.SetText(content);
            }

            ACD.Focus();
        }
    }
}

