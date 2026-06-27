using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.Windows;
using System.Windows.Forms;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;
using System.Runtime.InteropServices;


//using System.Windows.Forms;
//using SyncObject;
using System.Drawing.Imaging;

namespace AcadScript
{
    /*
An Algorithm for Automatically Fitting Digitized Curves
by Philip J. Schneider
from "Graphics Gems", Academic Press, 1990
*/
    public class Utility
    {
        public static Bitmap AdjustAlpha(System.Drawing.Image image, float translucency)
        {
            // Make the ColorMatrix.
            float t = translucency;
            ColorMatrix cm = new ColorMatrix(new float[][]
                {
                    new float[] {1, 0, 0, 0, 0},
                    new float[] {0, 1, 0, 0, 0},
                    new float[] {0, 0, 1, 0, 0},
                    new float[] {0, 0, 0, t, 0},
                    new float[] {0, 0, 0, 0, 1},
                });
            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(cm);

            // Draw the image onto the new bitmap while
            // applying the new ColorMatrix.
            Point[] points = { new Point(0, 0),
                    new Point(image.Width, 0),
                    new Point(0, image.Height),
                };

            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

            // Make the result bitmap.
            Bitmap bm = new Bitmap(image.Width, image.Height);
            using (Graphics gr = Graphics.FromImage(bm))
            {
                gr.DrawImage(image, points, rect, GraphicsUnit.Pixel, attributes);
            }

            // Return the result.
            return bm;
        }

        public static Bitmap OverlayImages(System.Drawing.Image imageBackground, System.Drawing.Image imageOverlay)
        {
            //Image imageBackground = Image.FromFile("bitmap1.png");
            //Image imageOverlay = Image.FromFile("bitmap2.png");
            //ImageAttributes attributes = new ImageAttributes();
            //attributes.
            System.Drawing.Image img = new Bitmap(imageBackground.Width, imageBackground.Height);
            using (Graphics gr = Graphics.FromImage(img))
            {
                //gr.DrawImage(imageBackground, new Point(0, 0));
                //gr.DrawImage(imageOverlay, new Point(0, 0));

                gr.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

                gr.DrawImage(imageBackground, 0, 0);

                for(int i = 0; i < 10; i++)
                    gr.DrawImage(imageOverlay, 0, 0);
            }
            return (Bitmap)img;
        }

        /// <summary>
        /// Uses the Douglas Peucker algorithim to reduce the number of points.
        /// </summary>
        /// <param name="Points">The points.</param>
        /// <param name="Tolerance">The tolerance.</param>
        /// <returns></returns>
        public static pPos[] DouglasPeuckerReduction(IEnumerable<pPos> Points, Double Tolerance)
        {
            if (Points == null || Points.Count() < 3)
                return Points.ToArray();

            Int32 firstPoint = 0;
            Int32 lastPoint = Points.Count() - 1;
            List<Int32> pointIndexsToKeep = new List<Int32>();

            //Add the first and last index to the keepers
            pointIndexsToKeep.Add(firstPoint);
            pointIndexsToKeep.Add(lastPoint);

            //The first and the last point can not be the same
            while (Points.ElementAt(firstPoint).Equals(Points.ElementAt(lastPoint)))
            {
                lastPoint--;
            }

            DouglasPeuckerReduction(Points.ToList(), firstPoint, lastPoint, Tolerance, ref pointIndexsToKeep);

            //List<pPos> returnPoints = new List<pPos>();
            pointIndexsToKeep.Sort();
            
            return pointIndexsToKeep.Select(n => Points.ElementAt(n)).ToArray();
        }

        /// <summary>
        /// Douglases the peucker reduction.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="firstPoint">The first point.</param>
        /// <param name="lastPoint">The last point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="pointIndexsToKeep">The point indexs to keep.</param>
        private static void DouglasPeuckerReduction(List<pPos> points, Int32 firstPoint, Int32 lastPoint, Double tolerance, ref List<Int32> pointIndexsToKeep)
        {
            Double maxDistance = 0;
            Int32 indexFarthest = 0;

            for (Int32 index = firstPoint; index < lastPoint; index++)
            {
                Double distance = PerpendicularDistance(points[firstPoint], points[lastPoint], points[index]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    indexFarthest = index;
                }
            }

            if (maxDistance > tolerance && indexFarthest != 0)
            {
                //Add the largest point that exceeds the tolerance
                pointIndexsToKeep.Add(indexFarthest);

                DouglasPeuckerReduction(points, firstPoint, indexFarthest, tolerance, ref pointIndexsToKeep);
                DouglasPeuckerReduction(points, indexFarthest, lastPoint, tolerance, ref pointIndexsToKeep);
            }
        }

