using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcadScript
{
    public class gDimensionCLS
    {
        //public gDimensionCLS(cReadData css) : base(css) { }

        public string category = "dimension";
        public double x1, y1, x2, y2, offset;
        public double[] values_override = null;
        public string txt_override = null;
        public bool show_full = true;

        public List<pPos> dimension_list = new List<pPos>();
        public bool dimension_architecture_tick = false;

        public string ValueListOverride
        {
            set
            {
                List<double> dims = new List<double>();

                string[] ar = value.filter("+");
                //s_area = ar.First();

                if (ar.Length > 1)
                {
                    for (int i = 1; i < ar.Length; i++)
                        dims.Add(ar[i].ToNumber());

                    if (dims.Count < 4)
                        for (int i = dims.Count; i < 4; i++)
                            dims.Add(dims.Last());
                }

                values_override = dims.ToArray();
            }
        }
    }
}
