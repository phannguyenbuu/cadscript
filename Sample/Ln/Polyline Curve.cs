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

using MessagingToolkit;
using System.Windows.Forms;
using SyncObject;

namespace AcadScript
{
    public class PolylineCurveCLS
    {
        [CommandMethod("TestFitCurve")]

        static void FitCurve(Database db, ObjectId lwpId)

        {
            
            ObjectId ModelSpaceId =

                    SymbolUtilityServices.GetBlockModelSpaceId(db);



            using (Transaction Tx = db.TransactionManager.StartTransaction())

            {

                Autodesk.AutoCAD.DatabaseServices.Polyline plineFit =

                    new Autodesk.AutoCAD.DatabaseServices.Polyline();

                Autodesk.AutoCAD.DatabaseServices.Polyline pline =

                    (Autodesk.AutoCAD.DatabaseServices.Polyline)Tx.GetObject(lwpId, OpenMode.ForRead);

                {



                    Polyline2d poly2d = pline.ConvertTo(false);

                    poly2d.CurveFit();

                    plineFit.ConvertFrom(poly2d, false);

                    poly2d.Dispose();



                    BlockTableRecord record = Tx.GetObject(ModelSpaceId,

                                      OpenMode.ForWrite) as BlockTableRecord;

                    plineFit.ColorIndex = 1;//red

                    record.AppendEntity(plineFit);

                    Tx.AddNewlyCreatedDBObject(plineFit, true);

                    Tx.Commit();

                }



            }

        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection selIds = EDSelection.GetSelection();

                foreach(ObjectId lwpId in selIds)
                {
                    FitCurve(ACD.DB, lwpId);
                }
                ACD.Focus();
            }
        }
    }
}