        /// <summary>
        /// The distance of a point from a line made from point1 and point2.
        /// </summary>
        /// <param name="pt1">The PT1.</param>
        /// <param name="pt2">The PT2.</param>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        public static Double PerpendicularDistance(pPos Point1, pPos Point2, pPos pPos)
        {
            //Area = |(1/2)(x1y2 + x2y3 + x3y1 - x2y1 - x3y2 - x1y3)|   *Area of triangle
            //Base = √((x1-x2)²+(x1-x2)²)                               *Base of Triangle*
            //Area = .5*Base*H                                          *Solve for height
            //Height = Area/.5/Base

            Double area = Math.Abs(.5 * (Point1.X * Point2.Y + Point2.X * pPos.Y + pPos.X * Point1.Y - Point2.X * Point1.Y - pPos.X * Point2.Y - Point1.X * pPos.Y));
            Double bottom = Math.Sqrt(Math.Pow(Point1.X - Point2.X, 2) + Math.Pow(Point1.Y - Point2.Y, 2));
            Double height = area / bottom * 2;

            return height;

            //Another option
            //Double A = pPos.X - Point1.X;
            //Double B = pPos.Y - Point1.Y;
            //Double C = Point2.X - Point1.X;
            //Double D = Point2.Y - Point1.Y;

            //Double dot = A * C + B * D;
            //Double len_sq = C * C + D * D;
            //Double param = dot / len_sq;

            //Double xx, yy;

            //if (param < 0)
            //{
            //    xx = Point1.X;
            //    yy = Point1.Y;
            //}
            //else if (param > 1)
            //{
            //    xx = Point2.X;
            //    yy = Point2.Y;
            //}
            //else
            //{
            //    xx = Point1.X + param * C;
            //    yy = Point1.Y + param * D;
            //}

            //Double d = DistanceBetweenOn2DPlane(pPos, new pPos(xx, yy));

        }
    }

    public class Vector
    {
        public double X { get; set; }
        public double Y { get; set; }
        public Vector(double x = 0, double y = 0)
        {
            X = x;
            Y = y;
        }

        public static implicit operator Vector(pPos b)
        {
            return new Vector(b.X, b.Y);
        }

        public static pPos operator *(Vector left, double right)
        {
            return new pPos(left.X * right, left.Y * right);
        }
        public static Vector operator -(Vector left, pPos right)
        {
            return new Vector(left.X - right.X, left.Y - right.Y);
        }

        internal void Negate()
        {
            X = -X;
            Y = -Y;
        }

        internal void Normalize()
        {
            double factor = 1.0 / Math.Sqrt(LengthSquared);
            X *= factor;
            Y *= factor;
        }

        public double LengthSquared { get { return X * X + Y * Y; } }
    }

    public static class FitCurves
    {
        /*  Fit the Bezier curves */

        private const int MAXPOINTS = 10000;

        public static pPos[] FitCurve(pPos[] d, double error)
        {
            Vector tHat1, tHat2;    /*  Unit tangent vectors at endpoints */

            tHat1 = ComputeLeftTangent(d, 0);
            tHat2 = ComputeRightTangent(d, d.Length - 1);
            List<pPos> result = new List<pPos>();
            FitCubic(d, 0, d.Length - 1, tHat1, tHat2, error, result);
            return result.ToArray();
        }
        
