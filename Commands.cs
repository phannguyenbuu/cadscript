using Autodesk.AutoCAD.Runtime;

namespace AcadScript
{
    public static class Commands
    {
        [CommandMethod("ns")]
        public static void ImportPolyline()
        {
            AcadForm ef = null;
            try
            {
                if (ef == null)
                    ef = new AcadForm();
                ef.Show();
            }
            catch (System.Exception e)
            {
                ACD.WR("Error occured " + e.Message);
            }
        }
    }
}
