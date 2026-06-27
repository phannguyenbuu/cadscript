namespace AcadScript
{
    public class DrawFromParamsCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                CadLibStructCLS.DrawBeam();
                ACD.Focus();
            }
        }
    }
}

