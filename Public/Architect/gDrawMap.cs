using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace AcadScript
{
    public class gDraw2DMap : gDraw2DDCLS
    {
        public cBuilder3D g3D;
        public pPos IMGBasepoint;
        public string IMGPath;
        

        public gDraw2DMap(cReadData css, string _py_filename) : base(css, _py_filename) {
            py_contents = File.ReadAllLines(py_script_path);
        }

       
        public string Id
        {
            get
            {
                string res = null;

                if (System.IO.File.Exists(cReadData.map_ref_img_file))
                {
                    string[] ar = cReadData.map_ref_img_file.filter("\\");
                    res = ar[ar.Length - 2];

                    if (!cReadData.SuperKeyValue("index").et_())
                        res += "$" + cReadData.SuperKeyValue("index");
                }

                return res;
            }
        }


        public virtual void DrawMap()
        {
            ACD.WR("SOP1");
            List<pPos> ls___ = new List<pPos>();

            for (int i = 0; i < g3D.Count; i++)
                if(!g3D[i].MtlName.st_("MT_"))
                    ls___.Add(g3D[i].MtlPoint);
            ACD.WR("SOP2");
            //ACD.WR("T1 {0}", ls___.Count);
            //if(ls___.Count > 0)
            //WriteHtmlCLS.AddVrayProxy("VillaA_obj005", ls___);

            string str_py_points = "    pls = (";
                
            for (int i = 0; i < g3D.Count; i++)
            {
                var itm = g3D[i];
                //ACD.DB.DrawPolyline(itm, true);
                string mtl = itm.MtlName;
                pPos pt_title = itm.MtlPoint;
                
                ACD.WR("SOP2.1");
                
                if (!mtl.et_() && !(itm is gRoadElement))
                {
                    PosCollection _segs = itm.GetSegment(true).OrderBy(_l
                        => -_l.Length(false)).ToCollectionSameClosed(false);

                    //double ____imgsc = cReadData.SuperKeyValue("sc").ToNumber(24) / 100;

                    //if (!mtl.ct_("MT_") && !mtl.ct_("PARK"))
                        //AppendPlanImage("i6-plan-image", itm, mtl, pt_title, -600 / 5, -400 / 5, ____imgsc, ____imgsc);

                    string __category = !mtl.st_("MT_") && !mtl.st_("PARK")
                        && !mtl.st_("ZERO") ? "i7-zone-" + mtl[0] : "i1-" + mtl;
                    ACD.WR("SOP2.2");
                    AppendPolylineHtml(__category, itm, true);

                    //WriteHtmlCLS.AddVrayExtrude(itm, 100, __category + ".jpg|#z1000");

                    RegionInfor region = new RegionInfor(CssData, null);
                    region.Basepoint = IMGBasepoint;
                    region.Points = itm.ToArray();
                    region.Title = pt_title;
                    region.Mtl = mtl;
                    region.DrawHtml();
                    //ACD.WR("SOP2.3 {0}", region.DjangoObject);
                    //str_py_points += "    " + region.DjangoObject + ",\n";
                    ACD.WR("SOP2.4");
                }
                
            }
            ACD.WR("SOP3");
            File.WriteAllLines(py_script_path, this.ReplaceLinesInList("# SAVE DATABASE", "# END DATABASE", 
                new string[] { String.Format("    create_scene_by_id(json_data, scene_id = '{0}', img = '{1}',", Id, IMGPath), str_py_points + "))\n"}));
        }
    }
}

