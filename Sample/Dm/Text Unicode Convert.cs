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


//using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class TextUnicodeConvertCLS
    {
        static string[] vniToUnicodeMap = new string[]
        {
            // Ví dụ một số ký tự phổ biến trong bảng mã VNI Windows
             "aø|à" ,  "aù|á" ,  "aû|ả" ,  "aõ|ã" ,  "aï|ạ" ,
             "âø|ầ" ,  "âù|ấ" ,  "âû|ẩ" ,  "âõ|ẫ" ,  "âï|ậ" ,
             "aê|ă" ,  "aè|ằ" ,  "aé|ắ" ,  "aú|ẳ" ,  "aù|ẵ" ,  "aï|ặ" ,
             "eø|è" ,  "eù|é" ,  "eû|ẻ" ,  "eõ|ẽ" ,  "eï|ẹ" ,
             "êø|ề" ,  "êù|ế" ,  "êû|ể" ,  "êõ|ễ" ,  "êï|ệ" ,
             "oø|ò" ,  "où|ó" ,  "oû|ỏ" ,  "oõ|õ" ,  "oï|ọ" ,
             "ôø|ồ" ,  "ôù|ố" ,  "ôû|ổ" ,  "ôõ|ỗ" ,  "ôï|ộ" ,
             "öø|ờ" ,  "öù|ớ" ,  "öû|ở" ,  "öõ|ỡ" ,  "öï|ợ" ,
             "uø|ù" ,  "uù|ú" ,  "uû|ủ" ,  "uõ|ũ" ,  "uï|ụ" ,
             "öø|ừ" ,  "öù|ứ" ,  "öû|ử" ,  "öõ|ữ" ,  "öï|ự" ,
             "OÁ|ố", "ÖÔ|,ươ"
            // Thêm các ký tự khác theo yêu cầu
        };


        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection ids = ACD.GetSelection()._filterDXF("MTEXT","TEXT","MULTILEADER");
                ACD.WR("OK:{0}", ids.Count);

                //Dictionary<string, string> dicts = vniToUnicodeMap
                //    .ToDictionary(s => s.filter("|")[0], s => s.filter("|")[1]);

                foreach (ObjectId txtId in ids)
                {
                    string unicodeText = db._getContent(txtId).Upper();
                    
                    foreach (string __s in vniToUnicodeMap)
                    {
                        unicodeText = unicodeText.Replace(__s.filter("|")[0].Upper(), __s.filter("|")[1].Upper());
                    }

                    db._setContent(txtId, unicodeText);
                    ACD.WR("OK {0}", unicodeText);
                }

                ACD.Focus();
            }
        }
    }
}