        private static void FitCubic(pPos[] d, int first, int last, Vector tHat1, Vector tHat2, double error, List<pPos> result)
        {
            pPos[] bezCurve; /*Control points of fitted Bezier curve*/
            double[] u;     /*  Parameter values for point  */
            double[] uPrime;    /*  Improved parameter values */
            double maxError;    /*  Maximum fitting error    */
            int splitPoint; /*  pPos to split point set at  */
            int nPts;       /*  Number of points in subset  */
            double iterationError; /*Error below which you try iterating  */
            int maxIterations = 4; /*  Max times to try iterating  */
            Vector tHatCenter;      /* Unit tangent vector at splitPoint */
            int i;

            iterationError = error * error;
            nPts = last - first + 1;

            /*  Use heuristic if region only has two points in it */
            if (nPts == 2)
            {
                double dist = (d[first] - d[last]).Length / 3.0;

                bezCurve = new pPos[4];
                bezCurve[0] = d[first];
                bezCurve[3] = d[last];
                bezCurve[1] = (tHat1 * dist) + bezCurve[0];
                bezCurve[2] = (tHat2 * dist) + bezCurve[3];

                result.Add(bezCurve[1]);
                result.Add(bezCurve[2]);
                result.Add(bezCurve[3]);
                return;
            }

            /*  Parameterize points, and attempt to fit curve */
            u = ChordLengthParameterize(d, first, last);
            bezCurve = GenerateBezier(d, first, last, u, tHat1, tHat2);

            /*  Find max deviation of points to fitted curve */
            maxError = ComputeMaxError(d, first, last, bezCurve, u, out splitPoint);
            if (maxError < error)
            {
                result.Add(bezCurve[1]);
                result.Add(bezCurve[2]);
                result.Add(bezCurve[3]);
                return;
            }


            /*  If error not too large, try some reparameterization  */
            /*  and iteration */
            if (maxError < iterationError)
            {
                for (i = 0; i < maxIterations; i++)
                {
                    uPrime = Reparameterize(d, first, last, u, bezCurve);
                    bezCurve = GenerateBezier(d, first, last, uPrime, tHat1, tHat2);
                    maxError = ComputeMaxError(d, first, last,
                               bezCurve, uPrime, out splitPoint);
                    if (maxError < error)
                    {
                        result.Add(bezCurve[1]);
                        result.Add(bezCurve[2]);
                        result.Add(bezCurve[3]);
                        return;
                    }
                    u = uPrime;
                }
            }

            /* Fitting failed -- split at max error point and fit recursively */
            tHatCenter = ComputeCenterTangent(d, splitPoint);
            FitCubic(d, first, splitPoint, tHat1, tHatCenter, error, result);
            tHatCenter.Negate();
            FitCubic(d, splitPoint, last, tHatCenter, tHat2, error, result);
        }

        static pPos[] GenerateBezier(pPos[] d, int first, int last, double[] uPrime, Vector tHat1, Vector tHat2)
        {
            int i;
            Vector[,] A = new Vector[MAXPOINTS, 2];/* Precomputed rhs for eqn    */

            int nPts;           /* Number of pts in sub-curve */
            double[,] C = new double[2, 2];            /* Matrix C     */
            double[] X = new double[2];          /* Matrix X         */
            double det_C0_C1,      /* Determinants of matrices */
                    det_C0_X,
                    det_X_C1;
            double alpha_l,        /* Alpha values, left and right */
                    alpha_r;
            Vector tmp;            /* Utility variable     */
            pPos[] bezCurve = new pPos[4];    /* RETURN bezier curve ctl pts  */
            nPts = last - first + 1;

            /* Compute the A's  */
            for (i = 0; i < nPts; i++)
            {
                Vector v1, v2;
                v1 = tHat1;
                v2 = tHat2;
                v1 *= B1(uPrime[i]);
                v2 *= B2(uPrime[i]);
                A[i, 0] = v1;
                A[i, 1] = v2;
            }

            /* Create the C and X matrices  */
            C[0, 0] = 0.0;
            C[0, 1] = 0.0;
            C[1, 0] = 0.0;
            C[1, 1] = 0.0;
            X[0] = 0.0;
            X[1] = 0.0;

            for (i = 0; i < nPts; i++)
            {
                C[0, 0] += V2Dot(A[i, 0], A[i, 0]);
                C[0, 1] += V2Dot(A[i, 0], A[i, 1]);
                /*                  C[1][0] += V2Dot(&A[i][0], &A[i][9]);*/
                C[1, 0] = C[0, 1];
                C[1, 1] += V2Dot(A[i, 1], A[i, 1]);

                tmp = ((Vector)d[first + i] -
                    (
                      ((Vector)d[first] * B0(uPrime[i])) +
                        (
                            ((Vector)d[first] * B1(uPrime[i])) +
                                    (
                                    ((Vector)d[last] * B2(uPrime[i])) +
                                        ((Vector)d[last] * B3(uPrime[i]))))));


                X[0] += V2Dot(A[i, 0], tmp);
                X[1] += V2Dot(A[i, 1], tmp);
            }

            /* Compute the determinants of C and X  */
            det_C0_C1 = C[0, 0] * C[1, 1] - C[1, 0] * C[0, 1];
            det_C0_X = C[0, 0] * X[1] - C[1, 0] * X[0];
            det_X_C1 = X[0] * C[1, 1] - X[1] * C[0, 1];

            /* Finally, derive alpha values */
            alpha_l = (det_C0_C1 == 0) ? 0.0 : det_X_C1 / det_C0_C1;
            alpha_r = (det_C0_C1 == 0) ? 0.0 : det_C0_X / det_C0_C1;

            /* If alpha negative, use the Wu/Barsky heuristic (see text) */
            /* (if alpha is 0, you get coincident control points that lead to
             * divide by zero in any subsequent NewtonRaphsonRootFind() call. */
            double segLength = (d[first] - d[last]).Length;
            double epsilon = 1.0e-6 * segLength;
            if (alpha_l < epsilon || alpha_r < epsilon)
            {
                /* fall back on standard (probably inaccurate) formula, and subdivide further if needed. */
                double dist = segLength / 3.0;
                bezCurve[0] = d[first];
                bezCurve[3] = d[last];
                bezCurve[1] = (tHat1 * dist) + bezCurve[0];
                bezCurve[2] = (tHat2 * dist) + bezCurve[3];
                return (bezCurve);
            }

            /*  First and last control points of the Bezier curve are */
            /*  positioned exactly at the first and last data points */
            /*  Control points 1 and 2 are positioned an alpha distance out */
            /*  on the tangent vectors, left and right, respectively */
            bezCurve[0] = d[first];
            bezCurve[3] = d[last];
            bezCurve[1] = (tHat1 * alpha_l) + bezCurve[0];
            bezCurve[2] = (tHat2 * alpha_r) + bezCurve[3];
            return (bezCurve);
        }

