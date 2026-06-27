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
using AcadScript;

namespace LayerPurge
{
    
    public class ObjectLayerMergeCLS
    {

        [CommandMethod("IBS")]

        public static void LayerPurge()
        {
            ACD.WR("Layercount = {0}", ACD.DB.ListLayers().Length);

            Dictionary<string, string[]> dict = ACD.LoadChapter(DE.INI_FILE, "LAYMRG_SETTING").ToDictionary(s
                => s._firstPropName(), s => s._firstProp().filter(";,"));

            Dictionary<string, string[]> res_dict = new Dictionary<string, string[]>();
            string[] layers = ACD.DB.ListLayers();

            foreach (var itm in dict)
            {
                string mergetolayer = itm.Key;
                string[] layerstomerge = itm.Value;
                
                List<string> ls = new List<string>();

                foreach (string layer in layers)
                    if (layer.Upper() != mergetolayer.Upper())
                    {
                        if (mergetolayer == "0")
                        {
                            if (layerstomerge.Length > 0 && layerstomerge[0].Any(s => layer.StartsWith(s.ToString())))
                                ls.Add(layer);
                        }
                        else if (layerstomerge.Any(s => s.StartsWith("#") ? 
                            layer.st_(s.Upper().Substring(1)) : layer.Upper().Contains(s.Upper())))
                            ls.Add(layer);
                    }

                if (ls.Count > 0)
                    res_dict.Add(mergetolayer, ls.ToArray());
            }


            if(res_dict.Count > 0)
            { 
                foreach(var itm in res_dict)
                    ACD.WR("{0} layers merge to {1}:[{2}]", itm.Value.Length, itm.Key, itm.Value.ToTextStr());

                if (ACD.ED.GetInputString("Do you want merge?(Y/N)").Upper() != "N")
                {
                    foreach (var itm in res_dict)
                    {
                        string mergetolayer = itm.Key;
                        string[] layerstomerge = itm.Value;

                        layers = ACD.DB.ListLayers();

                        if (!layers.Any(s => s.Upper() == mergetolayer.Upper()))
                            ACD.DB.CreateLayer(mergetolayer);

                        List<object> prams = new List<object> { "LAYMRG" };
                        foreach (string layer in layerstomerge)
                            if(layers.Contains(layer))
                            {
                                prams.Add("n");
                                prams.Add(layer);
                            }

                        prams.Add("");
                        prams.Add("n");
                        prams.Add(mergetolayer);
                        prams.Add("y");

                        ACD.ED.Command(prams.ToArray());
                    }
                }

                ACD.WR("Layercount after processed = {0}", ACD.DB.ListLayers().Length);
            }else
            {
                ACD.WR("No layername match");
            }
        }

        [CommandMethod("IBA")]

        async static public void InsertBlockAsync()

        {

            

            await ACD.ED.CommandAsync(

              "_.INSERT", "TEST", Editor.PauseToken, 1, 1, 0

            );



            ACD.WR("\nWe have inserted our block.");

        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.DOC.SendStringToExecute("IBS\n", false, false, true);
                ACD.Focus();
            }
        }
    }
}