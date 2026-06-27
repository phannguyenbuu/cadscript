using System;
using System.Collections;
using System.Collections.Generic;

public class LiangBarskyAlgorithm
{
    public static void ClipLine(int xMin, int yMin, int xMax,
        int yMax, ref int x1, ref int y1, ref int x2, ref int y2)
    {
        int dx = x2 - x1;
        int dy = y2 - y1;
        float p1 = -dx;
        float p2 = dx;
        float p3 = -dy;
        float p4 = dy;
        float q1 = x1 - xMin;
        float q2 = xMax - x1;
        float q3 = y1 - yMin;
        float q4 = yMax - y1;
        float u1 = 0;
        float u2 = 1;

        if (ClipTest(p1, q1, ref u1, ref u2) && ClipTest(p2, q2, ref u1, ref u2) &&
            ClipTest(p3, q3, ref u1, ref u2) && ClipTest(p4, q4, ref u1, ref u2))
        { 
            x1 = (int)(x1 + u1 * dx);
            y1 = (int)(y1 + u1*dy);
            x2 = (int)(x1 + u2 *dx);
            y2 = (int)(y1 + u2 *dy);
            Console.WriteLine("Line clipped successfully!");
            Console.WriteLine("Clipped Line Coordinates: ({0}, {1}) - ({2}, {3})", x1, y1, x2, y2);
        } else
        {
            Console.WriteLine("Line cannot be clipped!");
        }
    }

    private static bool ClipTest(float p, float q, ref float u1, ref float u2)
    {
        float r = q / p;

        if (p < 0)
        {
            if (r > u2)
            {
                return false;
            }
            else if (r > u1)
            {
                u1 = r;
            }
        }
        else if (p > 0)
        {
            if (r < u1)
            {
                return false;
            }
            else if (r < u2)
            {
                u2 = r;
            }
        }
        else if (q < 0)
        {
            return false;
        }
        return true;
    }

    public static void Main2(string[] args)
    {
        int x1 = 10;
        int y1 = 20; int x2 = 100; int y2 = 200;
        int xMin = 50;
        int yMin = 50; int xMax = 150; int yMax = 150;
        Console.WriteLine("Original Line Coordinates: ({0}, {1}) - ({2}, {3})", x1, y1, x2, y2);
        ClipLine(xMin, yMin, xMax, yMax, ref x1, ref y1, ref x2, ref y2);

        Console.ReadLine();
    }
}

public class Point
{
    public double X
    {
        get; set;
    }
    public double Y
    {
        get; set;
    }
    public Point(double x, double y)
    {
        X = X;
        Y = y;
    }
}
public class SutherlandHodgman
{
    public static List<Point> Clip(List<Point> polygon, double xmin, double xmax, double ymin, double ymax)
    {
        List<Point> outputList = polygon;
        // Left edge
        outputList = ClipEdge(outputList, new Point(xmin, ymin), new Point(xmin, ymax));
        // Top edge
        outputList = ClipEdge(outputList, new Point(xmin, ymax), new Point(xmax, ymax));
        // Right edge
        outputList = ClipEdge(outputList, new Point(xmax, ymax), new Point(xmax, ymin));
        // Bottom edge
        outputList = ClipEdge(outputList, new Point(xmax, ymin), new Point(xmin, ymin));
        return outputList;
    }

    private static List<Point> ClipEdge(List<Point> polygon, Point p1, Point p2)
    {
        List<Point> outputList = new List<Point>();

        if (polygon.Count == 0) return outputList;

        Point S = polygon[polygon.Count - 1];

        foreach (Point E in polygon)
        {
            if (!IsInside(S, p1, p2))
            {
                Point point = GetIntersection(S, E, p1, p2);
                if (point != null)
                    outputList.Add(point);

                outputList.Add(E);
            } else if (IsInside(S, p1, p2))
            {
                Point point = GetIntersection(S, E, p1, p2);
                if (point != null)
                    outputList.Add(point);
            }
            S = E;
        }

        return outputList;
    }

    private static bool IsInside(Point point, Point p1, Point p2)
    {
        return (p2.X - p1.X) * (point.Y - p1.Y) > (p2.Y - p1.Y) * (point.X - p1.X);
    }
    private static Point GetIntersection(Point A, Point B, Point C, Point D)
    {
        double denominator = (B.X - A.X) * (D.Y - C.Y) - (B.Y - A.Y) * (D.X - C.X);
        if (denominator == 0)
            return null;

        double numerator1 = (A.Y - C.Y) * (D.X - C.X) - (A.X - C.X) * (D.Y - C.Y);
        double numerator2 = (A.Y - C.Y) * (B.X - A.X) - (A.X - C.X) * (B.Y - A.Y);
        double r = numerator1 / denominator; double s = numerator2 / denominator;

        if (r >= 0 && r <= 1 && s >= 0 && s <= 1)
        {
            double x = A.X + (r * (B.X - A.X));
            double y = A.Y + (r * (B.Y - A.Y));
            return new Point(x, y);
        }

        return null;
    }

    public class Program
    {
        public static void Main2(string[] args)
        {
            List<Point> polygon = new List<Point>
            {new Point(50, 150), new Point(200, 50), new Point(350, 150),
            new Point(350, 300), new Point(250, 300), new Point(200, 250),
            new Point(150, 350), new Point(100, 250), new Point(100, 200) };

            List<Point> clippedPolygon = SutherlandHodgman.Clip(polygon, 100, 300, 100, 300);
            foreach (Point point in clippedPolygon)
            {
                Console.WriteLine("({point.X}, {point. Y})");
            }
        }
    }
}