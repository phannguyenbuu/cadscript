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
    public class TextSizeCLS
    {
        static void TextStyleIterator(string fontname)
        {
            Database database = HostApplicationServices.WorkingDatabase;
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                SymbolTable symTable = (SymbolTable)transaction.GetObject(database.TextStyleTableId, OpenMode.ForWrite);
                foreach (ObjectId id in symTable)
                {
                    TextStyleTableRecord symbol = (TextStyleTableRecord)transaction.GetObject(id, OpenMode.ForWrite);

                    symbol.Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor(fontname, 
                        symbol.Font.Bold, symbol.Font.Italic, symbol.Font.CharacterSet, symbol.Font.PitchAndFamily);
                    //TODO: Access to the symbol
                    //MgdAcApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage(string.Format("\nName: {0}", symbol.Name));
                }

                transaction.Commit();
            }
        }

        static void _replaceAllText(ObjectIdCollection ids, string[] contents)
        {
            foreach(ObjectId id in ids)
            {
                if(ACD.DB._isBlock(id))
                {
                    ObjectIdCollection subIds = ACD.DB.GetEntInBlockByName(ACD.DB._getIdName(id));
                    _replaceAllText(subIds, contents);
                }else if(ACD.DB._isText(id))
                {
                    
                    foreach (string content in contents)
                        ACD.DB._setContent(id, 
                            ACD.DB._getContent(id).Replace(content._firstPropName(), content._firstProp()));
                }
            }    
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection().ToList()
                    .Where(id => ACD.DB._isText(id))
                    .OrderBy(id => - ACD.DB._getPoint(id).Y.roundNumber(10))
                    .ThenBy(id => ACD.DB._getPoint(id).X).ToCollection();


                if (selIds.Count > 0)
                {
                    ACD.WR("Content:{0}", ACD.DB._getContent(selIds[0]));
                    string mode = ACD.ED.GetInputString("Merge all texts ?(Y/N", "N");


                    if (mode.Upper() == "Y")
                    {
                        ObjectIdCollection ids = new ObjectIdCollection();
                        string content = "";
                        
                        for (int i = 0; i < selIds.Count; i++)
                        {
                            if(i != 0) ids.Add(selIds[i]);
                            content += ACD.DB._getContent(selIds[i]) + "\\P";

                        }

                        ACD.DB.EraseObjects(ids);

                        ACD.DB._setContent(selIds[0], content);

                        
                        selIds = new ObjectIdCollection { selIds[0] };
                    }

                    string st = "";
                    for (int i = 0; i < selIds.Count; i++)
                    {
                        string content = ACD.DB._getContent(selIds[i]);
                        ACD.WR("Content:{0}", content);
                        st += " " + content;
                        ACD.DB._setContent(selIds[i], content);
                        ACD.DB._setCurrentStyle(selIds[i]);
                    }

                }

                ACD.Focus();
            }
        }
    }
}