        /*
         *  Reparameterize:
         *  Given set of points and their parameterization, try to find
         *   a better parameterization.
         *
         */
        static double[] Reparameterize(pPos[] d, int first, int last, double[] u, pPos[] bezCurve)
        {
            int nPts = last - first + 1;
            int i;
            double[] uPrime = new double[nPts];      /*  New parameter values    */

            for (i = first; i <= last; i++)
            {
                uPrime[i - first] = NewtonRaphsonRootFind(bezCurve, d[i], u[i - first]);
            }
            return uPrime;
        }



        /*
         *  NewtonRaphsonRootFind :
         *  Use Newton-Raphson iteration to find better root.
         */
        static double NewtonRaphsonRootFind(pPos[] Q, pPos P, double u)
        {
            double numerator, denominator;
            pPos[] Q1 = new pPos[3], Q2 = new pPos[2];   /*  Q' and Q''          */
            pPos Q_u, Q1_u, Q2_u; /*u evaluated at Q, Q', & Q''  */
            double uPrime;     /*  Improved u          */
            int i;

            /* Compute Q(u) */
            Q_u = BezierII(3, Q, u);

            /* Generate control vertices for Q' */
            for (i = 0; i <= 2; i++)
            {
                Q1[i].X = (Q[i + 1].X - Q[i].X) * 3.0;
                Q1[i].Y = (Q[i + 1].Y - Q[i].Y) * 3.0;
            }

            /* Generate control vertices for Q'' */
            for (i = 0; i <= 1; i++)
            {
                Q2[i].X = (Q1[i + 1].X - Q1[i].X) * 2.0;
                Q2[i].Y = (Q1[i + 1].Y - Q1[i].Y) * 2.0;
            }

            /* Compute Q'(u) and Q''(u) */
            Q1_u = BezierII(2, Q1, u);
            Q2_u = BezierII(1, Q2, u);

            /* Compute f(u)/f'(u) */
            numerator = (Q_u.X - P.X) * (Q1_u.X) + (Q_u.Y - P.Y) * (Q1_u.Y);
            denominator = (Q1_u.X) * (Q1_u.X) + (Q1_u.Y) * (Q1_u.Y) +
                          (Q_u.X - P.X) * (Q2_u.X) + (Q_u.Y - P.Y) * (Q2_u.Y);
            if (denominator == 0.0f)
                return u;

            /* u = u - f(u)/f'(u) */
            uPrime = u - (numerator / denominator);
            return (uPrime);
        }



