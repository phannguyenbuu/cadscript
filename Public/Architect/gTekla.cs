using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AcadScript
{


    public class gDraw2DTekla : gDraw2DDCLS
    {


        pPos[] __plate_sizes = new pPos[] { new pPos(150, 150, 10, "EB-003"), 
            new pPos(200, 200, 12, "EB-001"), 
            new pPos(250,150,10, "EB-002"), 
            new pPos(220,760, 20, "AB-001"),
            new pPos(300,760, 20, "AB-001")};

        public gDraw2DTekla(cReadData css, string _py_filename) : base(css, _py_filename) { }

        public pPos _isEBPlate(pPos[] pts)
        {
            pPos sz = pts.Size().Round(10);
            return __plate_sizes.FirstOrDefault(__sz => sz.X == __sz.X && sz.Y == __sz.Y);
        }
        public void DrawPlate()
        {
            CssData.PointList = CssData.PointList.Where(__ls => _isEBPlate(__ls) != null).ToCollectionSameClosed(true);
            pPos[] bb = CssData.PointList.Boundary;

            double H = - bb.Size().Y.roundNumber(0.01);
            //ACD.WR("H:{0}", bb.Size().Y.roundNumber(0.01));

            AppendPolylineHtml("t0-tekla-baseline", 
                new pPos[] { new pPos(bb[0].X , H), new pPos(bb[1].X,  H) });

            foreach (pPos[] __ls in CssData.PointList)
            {
                var __p = _isEBPlate(__ls);

                AppendPolylineHtml("t0-tekla-embed-plate-" + __p.Content, __ls.Move(new pPos(0, H)));
                AppendPolylineHtml("t0-tekla-embed-plate-grid", 
                    new pPos[] { new pPos(bb[0].X, __p.Y + H), new pPos(bb[1].X, __p.Y + H)});

                pPos[] __bs = __ls.Boundary();
                pPos[] _r = new pPos((__bs[0].X + __bs[1].X) / 2 - __p.X / 2, __bs[0].Y).Rect(__p.X, __p.Y);

                WriteHtmlCLS.AddVrayExtrude(_r, __p.Z, __p.Content);
            }
        }
    }
}
