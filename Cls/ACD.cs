using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AcadScript
{
    public static class ACD//: MarshalByRefObject
    {
        
        public static int flip;
        public static pPos MinPoint, MaxPoint, FirstPoint, LastPoint;

        public static string CADLIB = @"D:\Dropbox\CADLIb\";
        public static string CAD_UI = CADLIB + @"UI\";
        public static string CADLIB_ELEMENT = CADLIB + @"Elements\";
        public static string CADLIB_CONSTRUCT = CADLIB + @"Constructs\";

        public static string CADLIB_CONSTRUCT_HATCHSAMPLE = CADLIB_CONSTRUCT + @"HatchSample.txt";
        public static string CADLIB_CONSTRUCT_HATCHSETTING = CADLIB_CONSTRUCT + @"HatchSetting.txt";
        public static string CAD_TEMPLATE_DIR_TEMP = @"D:\Temp\";
        public static string CAD_TEMPLATE_FILE = CADLIB + "Template.dwg";
        public static string CADLIB_SAMPLES = CADLIB + @"Samples\";
        public static string CADLIB_SAMPLES_ROOM = CADLIB_SAMPLES + @"Room\";
        public static int[] DEF_SCALE_LIST = new int[] { 1, 2, 5, 10, 20, 25, 50, 80, 100, 120,140,150,180, 200, 250, 400, 500, 1000, 2000 };


        public static int PlotMethodType;
        public static System.Windows.Forms.ProgressBar ProgressLoad;
        public static bool ProgressCancel;
        public static System.Windows.Forms.Label ProgressPercent;

        public static System.Windows.Forms.ComboBox cbHatch, cbCategory, cbPlot, cbLayerKey, cbScale;

        public static string FindFullname(string filekey)
        {
            string dir = @"D:\Dropbox\VS Projects\ACadScript\Sample\";
            ACD.WR("Current sample dir: {0}", dir);
            string[] files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);

            string res = null;

            foreach (string f in files)
                if (Path.GetFileNameWithoutExtension(f).st_(filekey.Upper()) 
                    || Path.GetFileName(f).st_(filekey.Upper()))
                {
                    res = f;
                    break;
                }

            return res;
        }


        public static void Get2Points()
        {
            ACD.Focus();
            FirstPoint = null;
            LastPoint = null;

            PromptPointResult ppo = ACD.ED.GetPoint("\nPick first point: ");
            if (ppo.Status == PromptStatus.OK)
                FirstPoint = ppo.Value.ToPos();

            ppo = ACD.ED.GetPoint("\nPick second point: ");
            if (ppo.Status == PromptStatus.OK)
                LastPoint = ppo.Value.ToPos();

            if (FirstPoint != null && LastPoint != null)
            {
                MinPoint = (new pPos[] { FirstPoint, LastPoint }).MinPoint();
                MaxPoint = (new pPos[] { FirstPoint, LastPoint }).MaxPoint();
            }
            else
                MinPoint = MaxPoint = null;
        }

        public static pPos[] bRect
        {
            get
            {
                List<pPos> srcbb = bScreen.ToList();
                return srcbb.Boundary().ToArray();
            }
        }

        public static pPos[] bScreen
        {
            get
            {
                Point2d screenSize = (Point2d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("SCREENSIZE");
                System.Drawing.Point upperLeft = new System.Drawing.Point(0, 0);
                System.Drawing.Point lowerRight = new System.Drawing.Point((int)screenSize.X, (int)screenSize.X);

                return new pPos[] { MinPoint, MaxPoint };
            }
        }

        //public static DocumentLock Lock(Database db = null)
        //{
        //    if (db == null) db = ACD.DB;
        //    Document doc = GetDoc(db);
        //    return doc.LockDocument();
        //}
                
        public static Document DOC { get { return Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument; } }
        public static Database DB { get { return DOC.Database; } }
        public static Editor ED { get { return DOC.Editor; } }

        public static Editor GetEditor(this Database db)
        {
            
            Document oDoc = Application.DocumentManager.GetDocument(db);
            Editor res = null;

            if (oDoc != null)
            {
                res = oDoc.Editor;

            }
            return res;
        }
        //public static void WR(this Database db, string message, params object[] contents)
        //{
        //    Editor ED = db.GetEditor();

        //    if (ED != null)
        //    {
        //        string st = String.Format(message, contents);

        //        ED.WriteMessage(st + "\r\n");
        //        string fname = @"D:\CADinfo.txt";

        //        if (System.IO.File.Exists(fname))
        //            System.IO.File.AppendAllText(fname, st);
        //        else
        //            System.IO.File.WriteAllText(fname, st);
        //    }
        //}

        public static void AlignObject(this Database db, ObjectId objId, pPos p1, pPos p2)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity obj = (Entity)tr.GetObject(objId, OpenMode.ForWrite);
                if (obj != null)
                {
                    var rot = Math.Abs(p1.AngleVector(p2, p1 + new pPos(1, 0))) % 180;
                    if (rot < 1 || rot > 179) rot = 0;

                    try
                    {
                        Matrix3d curUCSMatrix = db.GetEditor().CurrentUserCoordinateSystem;
                        CoordinateSystem3d curUCS = curUCSMatrix.CoordinateSystem3d;

                        var mid = (p1 + p2) / 2;

                        obj.TransformBy(Matrix3d.Rotation(rot / 180 * Math.PI, curUCS.Zaxis, new Point3d(mid.X, mid.Y, 0)));
                        obj.UpgradeOpen();
                    }
                    catch (System.Exception ex)
                    {

                    }
                }
                tr.Commit();
            }
        }

        public static void WRArray(string message, ObjectIdCollection contents)
        {
            WR(message + " count = {0}\r\n", contents.Count);
            string res = "";
            foreach (ObjectId id in contents)
                res += id == ObjectId.Null ? "NULL" : id.ObjectClass.DxfName + ";";

            WR("{0}\r\n", res);
        }

        public static void WRArray(string message, IEnumerable<double> contents)
        {
            WR(message + " count = {0}\r\n", contents.Count());
            string res = "";
            foreach (double st in contents)
                res += st.roundNumber(2).ToString() + ";";

            WR("{0}\r\n", res);
        }

        public static void WRArray(string message, IEnumerable<pPos> contents)
        {
            WR("<" + message + ">count = {0}\r\n", contents.Count());
            string res = "";
            foreach (pPos pt in contents)
                res += pt == null ? "NULL" : pt.ToString() + ";";

            WR("{0}\r\n", res);
        }

        public static void WRArray(this Database db, string message, IEnumerable<int> contents)
        {
            WR(message + "({0} items)", contents.Count());
            string res = "";
            foreach (int st in contents)
                res += st.ToString() + ";";

            WR("{0}\r\n", res);
        }

        public static void WRArray(string message, IEnumerable<string> contents, string seperator = ";")
        {
            WR(message + "({0} items)", contents.Count());
            string res = "";
            foreach (string st in contents)
                res += st == null ? "NULL" : st + seperator;

            WR("{0}\r\n", res);
        }

        //---------------------------------------------------------------------------------------------------------

        public static string[] GetXNotes(this Database db, ObjectIdCollection ids)
        {
            return ids.ToList().SelectMany(id => db.GetXNotes(id)).Distinct().ToArray();
        }

        public static string[] GetXNotes(this Database db, ObjectId objId)
        {
            string res = null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity acEnt = tr.GetObject(objId, OpenMode.ForRead) as Entity;

                ObjectId dbIdExtDict = acEnt.ExtensionDictionary;
                if (dbIdExtDict.IsValid)
                {
                    DBDictionary dbExtDict = tr.GetObject(dbIdExtDict, OpenMode.ForRead) as DBDictionary;
                    if (dbExtDict != null)
                        foreach (DBDictionaryEntry ent in dbExtDict)
                        {
                            DBObject dbEntry = tr.GetObject(ent.Value, OpenMode.ForRead, false);
                            if (dbEntry != null && dbEntry.GetType() == typeof(Autodesk.Aec.DatabaseServices.TextNote))
                            {
                                Autodesk.Aec.DatabaseServices.TextNote xRec = dbEntry as Autodesk.Aec.DatabaseServices.TextNote;
                                if (xRec != null)
                                    res += xRec.Note;
                            }
                        }
                }

                tr.Commit();
            }

            return res.empty() ? new string[0] : res.filter("\r\n");
        }

        //public static string GetNoteXData(this Database db, ObjectId id)
        //{
        //    string res = null;

        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite, false);

        //        //ent.ExtensionDictionary;
        //        ObjectId dbIdExtDict = ent.ExtensionDictionary;
        //        if (dbIdExtDict.IsValid)
        //        //ACD.WR("Object has no extension dictionary.");
        //        //else
        //        {
        //            DBDictionary dbExtDict = tr.GetObject(dbIdExtDict, OpenMode.ForRead) as DBDictionary;
        //            if (dbExtDict != null)
        //            //ACD.WR("Object has no extension dictionary.");
        //            //else
        //            {
        //                foreach (var itm in dbExtDict)
        //                {
        //                    Autodesk.Aec.DatabaseServices.TextNote txt = (Autodesk.Aec.DatabaseServices.TextNote)tr.GetObject(itm.Value, OpenMode.ForRead);

        //                    if (txt != null)
        //                    {
        //                        res = txt.Note;
        //                        break;
        //                    }
        //                }
        //            }
        //        }
        //        //ReportDictionary(pcc, dbExtDict, 0);
        //        //Acad.Report("\n");


        //        tr.Commit();
        //    }

        //    return res;
        //}

        public static void SetXNotes(this Database db, ObjectId id, IEnumerable<string> values)
        {
            db.SetXNotes(id, values.ToTextStr("\r\n"));
        }

        public static void SetXNotes(this Database db, ObjectId id, string value)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //for (int i = 0; i < ids.Count; i++)
                {
                    Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite, false);

                    if (!ent.ExtensionDictionary.IsValid)
                        ent.CreateExtensionDictionary();
                    //ACD.WR("OK1");
                    DBDictionary dbExtDict = tr.GetObject(ent.ExtensionDictionary, OpenMode.ForWrite) as DBDictionary;
                    //ACD.WR("OK1.2");
                    Autodesk.Aec.DatabaseServices.TextNote txt = null;

                    try
                    {
                        foreach (var itm in dbExtDict)
                        {
                            //ACD.WR("OK1.3");
                            Autodesk.Aec.DatabaseServices.TextNote tmp = (Autodesk.Aec.DatabaseServices.TextNote)tr.GetObject(itm.Value, OpenMode.ForWrite);
                            //ACD.WR("OK1.4");
                            if (tmp != null)
                                txt = tmp;
                        }

                        txt.Note = value;
                    }
                    catch (System.Exception ex)
                    { }

                    if (txt == null)
                    {
                        txt = new Autodesk.Aec.DatabaseServices.TextNote(); //(Autodesk.Aec.DatabaseServices.TextNote)tr.GetObject(itm.Value, OpenMode.ForRead);
                        txt.Note = value;
                        dbExtDict.SetAt(Autodesk.Aec.DatabaseServices.TextNote.ExtensionDictionaryName, txt);
                        tr.AddNewlyCreatedDBObject(txt, true);
                    }

                    tr.Commit();
                }
            }
        }

        public static Document IsOpenDocument_document;

        public static bool IsOpenDocument(string fname)
        {
            DocumentCollection docs = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager;
            HostApplicationServices hs = HostApplicationServices.Current;

            IsOpenDocument_document = null;

            bool res = false;

            //DB.WRArray("FILES", docs.Cast<Document>().Select(doc
            //    => hs.FindFile(doc.Name, doc.Database, FindFileHint.Default)));

            if (docs.Count > 0)
            {
                string[] files = docs.Cast<Document>().Select(doc
                    => hs.FindFile(doc.Name, doc.Database, FindFileHint.Default).ToUpper()).ToArray();

                string key = Path.GetFileNameWithoutExtension(fname).Upper();
                int index = Array.FindIndex(files, f => Path.GetFileNameWithoutExtension(f).Upper().Contains(key));
                ACD.WR("Fname {0} Index {1}", fname, index);

                if (index != -1)
                {
                    res = true;
                    IsOpenDocument_document = docs.Cast<Document>().ToArray()[index];
                }
            }
            return res;
        }

        public static string ConstructDir
        {
            get
            {
                string res = null;
                string[] dirs = Directory.GetDirectories(Path.GetDirectoryName(CurrentDWGPath));
                int index = Array.FindIndex(dirs, st => st.Upper().Contains("CONST"));

                if (index != -1)
                    res = dirs[index];
                else
                    res = Path.GetDirectoryName(CurrentDWGPath);
                return res;
            }
        }
        public static string CurrentDWGPath
        {
            get
            {
                string res = ACD.CAD_TEMPLATE_FILE;
                HostApplicationServices hs = HostApplicationServices.Current;
                try
                {
                    res = hs.FindFile(ACD.DOC.Name, ACD.DB, FindFileHint.Default);
                }
                catch (System.Exception ex)
                {

                }
                return res;
            }
        }


        public static void SaveAs2007(this Database db, string fname)
        {
            if (File.Exists(fname)) File.Delete(fname);
            db.SaveAs(fname, true, DwgVersion.AC1021, db.SecurityParameters);
        }


        public static string SaveAs2007()
        {
            string st = CurrentDWGPath;
            string res = null;
            if (st != null)
            {
                string dir = Path.GetDirectoryName(st) + @"\exp";
                Directory.CreateDirectory(dir);
                res = dir + @"\" + Path.GetFileNameWithoutExtension(st) + "(2007).dwg";

                if (File.Exists(res)) File.Delete(res);
                DB.SaveAs(res, true, DwgVersion.AC1021, DB.SecurityParameters);
                System.Windows.Forms.Clipboard.SetText(res);
                ACD.WR("Save 2007 file: {0}", res);
            }

            return res;
        }
        public static string CurrentDWGFileName
        {
            get
            {
                string st = CurrentDWGPath;
                if (st != null) st = Path.GetFileNameWithoutExtension(st);
                return st.empty() ? "Untitle" : st;
            }
        }
        public static bool IsCurrentFile(string f)
        {
            string st = Path.GetFileNameWithoutExtension(f);
            return st.ToUpper() == CurrentDWGFileName.ToUpper();
        }

        public static void SaveDB(this Database db, string fname, params pPos[] zoom_bound)
        {
            if (IsOpenDocument(fname))
            {
                System.Windows.Forms.MessageBox.Show("File " + fname + " is opened!\r\nCannot save!");
            }
            else
            {
                //if (zoom_bound.Length > 1)
                //    db.ZoomBounds(zoom_bound[0], zoom_bound[1]);
                //else
                //    db.ZoomBounds(new pPos(0, 0), new pPos(10000, 10000));
                db.SaveAs(fname, DwgVersion.Current);
            }
        }
        public static pPos GetPoint()
        {
            ACD.Focus();
            PromptPointResult ppo = ACD.ED.GetPoint("\nPickpoint: ");

            if (ppo.Status == PromptStatus.OK)
                return ppo.Value.ToPos();
            else
                return null;
        }

        public static string GetInputString(this Editor ed, string message, string defalt = "")
        {
            PromptStringOptions pStrOpts = new PromptStringOptions("\n" + message + " ");
            pStrOpts.AllowSpaces = true;
            pStrOpts.DefaultValue = defalt;

            PromptResult pStrRes = ed.GetString(pStrOpts);
            string result = pStrRes.StringResult;

            if (result.empty()) result = defalt;
            return result;
        }

        public static ObjectIdCollection FilterIds(this ObjectIdCollection selIds, params string[] dxfs)
        {
            return selIds.ToList().Where(id
                        => dxfs.Contains(id.ObjectClass.DxfName)).ToCollection();
        }

        public static void Echo(string msg, params object[] contents)
        {
            ED.WriteMessage(String.Format(msg, contents));
            System.Windows.Forms.MessageBox.Show(msg);
        }



        public static void WR(string message, params object[] contents)
        {
            var ED = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            string st = String.Format(message, contents);

            ED.WriteMessage(st + "\r\n");
            string fname = @"D:\CADinfo.txt";

            if (System.IO.File.Exists(fname))
                System.IO.File.AppendAllText(fname, st + "\r\n");
            else
                System.IO.File.WriteAllText(fname, st + "\r\n");
        }

        public static pPos CurrentSelectedMousePosition;
        public static string[] AllSelectionXNotes;

        public static ObjectIdCollection GetSelection()
        {
            ACD.Focus();
            InitMousePoint();
            PromptSelectionResult psr = ACD.ED.GetSelection();
            ObjectIdCollection ids = new ObjectIdCollection();

            if (psr.Status == PromptStatus.OK)
                foreach (SelectedObject obj in psr.Value)
                    ids.Add(obj.ObjectId);

            CurrentSelectedMousePosition = cur_mouse_point.Clone();
            AllSelectionXNotes = ACD.DB.AllXNotes(ids);

            //ACD.DB.GetNoteXData
            return ids;
        }

        public static string[] GetHatchSetting(string hstyle)
        {
            string[] res = null;
            var dict = File.ReadAllLines(CADLIB_CONSTRUCT_HATCHSAMPLE).GetChapters();

            if (dict.Count > 0)
            {
                int index = dict.Keys.ToList().FindIndex(k => k.Upper() == hstyle.Upper());

                if (index != -1)
                {
                    res = dict.Values.ElementAt(index);
                }
            }

            return res;
        }

        public static string BoundScale(this IEnumerable<pPos> pts, double page_width, double page_height)
        {
            pPos[] bound = pts.Rotate(pts.LayoutRotation(), pts.CenterPoint()).Boundary();

            double offset = 10;
            bound[0] -= new pPos(offset, offset);
            bound[1] += new pPos(offset, offset);

            double sc = Math.Min((bound[1].X - bound[0].X) / page_width, (bound[1].Y - bound[0].Y) / page_height);

            int index = Array.FindIndex(ACD.DEF_SCALE_LIST, n => sc < n + 2);
            //ACD.WR("_boundScale Scale {0} Value {1}", sc, index != -1 ? DE.DEF_SCALE_LIST[index] : 1);

            return index != -1 ? ("1:" + ACD.DEF_SCALE_LIST[index]) : "1:1";
        }


        public static int GetHatchSetting(this string hathpattern, string key = "HSCALE")
        {
            int hscale = 1;
            if (File.Exists(ACD.CADLIB_CONSTRUCT_HATCHSETTING))
            {
                string[] str = File.ReadAllLines(ACD.CADLIB_CONSTRUCT_HATCHSETTING);
                var dict = str.GetChapters();

                for (int i = 0; i < dict.Count(); i++)
                    if (dict.Keys.ElementAt(i).Upper() == key.Upper())
                        hscale = (int)dict.Values.ElementAt(i)._props(hathpattern).ToNumber();
            }
            return hscale;
        }

        public static string RefFileList(string path = null)
        {
            string[] reffiles = path == null ?
                Directory.GetFiles(CADLIB_SAMPLES, "*.dwg", SearchOption.AllDirectories)
                : Directory.GetFiles(path, "*.dwg");

            reffiles = reffiles.Select(f => Path.GetFileNameWithoutExtension(f)).OrderBy(f => f).ToArray();

            string res = "<None;";

            if (path == ACD.CADLIB_SAMPLES_ROOM)
                res += "Plan;";

            foreach (string f in reffiles)
                res += f + ";";
            res += ">";

            return res;
        }

        public static string GetFileNamePrefix(string fname)
        {
            string chars = "ABCDEF";
            string res = null;
            string[] ar = Path.GetFileNameWithoutExtension(fname).filter(" ._-")
                .OrderBy(st => st.Length).ToArray();
            foreach (string st in ar)
                if (st.Length < 3 && chars.Contains(st[0].ToString().Upper()))
                {
                    res = st;
                    break;
                }
            return res;
        }

        public static Database NewDWG(string strFileName, string template = null)
        {
            Database db = null;
            if (template == null) template = ACD.CAD_TEMPLATE_FILE;

            if (File.Exists(template))
            {
                if (File.Exists(strFileName)) File.Delete(strFileName);

                System.IO.File.Copy(template, strFileName);

                if (!System.IO.File.Exists(template))
                {
                    //ACD.DB.WR("CreateDWG: Error in create file : {0}\n", strFileName);
                    return null;
                }

                db = new Database(true, true);
                db.ReadDwgFile(strFileName, FileShare.ReadWrite, true, "");
                //db.EraseObjects();
            }
            //else
                //ACD.DB.WR("Template file {0} not exist\n", template);

            return db;
        }

        public static void OpenDWG(string strFileName)
        {
            DocumentCollection acDocMgr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager;
            if (File.Exists(strFileName)) acDocMgr.Open(strFileName, false);
        }

        public static string[] LoadChapter(string file, string key)
        {
            string[] prams = new string[0];
            if (!File.Exists(file)) return prams;
            var chapters = File.ReadAllLines(file).GetChapters();
            //ACD.WR("Chapters {0}", chapters.Count);
            if (chapters.Count > 0)
            {
                //ACD.WR("H03");
                int index = chapters.Keys.ToList().FindIndex(k => k.Upper() == key.Upper());

                if (index != -1)
                    prams = chapters.Values.ElementAt(index);
            }

            return prams;
        }

        //public static int PlotMethodType;
        //public static ProgressBar ProgressLoad;
        //public static bool ProgressCancel;
        //public static Label ProgressPercent;

        //public static ComboBox cbHatch, cbCategory, cbPlot;

        public static string CURRENT_IMPORT_CHAPTER = "";

        public static float PLOT_OVERLAY = 1f;

        public static ObjectIdCollection CurrentSelIds;
        public static ObjectId CurrentRegionId;
        //public static List<KVL> Regions, Rooms, Levels;
        public static PosCollection Sections;
        public static double Tole = 5;

        public static string AC_EXCEL_LIB = @"D:\Dropbox\skMSTool\Docs\skObjectInfo.xlsx";
        public static bool plot_with_annotative = false;

        public static string currentBASEBoundName;
        public static PosCollection BASEBounds;
        public static List<string> BASEBoundNames;

        public static bool need_reload_file = false;
        public static string[] DV_CLIPBOARD;
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref RECT rectangle);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public static System.Windows.Forms.RichTextBox OutputText;


        public static void Output(string s)
        {
            OutputText.AppendText(s);
        }

        public static void OutputLine(string s = "")
        {
            Output(s);
            Output("\n");
        }

        static System.Drawing.Point[] GetWndRect(IntPtr hwnd)
        {
            RECT r = new RECT();
            GetWindowRect(hwnd, ref r);
            return new System.Drawing.Point[] { new System.Drawing.Point(r.Left, r.Top),
                new System.Drawing.Point(r.Right, r.Bottom) };
        }

        static string GetTitle(IntPtr hwnd)
        {
            int capacity = GetWindowTextLength(hwnd) * 2;
            StringBuilder stringBuilder = new StringBuilder(capacity);
            GetWindowText(hwnd, stringBuilder, stringBuilder.Capacity);
            return stringBuilder.ToString();
        }

        public static System.Drawing.Point[] FindViewportBounding()
        {
            System.Drawing.Point[] res = null;
            IntPtr acadHandle = Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Handle;
            var allChildWindows = new WindowHandleInfo(acadHandle).GetAllChildHandles();

            foreach (IntPtr hwnd in allChildWindows)
            {
                if (GetTitle(hwnd).ToLower().EndsWith(".dwg"))
                {
                    var allSubs = new WindowHandleInfo(hwnd).GetAllChildHandles();
                    //ACD.WR("Chilren {0}", allSubs.Count);

                    res = allSubs.Select(h => GetWndRect(h))
                        .OrderBy(ls => ls[1].X - ls[0].X).ThenBy(ls => ls[1].Y - ls[0].Y).Last();
                    break;
                }
            }

            return res;
        }
        public static pPos cur_mouse_point;
        public static void InitMousePoint()
        {
            cur_mouse_point = new pPos(0, 0);
            ACD.ED.PointMonitor += new PointMonitorEventHandler(ed_PointMonitor);
        }

        private static void ed_PointMonitor(object sender, PointMonitorEventArgs e)
        {
            cur_mouse_point = new pPos(e.Context.ComputedPoint.X, e.Context.ComputedPoint.Y);
        }

        public static ObjectIdCollection strToObjectId(this Database db, string st)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            foreach (string s in st.filter())
            {
                try
                {
                    ObjectId id = db.GetObjectId(false, new Handle(Convert.ToInt64(s)), 0);
                    if (db.ValidId(id))
                    {
                        res.Add(id);
                    }
                }
                catch (System.Exception ex)
                {
                    //db.WR("Error in id {0}", s);
                }
            }
            return res;
        }

        public static System.Windows.Point PointToScreen(double x, double y)
        {
            System.Drawing.Point[] pts = FindViewportBounding();

            Point3d pt = new Point3d(x, y, 0);
            System.Windows.Point pix = ACD.ED.PointToScreen(pt, 0);

            return new System.Windows.Point(pix.X, pix.Y);
        }

        public static void ReloadFile()
        {
            string fname = ACD.CurrentDWGFileName;
            ACD.DOC.CloseAndSave(fname);
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(fname, false);
        }

        public static string[] AC_HATCH_PATTERN = new string[]
        {
            "PATTERN=ANSI31|SCALE=100", "PATTERN=ANSI31|SCALE=100|ANGLE=90", "PATTERN=ANSI32|SCALE=25",
            "PATTERN=ANSI32|SCALE=25|ANGLE=90", "PATTERN=ANSI33|SCALE=100", "PATTERN=ANSI33|SCALE=100|ANGLE=90",
            "PATTERN=ANSI34|SCALE=100", "PATTERN=ANSI34|SCALE=100|ANGLE=90", "PATTERN=ANSI37|SCALE=100", "PATTERN=ANSI37|SCALE=100|ANGLE=45"
        };

        public static string[] ViewImageList, ViewImageTitles;

        public static ObjectIdCollection PatternIds, ErasedIds;

        //public static KeyValuePair<string, double>[] Levels
        //{
        //    get
        //    {
        //        string number = "-0123456789";
        //        List<KeyValuePair<string, double>> res = new List<KeyValuePair<string, double>>();
        //        string[] prams = ACD.DB.GetDrawingProp().Where(s => s.StartsWith("#LEVEL")).Select(s => s).ToArray();

        //        //ACD.WR("Levels Step 1 {0}", prams.Length);

        //        for (int i = 0; i < prams.Length; i++)
        //        {
        //            string[] ar = prams[i].filter("[]").Where(s => s.StartsWith("LC")).Select(s => s).ToArray();
        //            if (ar.Length > 0)
        //            {
        //                double[] vals = ar.First().filter(" ")
        //                    .Where(s => number.Any(k => s.StartsWith(k.ToString()))).Select(s => s.ToNumber()).ToArray();

        //                //ACD.WR("Levels Step 2");

        //                string[] level_names = prams[i].filter("=").First().Trim().filter(",");

        //                for(int j = 0; j < vals.Length; j ++)
        //                    if(level_names.Length > j)
        //                        res.Add(new KeyValuePair<string, double>(level_names[j] + "_" + j.ToString(), vals[j]));
        //                    else
        //                        res.Add(new KeyValuePair<string, double>(level_names.Last() + "_" + j.ToString(), vals[j]));
        //               // ACD.WR("Levels Step 3");
        //            }
        //        }

        //        return res.ToArray();
        //    }
        //}

        //public static ObjectIdCollection DrawLevelGrid(int axis, pPos[] bb)
        //{
        //    ObjectIdCollection res = new ObjectIdCollection();
        //    var levels = ACD.Levels;

        //    for (int i = 0; i < levels.Length; i++)
        //    {
        //        pPos p1 = new pPos(bb[0][axis], levels[i].Value);
        //        pPos p2 = new pPos(bb[1][axis], levels[i].Value);
        //        res.Add(ACD.DB.DrawPolyline(new pPos[] { p1, p2}));
        //        res.AddRange(ACD.DB.Insert(DE.CAD_TEMPLATE_FILE, "G_Cote", new pPos[] { p1 }));
        //        res.Add(ACD.DB.CreateText("#L" + levels[i].Value, p1 + new pPos(0,300), DE.DEF_TEXT_HEIGHT));
        //    }

        //    return res;
        //}

        //public static KeyValuePair<string, double> GetLevelFromCote(double n)
        //{
        //    KeyValuePair<string, double> res = new KeyValuePair<string, double>(null, 0);

        //    //ACD.WR("GetLevelFromCote Step 1");
        //    var levels = Levels;
        //    //ACD.WR("GetLevelFromCote Step 2");
        //    //foreach (var itm in levels)
        //    //ACD.WR("Level {0} cote {1}", itm.Key, itm.Value);

        //    if (levels.Length > 0)
        //    {
        //        levels = levels.OrderBy(v => Math.Abs(v.Value - n)).ToArray();

        //        res = levels.First();
        //        //res = new KeyValuePair<string, double>(itm.Key, itm.Value.OrderBy(v => Math.Abs(v - n)).First());
        //    }

        //    return res;
        //}

        //public static string GetLevelName(string st)
        //{
        //    string[] ar = st.filter(";");
        //    string res = "";

        //    string key = "0123456789-";
        //    return ar.First(s => key.All(k => !s.StartsWith(k.ToString())) && !s.StartsWith("LC "));
        //}

        //public static string GetLevelNameFromCote(double n)
        //{
        //    return GetLevelName(GetLevelFromCote(n).Key);
        //}

        public static bool ControlHold
        {
            get
            {
                return System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Control;
            }
        }

        public static bool ShiftHold
        {
            get
            {
                return System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Shift;
            }
        }


        public static string TempFileName
        {
            get
            {
                Directory.CreateDirectory(CAD_TEMPLATE_DIR_TEMP);
                string strFilename = CAD_TEMPLATE_DIR_TEMP
                    + Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".dwg";

                while (File.Exists(strFilename))
                    strFilename = CAD_TEMPLATE_DIR_TEMP
                        + Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".dwg";
                return strFilename;
            }
        }

        public static string TempImgName
        {
            get
            {
                Directory.CreateDirectory(CAD_TEMPLATE_DIR_TEMP);
                string strFilename = CAD_TEMPLATE_DIR_TEMP
                    + Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".jpg";

                while (File.Exists(strFilename))
                    strFilename = CAD_TEMPLATE_DIR_TEMP
                        + Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".jpg";
                return strFilename;
            }
        }

        public static Database ReadDWG(string fname)
        {
            Database db = null;
            if (File.Exists(fname) && !Path.GetFileNameWithoutExtension(fname).Upper().Contains("RECOVERX"))
            {
                string strFilename = TempFileName;

                File.Copy(fname, strFilename);

                db = new Database(true, true);

                try
                {
                    db.ReadDwgFile(strFilename, FileShare.Read, true, "");
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    db = null;
                    System.Windows.Forms.MessageBox.Show(ex.Message + Environment.NewLine + ex.ErrorStatus.ToString());
                }

                if (db != null)
                {
                    db.CloseInput(true);
                }
            }
            return db;
        }

        //public static void _readPatternLibrarX(string filename)
        //{
        //    IR.curDB = ACD.ReadDWG(DE.CAD_TEMPLATE_FILE);

        //    IR.GetEntities(filename, null, EN_SELECT.AC_DXF, "HATCH");
        //    PatternIds = new ObjectIdCollection();

        //    if (IR.curDB != null && IR.SelectedIds.Count > 0)
        //    {
        //        PatternIds.AddRange(IR.SelectedIds);
        //    }
        //}

        public static string FindTemplateFile_Result;

        public static string FindTemplateFile(string key, string dir = null)
        {
            string res = null;

            if (dir == null)
                dir = DE.CAD_TEMPLATE_DIR;

            string[] files = Directory.GetFiles(dir, "*.dwg", SearchOption.AllDirectories);
            int index = Array.FindIndex(files, st => Path.GetFileNameWithoutExtension(st).st_(key.Upper()));

            if (index != -1)
                res = files[index];

            return res;
            //{
            //    string[] dirs = Directory.GetDirectories(dir,"*");

            //    foreach (string d in dirs)
            //        if(FindTemplateFile_Result.empty())
            //            FindTemplateFile(key, d);
            //}
        }




        public static void Redraw()
        {
            Autodesk.AutoCAD.ApplicationServices.Application.UpdateScreen();
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.UpdateScreen();
        }

        public static void Focus()
        {
            //if (need_reload_file)
            //    ACD.ReloadFile();

            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
            //ED.Regen();
        }

        public static bool ControlPressed, ShiftPressed, AltPressed;

        public static DocumentLock Lock(Database db = null)
        {
            if (db == null) db = DB;

            Document doc = ACD.GetDoc(db);
            //ACD.ErasedIds = new ObjectIdCollection();

            //ControlPressed = System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Control;
            //AltPressed = System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Alt;
            //ShiftPressed = System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Control;
            //need_reload_file = false;

            return doc.LockDocument();
        }


        public static pPos[] GetPickPts()
        {
            List<pPos> res = new List<pPos>();

            while(true)
            {
                pPos pt = ACD.GetPoint();
                if (pt != null)
                    res.Add(pt);
                else
                    break;
            }

            return res.ToArray();
        }

        public static ObjectIdCollection ExplodeAEC(this Database db, ObjectIdCollection ids, bool erased = true)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (ObjectId id in ids)
                    if (id._isAEC())
                    {
                        Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);

                        DBObjectCollection subs = new DBObjectCollection();
                        ent.Explode(subs);

                        ObjectIdCollection eIds = new ObjectIdCollection();

                        foreach (Entity acEnt in subs)
                        {
                            btr.AppendEntity(acEnt);
                            tr.AddNewlyCreatedDBObject(acEnt, true);

                            DBObjectCollection newsubs = new DBObjectCollection();
                            acEnt.Explode(newsubs);

                            foreach (Entity tmp in newsubs)
                            {
                                btr.AppendEntity(tmp);
                                tr.AddNewlyCreatedDBObject(tmp, true);
                                res.Add(tmp.ObjectId);
                            }

                            eIds.Add(acEnt.ObjectId);
                        }

                        db.EraseObjects(eIds);

                        if (erased)
                            db.EraseObject(id);
                    }
                    else if (db.ValidId(id))
                        res.Add(db.CloneObject(id));
                tr.Commit();
            }

            return res;
        }

        public static void ShowModal(System.Windows.Forms.Form theObject)
        {
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(null, theObject, false);
        }

        public static Document GetDoc(Database db)
        {
            return Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.GetDocument(db);
        }


        public static ObjectIdCollection newImportIds;



        public static bool EscapePressed;
        const int KEXDOWN = 0x0100;

        public static void InitEscapePressed()
        {
            //Autodesk.AutoCAD.ApplicationServices.Application.PreTranslateMessage += (sender2, e2) =>
            //{
            //    if (e2.Message.message == KEXDOWN && ((Keys)(int)e2.Message.wParam & Keys.KeyCode) == Keys.Escape)
            //    {
            //        //do something when ESCAPE has been pressed, for example

            //        EscapePressed = true;
            //    }
            //    else
            //        EscapePressed = false;
            //};
        }


        
        public static void HideAllDimension(this Database db)
        {
            db.GetEntities(null, EN_SELECT.AC_DXF, "DIMENSION");
            ObjectIdCollection hidden_objects = IR.SelectedIds;
            db._setObjectVisible(hidden_objects, false);
        }

        public static void UnhideAll(this Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                ObjectIdCollection _selectedIds = btr.Cast<ObjectId>().Select(id => id).ToCollection();

                db._setObjectVisible(_selectedIds, true);
                tr.Commit();
            }
        }



    }

    //---------------------------------------------------------------

}
