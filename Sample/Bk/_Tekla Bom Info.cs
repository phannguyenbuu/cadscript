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
using AEC = Autodesk.Aec.Arch.DatabaseServices;

namespace AcadScript
{
    public class TeklaObjectLayerCLS
    {
        static double total_weight = 0;
        static double total_area = 0;

        static string[] _getInfo(ObjectIdCollection selIds)
        {
            PosCollection pls = new PosCollection();

            pPos[] titles = selIds.ToList().Where(__id => ACD.DB._isText(__id)).Select(__id => ACD.DB._getPoint(__id)).ToArray();
            Dictionary<string, string> dicts = new Dictionary<string, string>();

            foreach (ObjectId id in selIds)
            {
                if (ACD.DB._isPolyline(id))
                {
                    pPos[] pts = ACD.DB._getVertices(id);
                    //ACD.WR("PTS {0},{1}", titles.ToText(), pts.ToText());
                    pPos[] __pts = titles.Where(_p => _p.Inside(pts)).ToArray();

                    if(__pts.Length == 0)
                        __pts = titles.Where(_p => _p.DistanceToPts(pts) < 1).ToArray();
                    
                    foreach (pPos p in __pts)
                    {
                        pts[0].Content = p.Content;

                        string[] __ls = p.Content.filter("/");

                        if (__ls.Length > 1)
                        {
                            string _key = __ls[0];
                            string _depth = __ls[1];

                            pPos sz = pts.Size();

                            if (_depth.st_("P"))
                            {
                                sz.Z = _depth.Substring(1).ToNumber();
                                double _weight = sz.X * sz.Y * sz.Z * 7850 / 1000000000;
                                double _area = (pts.Area() * 2 + pts.Length(true) * sz.Z) / 1000000;

                                if (!dicts.ContainsKey(_key))
                                    dicts.Add(_key,
                                       String.Format("Qu={0}|Index={1}|Name=RM-HR{2}x{3}x{4}|Weight={5}|TotalWeight={6}|Area={7}|Mtl={8}",
                                       1, _key,
                                       _depth, sz.X.roundNumber(), sz.Y.roundNumber(0.01),
                                       _weight.roundNumber(0.01), "0", _area.roundNumber(0.01), "Q345B"));
                                else
                                    dicts[_key] = dicts[_key]._setprop("Qu", (dicts[_key]._prop("Qu").ToNumber() + 1).ToString());
                            }
                            else if (_depth.st_("ROD-"))
                            {
                                sz.Z = _depth.Substring(4).ToNumber();
                                double _weight = pts.Length(false) * 3.14 * (sz.Z / 2) * (sz.Z / 2) * 7850 / 1000000000;
                                double _area = (pts.Length(false) + sz.Z / 2) * 2 * 3.14 * (sz.Z / 2) / 1000000;
                                //2 * math.pi * r * (r + h)
                                if (!dicts.ContainsKey(_key))
                                    dicts.Add(_key,
                                        String.Format("Qu={0}|Index={1}|Name=RM-{2}-{3}|Weight={4}|TotalWeight={5}|Area={6}|Mtl={7}",
                                        1, _key, _depth, pts.Length(false).roundNumber(),
                                        _weight.roundNumber(0.01), "0", _area.roundNumber(0.01), "SS400"));
                                else
                                    dicts[_key] = dicts[_key]._setprop("Qu", (dicts[_key]._prop("Qu").ToNumber() + 1).ToString());
                            }
                            else if (_depth.st_("L"))
                            {
                                string[] ar = _depth.Substring(1).filter("x");
                                sz.Y = ar[1].ToNumber() + ar[2].ToNumber();
                                sz.Z = ar[0].ToNumber();

                                //ACD.WR("Size {0}", sz);

                                double _weight = sz.X * sz.Y * sz.Z * 7850 / 1000000000;
                                double _area = (sz.X * sz.Y * 2 + 2 * (sz.X + sz.Y) * sz.Z) / 1000000;

                                if (!dicts.ContainsKey(_key))
                                    dicts.Add(_key,
                                       String.Format("Qu={0}|Index={1}|Name=RM-{2}-{3}|Weight={4}|TotalWeight={5}|Area={6}|Mtl={7}",
                                       1, _key, _depth, sz.X.roundNumber(),
                                       _weight.roundNumber(0.01), "0", _area.roundNumber(0.01), "Q345B"));
                                else
                                    dicts[_key] = dicts[_key]._setprop("Qu", (dicts[_key]._prop("Qu").ToNumber() + 1).ToString());
                            }
                        }
                    }
                }
            }

            total_weight = 0;
            total_area = 0;

            for (int i = 0; i < dicts.Keys.Count; i++)
            {
                string _key = dicts.Keys.ElementAt(i);

                int qu = (int)dicts[_key]._prop("Qu").ToNumber();
                double w = dicts[_key]._prop("Weight").ToNumber();
                double r = dicts[_key]._prop("Area").ToNumber();

                dicts[_key] = dicts[_key]
                    ._setprop("TotalWeight", (qu * w).ToString())
                    ._setprop("Area", (qu * r).ToString()).ToString();

                total_weight += qu * w;
                total_area += qu * r;
            }

            return dicts.OrderBy(itm => itm.Key).Reverse().Select(itm => itm.Value).ToArray();
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                ACD.WR("Format as S-[Index]/P[Depth] or P-[Index]/ROD[Depth] or P-[Index]/L[Depth]");

                string[] infors = _getInfo(selIds);

                ACD.WR(infors.ToTextStr("\n"));
                System.Windows.Forms.Clipboard.SetText(infors.ToTextStr("\n"));

                ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "INSERT");
                ObjectIdCollection ids = new ObjectIdCollection();

