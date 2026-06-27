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
    public class StructuralTextFromBeamCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                ObjectIdCollection cirIds = selIds.Cast<ObjectId>()
                    .Where(id => ACD.DB._isCircle(id) && ACD.DB._getRadius(id) <= 20).ToCollection();

                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add(" ɸ ", "ɸ");
                dict.Add("ɸ ", "ɸ");
                dict.Add(" ɸ", "ɸ");
                
                string txt = ACD.ED.GetInputString("Enter text:");
                //double txt_h = ACD.ED.GetInputString("Enter text height (200):").ToNumber(200);

                foreach (ObjectId id in selIds)
                    if (ACD.DB._isText(id) || ACD.DB._isDim(id))
                    {
                        string content = ACD.DB._getContent(id);

                        content = content.Replace(dict);
                        ACD.DB._setContent(id, content);

                        if (content.Contains("ɸ"))
                            ACD.DB._setTextSize(id, 90);

                        if (content.Length == 1)
                            ACD.DB._setTextAlignment(id, AttachmentPoint.MiddleCenter);
                    }
                    else if (ACD.DB._getLayer(id).Upper().Contains("TEXT"))
                    {
                        ACD.DB._setLayer(id, pString.INI_String("DIM_LAYER"));
                    }else if(ACD.DB._isPolyline(id))
                    {
                        pPos[] bb = ACD.DB._getBound(id);
                        pPos pt = bb.CenterPoint();
                        pPos sz = bb.Size();

                        ObjectId txtId = ACD.DB.CreateText(txt, pt, 2, 0, "ANNO=ON");
                        if (sz.X < sz.Y)
                            ACD.DB._setRotation(txtId, 90);
                    }

                foreach (ObjectId id in cirIds)
                    ACD.DB.DrawHatchFromIds(new ObjectIdCollection() { id }, "HPATTERN=SOLID");
            }
            ACD.Focus();
        }
    }
}