        /*
         *  Bezier :
         *      Evaluate a Bezier curve at a particular parameter value
         * 
         */
        static pPos BezierII(int degree, pPos[] V, double t)
        {
            int i, j;
            pPos Q;          /* pPos on curve at parameter t    */
            pPos[] Vtemp;      /* Local copy of control points     */

            /* Copy array   */
            Vtemp = new pPos[degree + 1];
            for (i = 0; i <= degree; i++)
            {
                Vtemp[i] = V[i];
            }

            /* Triangle computation */
            for (i = 1; i <= degree; i++)
            {
                for (j = 0; j <= degree - i; j++)
                {
                    Vtemp[j].X = (1.0 - t) * Vtemp[j].X + t * Vtemp[j + 1].X;
                    Vtemp[j].Y = (1.0 - t) * Vtemp[j].Y + t * Vtemp[j + 1].Y;
                }
            }

            Q = Vtemp[0];
            return Q;
        }


        /*
         *  B0, B1, B2, B3 :
         *  Bezier multipliers
         */
        static double B0(double u)
        {
            double tmp = 1.0 - u;
            return (tmp * tmp * tmp);
        }


        static double B1(double u)
        {
            double tmp = 1.0 - u;
            return (3 * u * (tmp * tmp));
        }

        static double B2(double u)
        {
            double tmp = 1.0 - u;
            return (3 * u * u * tmp);
        }

        static double B3(double u)
        {
            return (u * u * u);
        }

        /*
         * ComputeLeftTangent, ComputeRightTangent, ComputeCenterTangent :
         *Approximate unit tangents at endpoints and "center" of digitized curve
         */
        static Vector ComputeLeftTangent(pPos[] d, int end)
        {
            Vector tHat1;
            tHat1 = d[end + 1] - d[end];
            tHat1.Normalize();
            return tHat1;
        }

        static Vector ComputeRightTangent(pPos[] d, int end)
        {
            Vector tHat2;
            tHat2 = d[end - 1] - d[end];
            tHat2.Normalize();
            return tHat2;
        }

        static Vector ComputeCenterTangent(pPos[] d, int center)
        {
            Vector V1, V2, tHatCenter = new Vector();

            V1 = d[center - 1] - d[center];
            V2 = d[center] - d[center + 1];
            tHatCenter.X = (V1.X + V2.X) / 2.0;
            tHatCenter.Y = (V1.Y + V2.Y) / 2.0;
            tHatCenter.Normalize();
            return tHatCenter;
        }


        /*
         *  ChordLengthParameterize :
         *  Assign parameter values to digitized points 
         *  using relative distances between points.
         */
        static double[] ChordLengthParameterize(pPos[] d, int first, int last)
        {
            int i;
            double[] u = new double[last - first + 1];           /*  Parameterization        */

            u[0] = 0.0;
            for (i = first + 1; i <= last; i++)
            {
                u[i - first] = u[i - first - 1] + (d[i - 1] - d[i]).Length;
            }

            for (i = first + 1; i <= last; i++)
            {
                u[i - first] = u[i - first] / u[last - first];
            }

            return u;
        }




        /*
         *  ComputeMaxError :
         *  Find the maximum squared distance of digitized points
         *  to fitted curve.
        */
        static double ComputeMaxError(pPos[] d, int first, int last, pPos[] bezCurve, double[] u, out int splitPoint)
        {
            int i;
            double maxDist;        /*  Maximum error       */
            double dist;       /*  Current error       */
            pPos P;          /*  pPos on curve      */
            Vector v;          /*  Vector from point to curve  */

            splitPoint = (last - first + 1) / 2;
            maxDist = 0.0;
            for (i = first + 1; i < last; i++)
            {
                P = BezierII(3, bezCurve, u[i - first]);
                v = P - d[i];
                dist = v.LengthSquared;
                if (dist >= maxDist)
                {
                    maxDist = dist;
                    splitPoint = i;
                }
            }
            return maxDist;
        }

        private static double V2Dot(Vector a, Vector b)
        {
            return ((a.X * b.X) + (a.Y * b.Y));
        }

    }
    
    public class TracingBasepointHatchCLS
    {
        static double viewscale = 1;
        static Point viewbase;
        static pPos basepoint;
        static Point movescreen;
        static double viewheight;

