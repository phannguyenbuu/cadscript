using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Internal;
//using SyncObject;

namespace AcadScript
{
    public class CurrentAnnoScaleCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                var scale_list = pString.INI_String("SCALE_LIST").filter(";").ToDictionary(s 
                    => s.filter("(").First(), s => s._getInComma());
                PosCollection regions = ACD.DB.GetDrawingZones();
                double page_w = pString.INI_String("PAGEWIDTH").ToNumber();
                double page_h = pString.INI_String("PAGEHEIGHT").ToNumber();
                    
                        pPos[] zone = ACD.DB.GetDrawingZone(ACD.bRect.CenterPoint());
                        //ACD.DB.DrawPolyline(zone.Rect(), true);
                        string scale_name = zone.BoundScale(page_w, page_h);

                //string key = cbScale.SelectedItem.ToString();
                //ACD.DB.ApplyAnno(key);
                ACD.DB.SetAnnotationScale(scale_name);
                //ACD.ED.Regen();
                //int index = scale_list.Keys.ToList().FindIndex(itm => itm == key);

                //if (index != -1)
                //{
                //    string display = scale_list.Values.ElementAt(index);
                //    string[] display_list = ACD.ListDisplayConfig();
                //    index = display_list.Cast<string>().ToList()
                //        .FindIndex(s => s.st_(display.Upper()));

                //    //if (index != -1)
                //        //cbDisplay.SelectedIndex = index;
                //}

                ACD.Focus();
                
            }
        }
    }
}

