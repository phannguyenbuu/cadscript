//fileIn Cls.IReader
//fileIn Cls.Global
//fileIn Cls.MethodEntity
//fileIn Cls.IZone

//fileIn Cls.ACD
//fileIn Cls.IS
//fileIn Cls.IDraw

//fileIn Cls.IBlock
//fileIn Cls.IAnno
//fileIn Cls.IDimChain

namespace AcadScript
{
    public class Annotative100CLS
    {
        public static void Main(string[] args)
        {
            ACD.DB.ApplyAnno("1:100");
        }
    }
}