                foreach(ObjectId id in IR.SelectedIds)
                    if (ACD.DB._getIdName(id).st_("Unknown"))
                        try
                        {
                            pPos sz = ACD.DB._getBound(id).Size();
                            if (sz.X >= 110 && sz.X <= 120 && sz.Y >= 18)
                                ids.Add(id);
                        }catch(System.Exception ex)
                        {
                            ACD.WR("Show errors {0}", ACD.DB._getIdName(id),ex.StackTrace, ex.Message);
                            continue;
                        }

                ACD.WR("Sel {0}", ids.Count);
                                
                foreach (ObjectId ___id in ids)
                {
                    pPos base_pt = ACD.DB._getBound(___id)[0];

                    ACD.DB.BlockEntitiesEdit(___id, (___ids) =>
                    {
                        ObjectIdCollection txtIds = ___ids.ToList().Where(__tid => ACD.DB._isText(__tid)).ToCollection();
                        pPos[] txtPts = txtIds.ToList().Select(__tid => ACD.DB._getPoint(__tid)).ToArray();

                        var _redTxts = txtIds.ToList().Where(_tid => ACD.DB._getColorIndex(_tid) == 1).OrderBy(_tid => ACD.DB._getPoint(_tid).X).ToCollection();
                        ACD.DB._setContent(_redTxts[0], total_weight.roundNumber(0.01).ToString());
                        ACD.DB._setContent(_redTxts[1], total_area.roundNumber(0.01).ToString());

                        var _whiteTxts = txtIds.ToList().Where(_tid => ACD.DB._getColorIndex(_tid) == 7).ToCollection();

                        foreach (ObjectId _tid in _whiteTxts)
                        {
                            int n = (int)Math.Floor((ACD.DB._getPoint(_tid).Y - base_pt.Y) / 4);
                            //ACD.WR("T{0}", n);

                            if (n > 0)
                            {
                                string st = infors[n - 1];

                                var txts = _whiteTxts.ToList()
                                            .Where(_t => n == (int)Math.Floor((ACD.DB._getPoint(_t).Y - base_pt.Y) / 4))
                                            .OrderBy(_t => ACD.DB._getPoint(_t).X).ToCollection();

                                ACD.DB._setContent(txts[0], st._prop("Qu"));
                                ACD.DB._setContent(txts[1], st._prop("Index"));
                                ACD.DB._setContent(txts[2], st._prop("Name"));
                                ACD.DB._setContent(txts[3], st._prop("Weight"));
                                ACD.DB._setContent(txts[4], st._prop("TotalWeight"));
                                ACD.DB._setContent(txts[5], st._prop("Area"));
                                ACD.DB._setContent(txts[6], st._prop("Mtl"));
                            }
                        }
                    });
                }

                ACD.ED.Regen();
            }
                
            ACD.Focus();
        }
    }
}

