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
    public class AECDimensionCLS
    {
        static ObjectIdCollection dimWithInids;
        static ObjectId lineId;
        static pPos dimPoint;
        static int axis;

        static void _setPointView(pPos pt)
        {
            int viewPortNumber = Convert.ToInt32(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("cvport").ToString());
            //var ptOffset = ACD.ED.PointToScreen(pt.ToPoint3(), viewPortNumber);
            //var windowLocation = ACD.DOC.Window.DeviceIndependentLocation;
            //Cursor.Position = new System.Drawing.Point((int)(windowLocation.X + ptOffset.X), (int)(windowLocation.Y + ptOffset.Y));
        }

        static ObjectId _maxLengthId(ObjectIdCollection ids)
        {
            PosCollection pls = ids.ToList().Select(id => ACD.DB._getVertices(id)).ToCollectionSameClosed();
            int[] indexes = DE.NumericArray(0, pls.Count - 1).OrderBy(n 
                => - pls[n][0].DistanceTo(pls[n][1])).ToArray();

            return ids[indexes[0]];
        }

        [CommandMethod("IBS")]

        public static void AECDimensionBuild()
        {
            //ACD.WR("D");
            if (dimWithInids.Count > 0 && dimPoint != null)
            {
                //ACD.WR("D0");
                
                
                //ACD.WR("D1");      

                ObjectIdCollection blockIds = dimWithInids.ToList().Where(id => ACD.DB._isPoint(id)).ToCollection();
                ObjectIdCollection objIds = dimWithInids.ToList().Where(id => !ACD.DB._isPoint(id)).ToCollection();
                pPos[] bb = ACD.DB._getBound(objIds);
                _setPointView(dimPoint);

                //ACD.WR("D2");

                //foreach (ObjectId id in blockIds)
                //{
                //    List<pPos> ls = ACD.DB._getBound(id).ToList();
                //    ls.Insert(0, ACD.DB._getPoint(id));
                //    objIds.Add(ACD.DB.DrawPolyline(ls, false, "LAYER=DEFPOINTS"));
                //}

                var ss = SelectionSet.FromObjectIds(objIds.ToArray());

                List<object> prams = new List<object> { "DIMADD" };
                
                prams.Add(ss);
                prams.Add("");

                prams.Add("r");
                prams.Add(axis == 0 ? 0: 90);
                //prams.Add(SelectionSet.FromObjectIds(new ObjectId[] { new ObjectId((IntPtr)140690086640640) }));

                prams.Add(dimPoint.X + "," + dimPoint.Y);
                //prams.Add("");
                
                ACD.ED.Command(prams.ToArray());
            }
        }

        static double RANGE = 2000;

        static ObjectIdCollection validIds(ObjectIdCollection ids)
        {
            return ids._filterDXF("AEC_WALL", "INSERT", "CIRCLE", "ELLIPSE", "LWPOLYLINE").ToList()
                    .Where(id => ACD.DB._getLayer(id).Upper() != "DEFPOINTS"
                    && !ACD.DB._isArray(id) && !(ACD.DB._isBlock(id) 
                    && ACD.DB._getIdName(id).StartsWith("grid"))).ToCollection();
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                RANGE = pString.INI_String("DIM_EFFECT_RANGE").ToNumber(2000);
                //dimWithInids = ACD.GetSelection();

                //while (true)
                {
                    dimPoint = null;
                    dimPoint = ACD.GetPoint();

                    if (dimPoint != null)
                    {
                        axis = ACD.ControlHold ? 1 : 0;

                        pPos[] zone = ACD.DB.GetDrawingZone(dimPoint);

                        if (zone == null)
                        {
                            ACD.WR("Please select drawing zone... Function exit");
                        }
                        else
                        {
                            ACD.DB.GetEntities(zone, EN_SELECT.AC_ALL);
                            ObjectIdCollection ids = validIds(IR.SelectedIds);

                            PosCollection pls = ids.ToList().Select(id => ACD.DB._getBound(id)).ToCollectionSameClosed();

                            int nex = (axis + 1) % 2;
                            double val = dimPoint[nex];

                            int[] indexes = DE.NumericArray(0, pls.Count - 1).Where(n
                                => !(pls[n].Boundary()[0][nex] > val + RANGE 
                                || pls[n].Boundary()[1][nex] < val - RANGE)).ToArray();

                            dimWithInids = indexes.Select(n => ids[n]).ToCollection();
                            //foreach(ObjectId id in dimWithInids)
                            //ACD.DB.DrawPolyline(ACD.DB._getBound(id), false);

                            //ACD.WR("Dimpoint: {0} Axis:{1} Objects: {2}", dimPoint, axis, dimWithInids.Count);
                        }

                        //ACD.DOC.SendStringToExecute("IBS\n", false, false, true);
                    }
                }
                ACD.Focus();
            }
        }
    }
}

