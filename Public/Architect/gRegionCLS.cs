using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace AcadScript
{
    public class RegionInfor : gDraw2DDCLS
    {
        public pPos[] Points;
        public pPos Title, Basepoint;
        public string Mtl;
        double Angle;

        public string AreaText
        {
            get
            {
                string s_area = "";

                pPos __txt = cReadData.AreaList.FirstOrDefault(__p => __p.Inside(Points));

                if (__txt != null)
                {
                    s_area = __txt.Content.filter("+").First();
                    DimObj.ValueListOverride = __txt.Content;
                }
                
                if(s_area.et_())
                    s_area = (Points.Area() / 1000000).roundNumber(0.1).ToString();

                return s_area;
            }
        }
                
        bool _isRegionDetailVisible(string mtl)
        {
            return !mtl.ct_("F") && !mtl.ct_("MT_") && !mtl.ct_("ZERO");
        }

        public RegionInfor(cReadData css, string _py_filename) : base(css, _py_filename)
        {
            DimObj.category = "i9-dimension";
            Angle = 0;
        }

        public double CircleSize
        {
            get
            {
                double circle_size = cReadData.SuperKeyValue("r").ToNumber();
                if (circle_size == 0) circle_size = 2.8;
                return circle_size * cReadData._dpsc;
            }
        }

        public string DjangoObject
        {
            get
            {
                string st_points = "{" + String.Format("'name':'{0}','name_pos':'{1},{2}','area':'{3}','note':'{4}',\n\t\t'pts':[",
                    Title.Content, pX(Title - Basepoint), pY(Title - Basepoint), AreaText, Mtl);

                foreach (pPos _p in Points)
                    st_points += "(" + pX(_p - Basepoint) + "," + pY(_p - Basepoint) + "),";

                st_points += "]}";

                return st_points;
            }
        }

        public void DrawHtml()
        {
            drawHtmlDimension();

            if (_isRegionDetailVisible(Mtl))
            {
                AppendCircleHtml("i8-caption-txt", Title, CircleSize);
                AppendTextHtml("i8-caption-txt", Mtl, Title + new pPos(0, 1.25));
                AppendTextHtml("i9-area-txt", AreaText, null, "id ='" + Mtl + "'", Angle);
            }
        }

        void drawHtmlDimension()
        {
            double _max_lengthest = 0;

            for (int i = 0; i < Points.Length; i++)
            {
                pPos p0 = Points.ElementAt(i);
                pPos p1 = Points.ElementAt(i == Points.Length - 1 ? 0 : i + 1);
                double m = p0.DistanceToPoint(p1);

                if (m > _max_lengthest)
                {
                    _max_lengthest = m;
                    Angle = p0.X > p1.X ? _rounAngle((p0 - p1).Angle()) : _rounAngle((p1 - p0).Angle());
                }

                if (_isRegionDetailVisible(Mtl))
                    AppendDimension(p0, p1);
            }

            if (Mtl.ct_("WALK") && Points.Count() > 3 * cReadData.__sc)
            {
                pPos p0 = Points[0];
                pPos p1 = Points[1];
                //pPos p2 = Points[2];
                pPos p3 = Points[3];

                if (p0.DistanceToPoint(p1) > p0.DistanceToPoint(p3))
                    AppendDimension(p0, p1);
                else
                    AppendDimension(p0, p3);
            }
        }
    }

}
