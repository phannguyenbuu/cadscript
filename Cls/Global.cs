using Autodesk.AutoCAD.DatabaseServices;

namespace AcadScript
{
    public static class GP
    {
        public static string DEF_LAYER_TEXT = "A-Anno-Dims";
        //[PLOT SETTING]
        public static float PLOT_OVERLAY = 0.5f;
        public static string PLOT_DEVICE = "DWG To PDF A4 297x210.pc3";
        //PageSetup=ISO_expand_A3_(297.00_x_420.00_MM)

        public static string PAGESETUP = "A4 297x210";
        public static string PAGESCALE = "1:50";

        //PAGEWIDTH=320
        //PAGEHEIGHT=250
        public static pPos PAGESIZE = new pPos(390, 285);

        //PAGEWIDTH=780
        //PAGEHEIGHT=570

        public static int PLOTREGIONTYPE = 4;

        public static pPos PLOT_TITLE = new pPos(1,297);
        public static Extents2d PLOTWINDOW = new Extents2d( 306164.66, -20087.89, 325664.66, -7087.89 );

        public static string[] PLOTINFO = new string[]
            { "#TXT~TITLE/VERTS=100,281[TITLE];285,275[PROJECT];285,281[OWNER];285,286.5[ADDRESS];258,286.5[ID@C];195,275[CHAPTER];185,286.5[DATE];236,286.5[SCALE];",

                "#TXT~TITLE/VERTS=130,568[TITLE];315,564[PROJECT];315,570[OWNER];315,575.5[ADDRESS];266,575.5[ID@C];225,564[CHAPTER];215,575.5[DATE];241,575.5[SCALE];" };


        public static string PLOT_TITLE_FONT = "Tahoma.ttf";
        public static int PLOT_TITLE_ROTATION = 90;
        public static int PLOT_TITLE_FONT_HEIGHT = 18;

        public static string[] PLOT_TITLE_DRAWING_LIST_TITLE = new string[] {
            "DANH SÁCH BẢN VẼ", "PHẦN BẢN VẼ KIẾN TRÚC", "PHẦN BẢN VẼ KẾT CẤU",
            "PHẦN BẢN VẼ ĐIỆN - ĐIỆN NHẸ VÀ CAMERA", "PHẦN BẢN VẼ CẤP THOÁT NƯỚC", "PHẦN BẢN VẼ ĐIỀU HÒA KHÔNG KHÍ",
            "PHẦN BẢN VẼ PHÒNG CHÁY CHỮA CHÁY","PHẦN BẢN VẼ TRANG TRÍ NỘI THẤT","PHẦN BẢN VẼ VẬT DỤNG" };

        public static float PLOT_TITLE_DRAWING_LIST_TITLE_FONT_HEIGHT = 20f;
        public static string PLOT_TITLE_DRAWING_LIST_FORMAT = "NO ~100|ID ~100|TITLE ~200";

        public static float PDF_TEXT_HEIGHT = 8;
        public static string PDF_TEXT_PREFIX = "A.1";
        public static double PLOT_SCALE_MULTIPLY = 1;

        public static double PLOT_SCHEDULE_TITLE_Y_SCALE_IN_ROW = 0.5;
    }
}

