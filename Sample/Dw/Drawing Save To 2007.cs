//using System;
using System.Collections.Generic;
//using System.Linq;
using System.IO;
using Autodesk.AutoCAD.DatabaseServices;

//fileIn Cls.IReader
//fileIn Cls.MethodEntity
//fileIn Cls.EDSelection
//fileIn Cls.ACD
//fileIn Cls.IS
//fileIn Cls.IBlock
//fileIn Cls.IAnno
//fileIn Cls.IDimChain

namespace AcadScript
{
    public class DrawingSaveTo07CLS
    {
        


        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                string[] files = Directory.GetFiles(Path.GetDirectoryName(ACD.CurrentDWGPath), "*.dwg");
                List<string> prams = new List<string>();
                prams.Add("Title=Current files in project");

                string dir = Path.GetDirectoryName(ACD.CurrentDWGPath) + @"\exp\";
                Directory.CreateDirectory(dir);

                foreach (string f in files)
                {
                    string newname = dir + Path.GetFileNameWithoutExtension(f) + "(2007).dwg";
                    ACD.OpenDWG(f);
                    using (Database db = ACD.DB)
                    {
                        //db.GetEntities(null, EN_SELECT.AC_DXF, "AEC_WALL", "AEC_DOOR", "AEC_WINDOW");
                        //db.ExplodeAEC(IR.SelectedIds);

                        ACD.SaveAs2007(db, newname);
                    }
                    
                }
                //res.Add(newname);

                //ACD.WR("OK2");

                //prams.Add("File=" + f);


                //ACD.WR("OK3");
                //ZipFile.CreateFromDirectory(zipdir, dir + (Path.GetFileNameWithoutExtension(ACD.CurrentDWGPath)) + ".zip");
                ///ACD.WR("OK4");
                //Directory.Delete(zipdir, true);

                //DV.ViewParamData(prams);
                //System.Windows.Forms.Clipboard.SetText(newname);
                ACD.Focus();
            }
        }
    }
}

