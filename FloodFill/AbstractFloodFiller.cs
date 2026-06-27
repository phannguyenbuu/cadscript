using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Drawing;
using System.Diagnostics;

namespace AcadScript
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public delegate void UpdateScreenDelegate(ref int x, ref int y);

    /// <summary>
    /// The base class that the flood fill algorithms inherit from. Implements the
    /// basic flood filler functionality that is the same across all algorithms.
    /// </summary>
    public abstract class AbstractFloodFiller
    {
        public int tole = 5;
        public List<Point> results;
        protected EditableBitmap bitmap;
        protected byte[] tolerance = new byte[] { 25, 25, 25 };
        protected Color fillColor = Color.Magenta;
        protected bool fillDiagonally = false;
        protected bool slow = false;

        //cached bitmap properties
        protected int bitmapWidth = 0;
        protected int bitmapHeight = 0;
        protected int bitmapStride = 0;
        protected int bitmapPixelFormatSize = 0;
        protected byte[] bitmapBits = null;

        //internal int timeBenchmark = 0;
        internal Stopwatch watch = new Stopwatch();
        internal UpdateScreenDelegate UpdateScreen;

        //internal, initialized per fill
        //protected BitArray pixelsChecked;
        protected bool[] pixelsChecked;
        protected byte[] byteFillColor;
        protected byte[] startColor;
        //protected int stride;

        public AbstractFloodFiller()
        {

        }

        List<Point> borders;

        bool _exist(int x, int y, int axis)
        {
            return (axis == 0 && borders.Any(p => p.X == x && p.Y == y)) 
                || (axis == 1 && borders.Any(p => p.Y == x && p.X == y));
        }

        void AddToBorder(int x, int y, int axis)
        {
            if(!_exist(x,y,axis))
                borders.Add(axis == 0 ? new Point(x, y) : new Point(y, x));
        }

        public void AddResult(int x, int y)
        {
            if (!results.Any(p => p.X == x && p.Y == y))
                results.Add(new Point(x, y));
        }

        List<Point[]> SortBorder()
        {
            List<Point[]> resss = new List<Point[]>();
            List<int> res = new List<int> { 0 };
            List<int> his = new List<int> ();
            
            while (true)
            {
                int i = res.Last();
                his.Add(i);

                List<int> indexes = new List<int>();

                for (int j = 1; j < borders.Count; j++)
                    if (!res.Contains(j))
                        indexes.Add(j);

                if (indexes.Count == 0)
                    break;

                indexes = indexes.Where(j => !his.Contains(j)).ToList();

                if (indexes.Count == 0) break;

                List<int> indx2 = indexes.Where(j => Math.Abs(borders[j].Y - borders[i].Y) < tole
                    && Math.Abs(borders[j].X - borders[i].X) < tole).ToList();

                if (indx2.Count == 0) indx2 = indexes.ToList();
                    
                indx2 = indx2.OrderBy(j => (borders[j].Y - borders[i].Y) * (borders[j].Y - borders[i].Y)
                    + (borders[j].X - borders[i].X) * (borders[j].X - borders[i].X)).ToList();

                int k = indx2.First();

                if (Math.Abs(borders[k].Y - borders[i].Y) < tole
                    && Math.Abs(borders[k].X - borders[i].X) < tole)
                    res.Add(k);
                else
                {
                    if(res.Count > 1)
                        resss.Add(res.Select(n => borders[n]).ToArray());
                    res = new List<int> { k };
                }
            }

            if (res.Count > 1)
                resss.Add(res.Select(n => borders[n]).ToArray());
            //borders = res.Select(n => borders[n]).ToList();
            return resss;
        }

        int np(Point p, int axis)
        {
            return axis == 0 ? p.X : p.Y;
        }

        void _addPoint(int axis)
        {
            int nex = (axis + 1) % 2;
                        
            Point[] res = results.OrderBy(p => np(p, nex)).ThenBy(p => np(p, axis)).ToArray();
            List<int> ls = new List<int> { np(res.First(), axis) };
            int cur = np(res.First(), nex);

            foreach (Point p in res)
                if (np(p, nex) != cur)
                {
                    for (int i = 0; i < ls.Count; i++)
                        if(!_exist(ls[i], cur, axis))
                        {
                            if (i == 0 || !ls.Contains(ls[i] - 1))
                                AddToBorder(ls[i], cur, axis);

                            if (i == ls.Count - 1 || !ls.Contains(ls[i] + 1))
                                AddToBorder(ls[i], cur, axis);
                        }

                    ls = new List<int> { np(p, axis) };
                    cur = np(p,nex);
                }
                else
                    ls.Add(np(p, axis));

            for (int i = 0; i < ls.Count; i++)
                if (!_exist(ls[i], cur, axis))
                {
                    if (i == 0 || !ls.Contains(ls[i] - 1))
                        AddToBorder(ls[i], cur, axis);

                    if (i == ls.Count - 1 || !ls.Contains(ls[i] + 1))
                        AddToBorder(ls[i], cur, axis);
                }
        }

        public List<Point[]> Borders
        {
            get
            {
                borders = new List<Point>();
                _addPoint(0);
                _addPoint(1);

                return SortBorder();
            }
        }

        public AbstractFloodFiller(AbstractFloodFiller configSource)
        {
            if (configSource != null)
            {
                this.Bitmap = configSource.Bitmap;
                this.FillColor = configSource.FillColor;
                this.FillDiagonally = configSource.FillDiagonally;
                this.Slow = configSource.Slow;
                this.Tolerance = configSource.Tolerance;
            }
        }

        public bool Slow
        {
            get { return slow; }
            set { slow = value; }
        }

        public Color FillColor
        {
            get { return fillColor; }
            set { fillColor = value; }
        }

        public bool FillDiagonally
        {
            get { return fillDiagonally; }
            set { fillDiagonally = value; }
        }

        public byte[] Tolerance
        {
            get { return tolerance; }
            set { tolerance = value; }
        }

        public EditableBitmap Bitmap
        {
            get { return bitmap; }
            set 
            { 
                bitmap = value;
            }
        }

        public abstract void FloodFill(Point pt);

        protected void PrepareForFloodFill(Point pt)
        {   
            //cache data in member variables to decrease overhead of property calls
            //this is especially important with Width and Height, as they call
            //GdipGetImageWidth() and GdipGetImageHeight() respectively in gdiplus.dll - 
            //which means major overhead.
            byteFillColor = new byte[] { fillColor.B, fillColor.G, fillColor.R };
            bitmapStride=bitmap.Stride;
            bitmapPixelFormatSize=bitmap.PixelFormatSize;
            bitmapBits = bitmap.Bits;
            bitmapWidth = bitmap.Bitmap.Width;
            bitmapHeight = bitmap.Bitmap.Height;

            pixelsChecked = new bool[bitmapBits.Length / bitmapPixelFormatSize];
        }
    }
}
