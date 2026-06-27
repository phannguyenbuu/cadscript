using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AcadScript
{
    public class TableHeader
    {
        public string Name, Key;
        public double ColumnWidth;
        public CellAlignment Align = CellAlignment.MiddleLeft;
        public int Color;

        public TableHeader(string _key, double _col = 1200, string _align = "C")
        {
            this.Name = this.Key = _key.Upper();
            this.ColumnWidth = _col;

            //int index = Array.FindIndex(ITable.TEMPLATE_HEADER, key => key._prop("KEY") == this.Key);
            //if (index != -1)
            //{
            //    string st = ITable.TEMPLATE_HEADER[index];
            //    this.Name = st._prop("NAME");
            //    this.ColumnWidth = this.Name == "TITLE" || this.Name == "CONTENT" ? 
            //            DE.DEF_TABLE_COLUMNWIDTH * 5 : st._prop("WIDTH").ToNumber(DE.DEF_TABLE_COLUMNWIDTH);

            //    st = st._prop("ALIGN");
            //    this.Align = st == "L" ? CellAlignment.MiddleLeft
            //        : (st == "R" ? CellAlignment.MiddleRight : CellAlignment.MiddleCenter);
            //}
        }

        public static TableHeader[] ImportText(IEnumerable<string> txt, string _includeHeaders = null, 
            string _excludeHeaders = null, bool has_quatity = false)
        {
            List<string> headers = !_includeHeaders.empty() ? _includeHeaders.filter().ToList() : new List<string> { "NO" };

            foreach (string st in txt)
                headers.AddRange(st.filter("|").Cast<string>()
                    .Where(word => word.Contains("="))
                    .Select(word => word.filter("=")[0].Upper())
                    .Where(word => !headers.Contains(word) 
                    && (_excludeHeaders == null || !_excludeHeaders.Contains(word)))
                    .Select(word => word).ToArray());

            if (has_quatity) headers.AddRange(new string[] { "QA" });

            return headers.Cast<string>().Select(st => new TableHeader(st)).ToArray();
        }
    }

    public static class ITable
    {
        public static string[] TEMPLATE_HEADER, TEMPLATE_ELEMENT, TEMPLATE_DRAWING, DRAWING_LIST;

        public static string _findInElements(string sName)
        {
            if (TEMPLATE_ELEMENT == null)
                return null;

            if (sName.empty())
                return null;
            sName = sName.Upper();
            int index = Array.FindIndex(TEMPLATE_ELEMENT,
                st => st._prop("KEY").filter("_").All(word => sName.Contains(word.Upper())));
            return index == -1 ? null : TEMPLATE_ELEMENT[index];
        }
        
        //public static string _findLAValue(this ROOM_CLS rm)
        //{
        //    return _findLAValue(rm.Content._prop("KEY"));
        //}
        
        public static string _findLAValue(string sName)
        {
            string st = _findInElements(sName);
            return !st.empty() ? st._prop("LA") : null;
        }

        //public static string _findElementByLA(string la_key)
        //{
        //    if (TEMPLATE_ELEMENT == null)
        //        return null;

        //    int index = Array.FindIndex(TEMPLATE_ELEMENT, 
        //        st => st._prop("LA").ChainPrefix() == la_key.ChainPrefix() 
        //        && st._prop("LA").ChainMiddle() == la_key.ChainMiddle());
        //    return index == -1 ? "" : TEMPLATE_ELEMENT[index];
        //}

        public static ObjectIdCollection CreateTable(this Database db, string _title, IEnumerable<string> str, bool with_pref = false)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            string[] ref_contents = new string[str.Count()];
            
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                Table tb = new Table();

                TableHeader[] headers = with_pref ?
                    TableHeader.ImportText(str, "NO,NA,CONTENT","KEY,ID,IDS", true)
                    : TableHeader.ImportText(str, "NO,LA,KEY,IDS");

                tb.NumRows = str.Count() + 2;
                tb.NumColumns = headers.Length;
                tb.HorizontalCellMargin = 50;
                tb.VerticalCellMargin = 0;

                tb.SetRowHeight(DE.DEF_TABLE_ROWHEIGHT);

                //db.WR("Rows {0} Columns {1}", tb.NumRows, tb.NumColumns);

                tb.BreakEnabled = true;
                tb.SetBreakSpacing(5);
                tb.SetBreakHeight(0, DE.DEF_TABLE_ROWBREAK * DE.DEF_TABLE_ROWHEIGHT);

                //db.WR("TABLE_CREATOR\nColumns = {0}, Rows = {1}\n", tb.Columns.Count, tb.Rows.Count);

                SetCellContent(tb, 0, 0, DE.DEF_TABLE_ROWHEIGHT * 2 / 3, _title, CellAlignment.MiddleCenter);

                for (int i = 0; i < headers.Length; i++)
                {
                    tb.Columns[i].Width = headers[i].ColumnWidth;
                    SetCellContent(tb, 1, i, DE.DEF_TABLE_ROWHEIGHT / 2, headers[i].Name, CellAlignment.MiddleCenter);
                }

                string current_na = "";
                int current_index = 0;

                //ScheduleIds = new ObjectIdCollection[str.Length];

                for (int i = 0; i < str.Count(); i++)
                {
                    string line = str.ElementAt(i);
                    string key = line._prop("KEY");

                    string result_content = _findInElements(key);
                    //db.WR("KEY = {0}", key);
                    ref_contents[i] = "|LA=" + result_content._prop("LA")
                                    //+ "|OBJ=" + line._prop("OBJ")
                                    + "|KEY=" + key;

                    //db.WR("LA {0}", result_content._prop("LA"));

                    SetCellContent(tb, i + 2, 0, DE.DEF_TABLE_ROWHEIGHT / 2, (i + 1).ToString(), headers[0].Align);

                    for (int j = 1; j < headers.Length; j++)
                    {
                        string content = "--";

                        switch (headers[j].Key)
                        {
                            case "NA":
                                content = line._prop("KEY");
                                if (!content.empty() && content.Length >= 2)
                                {
                                    content = content.Substring(0, content.Contains(".") ? content.IndexOf(".") + 1 : 2);

                                    if (content != current_na)
                                    {
                                        current_na = content;
                                        current_index = 0;
                                    }

                                    current_index++;
                                    content += current_index.ToString();
                                }
                                break;

                            case "QA":
                                string st = line._prop("IDS");
                                if (!st.empty())
                                {
                                    content = st.filter(",").Length.ToString();
                                    ref_contents[i] += "|IDS=" + st;
                                }
                                break;
                            case "CONTENT":
                                content = line._prop("CONTENT");
                                if(content.empty() && result_content != null)
                                    content = result_content._prop("CONTENT");
                                break;
                            case "DESCRIPTION":
                                if (result_content != null)
                                    content = result_content._prop("DESCRIPTION");
                                break;
                            default:
                                content = line._prop(headers[j].Key);
                                break;
                        }

                        if(!content.empty())
                            SetCellContent(tb, i + 2, j, DE.DEF_TABLE_ROWHEIGHT / 2, content.UpperWords(), headers[j].Align);
                    }
                }

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                res.Add(btr.AppendEntity(tb));
                db._setLayer(res, DE.DEF_LAYER_TEXT);
                tr.AddNewlyCreatedDBObject(tb, true);

                tb.TableStyle = db.Tablestyle;
                tb.GenerateLayout();

                tr.Commit();
            }

            if (with_pref)
            {
                pPos[] bb = db._getBound(res);
                res.AddRange(db.CreateTable(_title + "_PREF", ref_contents));
                db.MoveObject(res.Last(), new pPos(bb[1].X - bb[0].X + 500, 0));
                db._setLayer(res.Last(), DE.DEFPOINTS);
            }

            return res;
        }

        public static ObjectId CreateTable(this Database db, string title, 
            IEnumerable<string> columns, IEnumerable<string> str)
        {
            ObjectId res = ObjectId.Null;
            
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                Table tb = new Table();
                
                tb.NumRows = str.Count() + 2;
                tb.NumColumns = columns.Count();
                tb.HorizontalCellMargin = 50;
                tb.VerticalCellMargin = 0;

                tb.SetRowHeight(DE.DEF_TABLE_ROWHEIGHT);
                
                tb.BreakEnabled = true;
                tb.SetBreakSpacing(5);
                tb.SetBreakHeight(0, DE.DEF_TABLE_ROWBREAK * DE.DEF_TABLE_ROWHEIGHT);

                SetCellContent(tb, 0, 0, DE.DEF_TABLE_ROWHEIGHT * 2 / 3, title, CellAlignment.MiddleCenter);

                for (int i = 0; i < columns.Count(); i++)
                {
                    tb.Columns[i].Width = columns.ElementAt(i).filter(":").Last().ToNumber(100);

                    SetCellContent(tb, 1, i, DE.DEF_TABLE_ROWHEIGHT / 2,
                        columns.ElementAt(i).st_("#C") ?
                        columns.ElementAt(i).filter(":").First().Substring(2)
                        : columns.ElementAt(i).filter(":").First(), CellAlignment.MiddleCenter);
                }

                for (int i = 0; i < str.Count(); i++)
                {
                    string[] contents = str.ElementAt(i).filter(";");
                    //SetCellContent(tb, i + 2, 0, DE.DEF_TABLE_ROWHEIGHT / 2, (i + 1).ToString(), CellAlignment.MiddleCenter);

                    for (int j = 0; j < columns.Count(); j++)
                    {
                        if (j < contents.Length && !contents[j].empty())
                        {
                            bool center = columns.ElementAt(j).st_("#C");
                            
                            SetCellContent(tb, i + 2, j, DE.DEF_TABLE_ROWHEIGHT / 2, contents[j],
                                columns.ElementAt(j).st_("#C") ? CellAlignment.MiddleCenter : CellAlignment.MiddleLeft);
                        }
                    }
                }

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                res = btr.AppendEntity(tb);
                db._setLayer(res, DE.DEF_LAYER_TEXT);
                tr.AddNewlyCreatedDBObject(tb, true);

                tb.TableStyle = db.Tablestyle;
                tb.GenerateLayout();

                tr.Commit();
            }
            
            return res;
        }

        static string _firstValueBySpacing(this string st)
        {
            string[] ar = st.filter(" ");
            string res = "";
            for (int i = 0; i < ar.Length - 1; i++)
                res += ar[i] + " ";
            return res.Trim();
        }

        static string _lastValueBySpacing(this string st)
        {
            return st.filter(" ").Last();
        }

        static string[] _getTableCellDVT(IEnumerable<string> str, IEnumerable<string> headers)
        {
            string[] res = new string[headers.Count()];

            for (int i = 0; i < headers.Count(); i++)
            {
                string h = headers.ElementAt(i);
                string first = str.First(tmp => !tmp._prop(h).empty());
                
                if (first != null)
                {
                    string k = first._prop(h)._lastValueBySpacing();
                    //db.WR("First {0} Val {1}", first, k);
                    if (str.All(s => s._prop(h).empty() || s._prop(h)._lastValueBySpacing().ToUpper() == k.ToUpper()))
                        res[i] = k;
                }
            }

            return res;
        }

        public static ObjectId CreateStandardTable(this Database db, string _title, IEnumerable<string> str, pPos basept)
        {
            ObjectId res = new ObjectId();

            if(_title.StartsWith("#"))
            {
                db.GetEntities(null, EN_SELECT.AC_DXF, "ACAD_TABLE");
                int index = IR.SelectedIds.FindIndex(id => db._getIdName(id).Upper() == _title.Upper());
                if (index != -1)
                {
                    basept = db._getPoint(IR.SelectedIds[index]);
                    db.EraseObject(IR.SelectedIds[index]);
                }
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                Table tb = new Table();

                //db.WRArray("[Table_Content]", str);

                string[] headers = str._getPramsHeaders();

                //headers = headers.OrderBy(s => s).ToList();

                tb.NumRows = str.Count() + 2;
                tb.NumColumns = headers.Length;
                tb.HorizontalCellMargin = 50;
                tb.VerticalCellMargin = 0;

                tb.TableStyle = db.Tablestyle;

                TableStyle ts = (TableStyle)tr.GetObject(tb.TableStyle, OpenMode.ForWrite);
                ts.HorizontalCellMargin = 150;

                tb.SetRowHeight(DE.DEF_TABLE_ROWHEIGHT);

                //db.WR("Rows {0} Columns {1}", tb.NumRows, tb.NumColumns);

                tb.BreakEnabled = true;
                tb.SetBreakSpacing(5);
                tb.SetBreakHeight(0, DE.DEF_TABLE_ROWBREAK * DE.DEF_TABLE_ROWHEIGHT);

                //db.WR("TABLE_CREATOR\nColumns = {0}, Rows = {1}\n", tb.Columns.Count, tb.Rows.Count);

                tb.SetCellContent(0, 0, DE.DEF_TABLE_ROWHEIGHT * 2 / 3, _title, CellAlignment.MiddleCenter);

                CellAlignment[] cell_aligns = new CellAlignment[headers.Length];
                string[] cell_dvt = _getTableCellDVT(str, headers);

                //db.WRArray("Cell DVT", cell_dvt);

                for (int i = 0; i < headers.Length; i++)
                {
                    string[] ar = str.Where(s => !s._prop(headers[i]).empty())
                        .Select(s => cell_dvt[i].empty() ? s._prop(headers[i]) 
                        : s._prop(headers[i])._firstValueBySpacing()).ToArray();
                    
                    tb.Columns[i].Width = DE.DEF_TABLE_ROWHEIGHT * 5;
                    
                    if (ar.All(s => s.Length < 10))
                    {
                        cell_aligns[i] = CellAlignment.MiddleCenter;
                    }else
                    {
                        cell_aligns[i] = CellAlignment.MiddleLeft;
                        tb.Columns[i].Width *= 2;
                    }

                    string content = headers[i];
                    if (!cell_dvt[i].empty()) content += "\r\n(" + cell_dvt[i] + ")";

                    tb.SetCellContent(1, i, DE.DEF_TABLE_ROWHEIGHT / 2, content, CellAlignment.MiddleCenter);
                }
                
                for (int i = 0; i < str.Count(); i++)
                    for (int j = 0; j < headers.Length; j++)
                    {
                        string content = cell_dvt[j].empty() ? str.ElementAt(i)._prop(headers[j])
                            : str.ElementAt(i)._prop(headers[j])._firstValueBySpacing();

                        if (!content.empty())
                            tb.SetCellContent(i + 2, j, DE.DEF_TABLE_ROWHEIGHT / 2, content, cell_aligns[j]);
                    }

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                res = btr.AppendEntity(tb);
                db._setLayer(res, DE.DEF_LAYER_TEXT);
                tr.AddNewlyCreatedDBObject(tb, true);

                
                tb.Position = basept.ToPoint3();
                tb.GenerateLayout();

                tr.Commit();
            }
            
            return res;
        }

        private static void SetCellContent(this Table tb, int i, int j, double nTextHeight, string content, CellAlignment algn)
        {
            CellRange cell = tb.Cells[i, j];
            
            tb.Cells[i, j].TextHeight = nTextHeight;
            if(!content.empty()) tb.Cells[i, j].TextString = content;
            if (algn != null)
                tb.Cells[i,j].Alignment = algn;// (CellAlignment)algn);
        }


        public static string _getTabTitle(this Database db, ObjectId tabId)
        {
            string res = null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Table tb = (Table)tr.GetObject(tabId, OpenMode.ForRead);
                res = tb.Cells[0, 0].GetTextString(FormatOption.IgnoreMtextFormat);
                tr.Commit();
            }
            
            return res;
        }

        //public static List<string[]> element_object_keys, element_object_descs; // for object with LA < 10.?.?

        //public static string[] GetTableElement(int category)
        //{
        //    return ITable.TEMPLATE_ELEMENT.Cast<string>()
        //                .Where(st => (int)(st._prop("LA").ChainNumber() / 1000000) == category).Select(st => st).ToArray();
        //}

        public static string GetTableElement(string[] contents, string _key)
        {
            string res = "";
            string key = _key;
            int index = Array.FindIndex(ITable.TEMPLATE_HEADER, st => st._prop("KEY") == key);
            if (index != -1 && index < contents.Length)
                res = contents[index];
            return res;
        }

        //public static void _loadTableLibDatabase(this Database db)
        //{
        //    //using (Database new_db = pDES.ReadDWG(DE.CAD_TEMPLATE_LAYOUT))
        //    //{
        //    //    TEMPLATE_HEADER = new_db._getTabContent("#HEADER");
        //    //    TEMPLATE_ELEMENT = new_db._getTabContent("#ELEMENT");
        //    //    TEMPLATE_DRAWING = new_db._getTabContent("#DRAWING");
        //    //}

        //    //DRAWING_LIST = db._getTabContent("#DRAWING");

        //    if(DRAWING_LIST.Length == 0)
        //    {
        //        string[] files = System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(ACD.CurrentDWGPath) + @"\", "*.dwg");

        //        db.WRArray("SEARCH", files);

        //        foreach (string f in files)
        //        {
        //            using (Database new_db = ACD.ReadDWG(f))
        //            {
        //                DRAWING_LIST = new_db._getTabContent("#DRAWING");
        //                if (DRAWING_LIST.Length > 0)
        //                {
        //                    db.WR("Found table drawing list {0}", f);
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //    //db.WRArray("DRAWING", DRAWING_LIST);
        //}

        public static ObjectId _getRefTable(this Database db, ObjectId tabId)
        {
            ObjectId res = ObjectId.Null;
            if (!db._getIdName(tabId).EndsWith("_PREF"))
            {
                string title = db._getIdName(tabId);
                db.GetEntities(null, EN_SELECT.AC_DXF, "ACAD_TABLE");

                ObjectIdCollection tableIds = IR.SelectedIds.Cast<ObjectId>()
                    .Where(id => id != tabId && db._getIdName(id) == title + "_PREF").Select(id => id).ToCollection();

                if (tableIds.Count > 0)
                    res = tableIds.First();
            }
            return res;
        }

        public static string GetTableElement(this Database db, pPos pt)
        {
            string res = null;

            db.GetEntities(null, EN_SELECT.AC_DXF, "ACAD_TABLE");
            ObjectIdCollection tableIds = IR.SelectedIds.Cast<ObjectId>()
                .Where(id => pt.Inside(db._getBound(id).Rect())).Select(id => id).ToCollection();

            if (tableIds.Count > 0)
            {
                ObjectId current_table_Id = tableIds.First();
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Table tb = (Table)tr.GetObject(current_table_Id, OpenMode.ForRead);

                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        for (int j = 0; j < tb.Columns.Count; j++)
                        {
                            Point3dCollection p3d = new Point3dCollection();
                            tb.GetCellExtents(i, j, false, p3d);
                            pPos[] bb = p3d.Cast<Point3d>().Select(p => p.ToPos()).Boundary();
                            if (pt.InsideRect(bb[0], bb[1]))
                            {
                                string[] current_contents = db._getTabContent(current_table_Id);

                                //db.WRArray("contents", current_contents);
                                res = current_contents[i - 2];
                                break;
                            }
                        }
                        if (res != null)
                            break;
                    }

                    tr.Commit();
                }
            }

            return res;
        }

        public static ObjectIdCollection CreateSchedule(this Database db, string title, IEnumerable<string> infors, pPos pt)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            
            db.GetEntities(null, EN_SELECT.AC_DXF, "ACAD_TABLE");

            ObjectIdCollection tableIds = IR.SelectedIds.Cast<ObjectId>()
                .Where(id => db._getIdName(id).StartsWith(title)).Select(id => id).ToCollection();

            //db.WR("SelectIds {0} TableIds {1}", IR.SelectedIds.Count, tableIds.Count);
            if (tableIds.Count > 0)
            {
                pt = db._getPoint(tableIds[0]);
                db.EraseObjects(tableIds);
            }

            res = db.CreateTable(title, infors, true);

            if (res.Count > 0)
                db.MoveObject(res, pt);

            return res;
        }
    }
}