using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcadScript
{
    public static class GB
    {
        public static double PLOT_PAGE_W = 841;
        public static double PLOT_PAGE_H = 594;
        public static string[] TITLE_BLOCKS = new string[] { "Frame Logos A2" };
        public static string HIDDEN_LAYER = "LAYER=1_Gridline_QHP";

        public static pPos txt_page_no = new pPos(37950, 1270);
        public static pPos title_block_mv = new pPos(-3239.302, -4084.664);
        //X8-Y1, X9 - Y1
    }
}
