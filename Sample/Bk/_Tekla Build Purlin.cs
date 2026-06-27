using System;
using System.Text;
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
    public class SVGOutput
    {
        StringBuilder sb;

        public void Save(string path)
        {
            File.WriteAllText(path, sb.ToString());
        }

        public string GenerateHTML(PosCollection pls)
        {
            sb = new StringBuilder();

            // Bắt đầu tạo nội dung HTML
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("\t<style>");
            sb.AppendLine("\t\t.myLine { stroke: black; stroke-width: 20; }");
            sb.AppendLine("\t\t.svg-container {transform:scaleX(0.1) scaleY(-0.1) translate(-30000px, 330000px)}");
            sb.AppendLine("\t</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("\t<div class = 'svg-container'>");
            sb.AppendLine("\t\t<svg width=\"120000\" height=\"65000\" viewBox = \"0 0 120000 65000\">"); // Kích thước SVG

            int i = 0;
            // Tạo các đường line từ tập hợp điểm 2D
            foreach (var pts in pls)
            {
                if (pts.Length > 2)
                {
                    string k = pts[0].Content;
                    pts[0].Content = null;

                    sb.AppendLine(String.Format("\t\t\t<polyline name=\"{0}\" points =\"{1}\" class=\"myLine\" />",
                        k, pts.ToText(false, ' ').Replace("[","").Replace("[", "")

                        ));
                }
                else
                {
                    i++;

                    pPos start = pts[0];
                    pPos end = pts[1];

                    sb.AppendLine(String.Format("\t\t\t<line name=\"{0}\" x1=\"{1}\" y1=\"{2}\" z1=\"{3}\" x2=\"{4}\" y2=\"{5}\" z2=\"{6}\" class=\"myLine\" />",
                        start.Content.et_() ? Math.Floor((double)i / 12).ToString() : start.Content,
                        start.X.roundNumber(), start.Y.roundNumber(), start.Z.roundNumber(),
                        end.X.roundNumber(), end.Y.roundNumber(), end.Z.roundNumber()

                        ));
                }
            }

            sb.AppendLine("\t\t</svg>");
            sb.AppendLine("\t</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }

    public class TeklaBuildCLS
    {
        static PosCollection _read_tekla_block(ObjectIdCollection selIds, 
            string keyword, int order_axis, Func<pPos[], PosCollection> fn)
        {
            pPos basept = null;

            foreach (ObjectId id in selIds)
                if (ACD.DB._getIdName(id).st_(keyword))
                {
                    basept = ACD.DB._getPoint(id);
                    break;
                }

            PosCollection res = new PosCollection();
            girts = new PosCollection();

            foreach (ObjectId id in selIds)
                if (ACD.DB._isBlock(id))
                {
                    if (ACD.DB._getIdName(id).st_(keyword))
                    {
                        ACD.DB.BlockEntitiesAction(id, (_ids) =>
                        {
                            var _new_ids = _ids.ToList().OrderBy(_id => ACD.DB._getBound(_id).CenterPoint()[order_axis]);
                            
                            foreach(ObjectId __id in _new_ids)
                                res.AddRange(fn(ACD.DB._getVertices(__id).Move(basept.Invert)));
                        });
                    }
                    //if (ACD.DB._getIdName(id).st_("hold_purlin"))
                    //{
                    //    ACD.DB.BlockEntitiesAction(id, (_ids) =>
                    //    {
                    //        foreach (ObjectId _id in _ids.ToList().OrderBy(_id => ACD.DB._getBound(_id)[0].Y))
                    //        {
                    //            pPos[] ls = ACD.DB._getVertices(_id);

                        //            var y1 = (ls[0].Y - basept.Y).roundNumber();
                        //            var y2 = (ls[1].Y - basept.Y).roundNumber();

                        //            res.Add(new pPos[] { new pPos(ls[0].X  - basept.X, y1, 0, "lock"),
                        //                    new pPos(ls[0].X - basept.X, y2, 0, "lock") });
                        //        }
                        //    });

                        //}
                        //else if (ACD.DB._getIdName(id).st_("purlin"))
                        //{
                        //    ACD.DB.BlockEntitiesAction(id, (_ids) =>
                        //    {
                        //        foreach (ObjectId _id in _ids.ToList().OrderBy(_id => ACD.DB._getBound(_id)[0].Y))
                        //        {
                        //            pPos[] ls = ACD.DB._getVertices(_id);

                        //            for (int i = 0; i < Math.Ceiling(ls.Length(false) / 12000); i++)
                        //            {
                        //                var y = (ls[0].Y - basept.Y).roundNumber();

                        //                res.Add(new pPos[] { new pPos(i * 12000, y),
                        //                        new pPos((i + 1) * 12000, y) });
                        //            }
                        //        }
                        //    });
                        //}
                        //else if (ACD.DB._getIdName(id).st_("sag_angle"))
                        //{
                        //    ACD.DB.BlockEntitiesAction(id, (_ids) =>
                        //    {
                        //        foreach (ObjectId _id in _ids)
                        //        {
                        //            pPos[] ls = ACD.DB._getVertices(_id).Move(basept.Invert).ToArray();
                        //            ls[0].Content = "sag_angle";
                        //            res.Add(ls);

                        //        }
                        //    });
                        //}
                }

            PosCollection sagrods = new PosCollection();

            for (int i = 0; i < res.Count; i++)
                if (res[i][0].Content == "sagrod")
                {
                    List<pPos> _intpts = res[i].ToList();

                    foreach (pPos[] ss in res)
                        _intpts.AddRange(res[i].IntersectPts(ss, false, false));

                    _intpts = _intpts.OrderBy(p => p.Y).ToList();

                    foreach (pPos _p in _intpts)
                        _p.Content = "sagrod";
                                        
                    for (int j = 0; j < _intpts.Count - 1; j++)
                        sagrods.Add(new pPos[] { _intpts[j], _intpts[j + 1] });
                    
                    res[i][0].Content = null;
                }

            res = res.Where(__ls => !__ls[0].Content.et_()).ToCollectionSameClosed(false);
            res.AddRange(sagrods);

            return res;
        }

        static bool _is_axis(pPos[] ls, int axis)
        {
            return Math.Abs(ls[0][axis] - ls[1][axis]) < 10;
        }

        static PosCollection girts;

        static PosCollection _read_girts(pPos[] ls)
        {
            PosCollection res = new PosCollection() { ls.ToList().ToArray() };

            if (ls.Length >= 4)
            {
                res[0][0].Content = ls.Area() < 1000000 ? "sagangle" : "dummy";
            }
            else if (_is_axis(ls, 1))
            {
                girts.Add(ls);

                var y = ls[0].Y;
                int total = (int)Math.Ceiling(ls.Length(false) / 12000);

                for (int i = 0; i < total; i++)
                    res.Add(new pPos[] { new pPos(i * 12000, y), new pPos((i + 1) * 12000, y) });

                if (ls.Length(false) - total * 12000 > 10)
                    res.Add(new pPos[] { new pPos((total - 1) * 12000, y), new pPos(total * 12000, y) });

                foreach (pPos[] __ls in res)
                    __ls[0].Content = "girt";
            }
            else if (_is_axis(ls, 0))
                res[0][0].Content = "sagrod";

            return res;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    var svg = new SVGOutput();
                    string key = "girt";

                    PosCollection pls = _read_tekla_block(selIds, key, 1, _read_girts);
                                        
                    svg.GenerateHTML(pls);
                    svg.Save(@"D:\html\tekla_" + key + ".html");
                }
            }
                
            ACD.Focus();
        }
    }
}

