using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Autodesk.AutoCAD.DatabaseServices;

namespace AcadScript
{
    class pObj
    {
        public pPos Pos, Color;
        public pPos[] Bound;
        public string id, name, type;
        
        string _gValue(string st, string key)
        {
            string res = null;
            string[] ar = st.filter(" ");
            string v = ar.FirstOrDefault(s => s.st_(key + "=\""));

            if(!v.et_())
            {
                res = v._firstProp().Replace("\"","");
            }

            return res;
        }

        public pObj(string s)
        {
            Pos = pPos.FromString(_gValue(s, "pos"));
            Color = pPos.FromString(_gValue(s, "color"));
            Bound = new PosCollection(_gValue(s, "bound")).FirstOrDefault();
            id = _gValue(s, "id");
            name = _gValue(s, "name");
            type = _gValue(s, "type");
        }

        public override string ToString()
        {
            return String.Format("type {0} id {1} name {2}", type,id,name);
        }

        public static void _setLineworkColor(ObjectId id, byte r, byte g, byte b)
        {
            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                
                if (ent != null)
                    ent.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(r,g,b);
                tr.Commit();
            }
        }

        public static ObjectIdCollection FromString(string[] contents, pPos basept)
        {
            List<pObj> objs = new List<pObj>();
            ObjectIdCollection res = new ObjectIdCollection();

            foreach (string s in contents)
            {
                if (s.st_("<DIV "))
                {
                    pObj obj = new pObj(s);

                    ObjectIdCollection newIds = new ObjectIdCollection();
                    newIds.Add(ACD.DB.Draw2D((obj.Bound[0] + basept)
                            .RectToPoint((obj.Bound[1] + basept)), "c"));

                    //newIds.AddRange(ACD.DB.DrawCircle(obj.Pos + basept, 100));

                    //newIds.Add(ACD.DB.CreateText("#M" 
                    //    + obj.name.Replace(".","\r\n").Replace("_", "\r\n")
                    //    + "\r\n[" + obj.id + "]",obj.Pos + basept,
                    //    0.0005 * (Math.Min(obj.Bound.Size().X,obj.Bound.Size().Y))));


                    foreach (ObjectId id in newIds)
                        _setLineworkColor(id, (byte)obj.Color.X, (byte)obj.Color.Y, (byte)obj.Color.Z);

                    if(!ACD.DB.HasBlock(obj.name).IsNull)
                    {
                        ACD.DB.GetEntities(null, EN_SELECT.AC_DXF_AND_NAME, "INSERT", obj.name);
                        ACD.DB.EraseObjects(IR.SelectedIds);
                        ACD.DB.PurgeBlock();
                    }

                    string blockname = ACD.DB.uniqueBlockName(obj.name);

                    //ACD.DB.NewBlock(newIds, blockname, true,false, obj.Pos + basept);
                    

                    //res.Add(ACD.DB.Insert(blockname, obj.Pos + basept));
                }
            }

            return res;
        }
    }
    
    public class CadLibImportCLS
    {
        static double ROUND_VALUE = 10;
                
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                string str = System.Windows.Forms.Clipboard.GetText();
                pPos pt = ACD.GetPoint();

                //pPos[] zone = ACD.DB.GetDrawingZone(pt);

                if (pt != null)
                {
                    if (File.Exists(@"D:\html\obj.html"))
                        pObj.FromString(File.ReadAllLines(@"D:\html\obj.html"), pt);
                    
                    PrgImportIText imp = new PrgImportIText(pt);

                    imp.SourceViewport = new PosCollection();
                    imp.DrawData(str.filter("\r\n"), ROUND_VALUE);

                    for (int i = 0; i < imp.NoteString.Count; i++)
                        imp.NoteString[i] = "(" + (i + 1) + "):" + imp.NoteString[i];

                    if (imp.ResultIds.Count > 0)
                    {
                        pPos mv = ACD.DB._getBound(imp.ResultIds)[0];
                        ACD.DB.MoveObject(imp.ResultIds, pt - mv);
                    }
                    else
                        ACD.WR("Nothing imported!");
                }

                ACD.Focus();
            }
        }
    }
}