using System.Collections.Generic;

namespace AcadScript
{
    public static class SegIntersect
    {
        public static pPos _intersection;
        public static bool Paralell;
        public static bool HalfIntersect;
        public static bool SegmentIntersect;
        public static bool LineIntersect;
        public static double t1, t2;
        public static List<int> duplicateIndexes;
        public static bool[] can_extents;
        
        public static pPos Intersection
        {
            get
            {
                return _intersection != null ? new pPos(_intersection.X, _intersection.Y) : null;
            }
        }
                
        public static void CalcIntersection(double Tole = 0, params pPos[] P)
        {
            if(can_extents == null)
            {
                can_extents = new bool[4];
                for(int i = 0; i < 4; i ++) can_extents[i] = false;
            }
            Paralell = LineIntersect = false;
            _intersection = null;
            duplicateIndexes = new List<int>();

            double dx12 = P[1].X - P[0].X;
            double dy12 = P[1].Y - P[0].Y;
            double dx34 = P[3].X - P[2].X;
            double dy34 = P[3].Y - P[2].Y;

            double delta = (dy12 * dx34 - dx12 * dy34);

            if (delta == 0)
            {
                Paralell = true;
                SegmentIntersect = false;
                return;
            }
            t1 = ((P[0].X - P[2].X) * dy34 + (P[2].Y - P[0].Y) * dx34) / delta;

            LineIntersect = true;

            t2 = ((P[2].X - P[0].X) * dy12 + (P[0].Y - P[2].Y) * dx12) / -delta;

            // Find the point of intersection.
            _intersection = new pPos(P[0].X + dx12 * t1, P[0].Y + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            
            double tole_c1 = 0, tole_c2 = 0;
            if(Tole != 0)
            {
				tole_c1 = Tole / (P[0].DistanceTo(P[1]));
                tole_c2 = Tole / (P[2].DistanceTo(P[3]));
            }
            
            bool b1 = t1 >= 0 && t1 <= 1;
            bool b2 = t2 >= 0 && t2 <= 1;

            bool[] ar = new bool[]{t1 < 0 && can_extents[0] && P[0].DistanceTo(_intersection) < Tole,
                                        t1 > 1 && can_extents[1] && P[1].DistanceTo(_intersection) < Tole,
                                        t2 < 0 && can_extents[2] && P[2].DistanceTo(_intersection) < Tole,
                                        t2 > 1 && can_extents[3] && P[3].DistanceTo(_intersection) < Tole};

            SegmentIntersect = b1  && b2 ;

            for (int i = 0; i < 4; i++)
                if (ar[i]) duplicateIndexes.Add(i);

            if(!SegmentIntersect)
            {
                HalfIntersect = (ar[0] && ar[2]) || (ar[0] && ar[3]) || (ar[1] && ar[2]) || (ar[1] && ar[3]);
                if (HalfIntersect) 
                    SegmentIntersect = false;
                else
                    SegmentIntersect = (b1 && ar[2]) || (b1 && ar[3]) || (b2 && ar[0]) || (b2 && ar[1]);
            }
        }
    }
}