        static Bitmap CaptureViewport()
        {
            //var acadapp = Autodesk.AutoCAD.ApplicationServices.Application.AcadApplication;
            //acadapp.GetType().InvokeMember("ZoomExtents", System.Reflection.BindingFlags.InvokeMethod, null, acadapp, null);

            pPos pMin = ACD.DB.Extmin.ToPos();
            pPos pMax = ACD.DB.Extmax.ToPos();

            pPos[] bb = ACD.bRect;
            pPos sz = bb.Size();
            //ObjectId lwpId = ACD.DB.DrawPolyline(pMin.Rect(pMax), true, "LAYER=0");

            Point[] r = ACD.FindViewportBounding();
            viewbase = r.First();

            basepoint = bb.First();
            viewheight = sz.Y;
            //ACD.WR("AB:{0},{1}    {2},{3}    {4},{5}", sz.X, sz.Y, (r[1].X - r[0].X), 
            //    (r[1].Y - r[0].Y), sz.X / (r[1].X - r[0].X), sz.Y / (r[1].Y - r[0].Y));
            viewscale = Math.Min(sz.X / (r[1].X - r[0].X) , sz.Y / (r[1].Y - r[0].Y));

            Bitmap img = ACD.DOC.CapturePreviewImage((uint)(r[1].X - r[0].X), (uint)(r[1].Y - r[0].Y));//.InvertingImage());
            return img;
        }

        static Bitmap CaptureMyScreen()
        {
            Bitmap captureBitmap = null;
            try
            {
                Rectangle captureRectangle = Screen.AllScreens[0].Bounds;
                captureBitmap = new Bitmap(captureRectangle.Width, captureRectangle.Height, PixelFormat.Format32bppArgb);

                using (Graphics captureGraphics = Graphics.FromImage(captureBitmap))
                {
                    captureGraphics.CopyFromScreen(captureRectangle.Left, 
                        captureRectangle.Top, 0, 0, captureRectangle.Size);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return captureBitmap;
        }

        //static void FloodFillBitmap(System.Drawing.Bitmap bmp, IEnumerable<pPos> pts)
        //{
        //    //AbstractFloodFiller floodFiller = null;
        //    QueueLinearFloodFiller floodFiller = new QueueLinearFloodFiller();

        //    floodFiller.FillColor = System.Drawing.Color.Black;
        //    floodFiller.Tolerance[0] = 0;
        //    floodFiller.Tolerance[1] = 0;
        //    floodFiller.Tolerance[2] = 0;
        //    floodFiller.Slow = false;
        //    floodFiller.tole = 5;

        //    floodFiller.Bitmap = new EditableBitmap(bmp, PixelFormat.Format32bppArgb);
        //    Bitmap res = new Bitmap(floodFiller.Bitmap.Bitmap.Width, floodFiller.Bitmap.Bitmap.Height);

        //    using (Graphics g = Graphics.FromImage(res))
        //    {
        //        g.DrawImage(bmp, new Point(0, 0));

        //        foreach (pPos pt in pts)
        //        {
        //            floodFiller.FloodFill(new System.Drawing.Point((int)pt.X, (int)pt.Y));

        //            foreach (Point[] ls in floodFiller.Borders)
        //            {
        //                pPos[] result = ls.Select(p => basepoint +
        //                    new pPos(viewscale * (p.X), viewheight - viewscale * (p.Y))).ToArray();
        //                result = Utility.DouglasPeuckerReduction(result, 2);

        //                ACD.DB.DrawPolyline(result, true);
        //                g.DrawPolygon(new Pen(System.Drawing.Color.Yellow), ls);
        //            }
        //        }
        //    }

        //    res.Save(@"D:\img" + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second + ".jpg");
        //}

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                List<pPos> pickpoints = new List<pPos>();
                ObjectIdCollection ids = new ObjectIdCollection();

                while (true)
                {
                    pPos pt = ACD.GetPoint();

                    if (pt == null)
                    {
                        break;
                    }
                    else
                    {
                        ids.AddRange(ACD.DB.DrawCircle(pt, 10));
                        System.Windows.Point p = ACD.PointToScreen(pt.X, pt.Y);
                        pickpoints.Add(new pPos(p.X,p.Y));
                        //ACD.WR("Point {0},{1}", p.X, p.Y);
                    }
                }

                ACD.DB.EraseObjects(ids);
                ACD.ED.Regen();

                Bitmap screen = CaptureViewport();

                //for(int i = 0; i < 3; i ++)
                //screen = Utility.OverlayImages(screen, screen);

                //screen.Save(@"D:\f.png", ImageFormat.Png);
               // FloodFillBitmap(screen, pickpoints);
            }

            ACD.Focus();
        }
    }
}

