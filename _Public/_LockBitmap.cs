using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SyncObject;

namespace AcadScript
{

    public static class BitmapExtension
    {
        public static PointF[] FloodFillBorders;

        public static PointF[] FloodFill(this Bitmap bmp, Point pt, int tole = 10)
        {
            Bitmap b = new Bitmap(bmp.Width, bmp.Height);

            using (Graphics g = Graphics.FromImage(b))
            {
                g.DrawImage(bmp, 0, 0);
            }

            List<PointF> res = new List<PointF>();
            Stack<Point> pixels = new Stack<Point>();

            pixels.Push(pt);

            LockBitmap lBmp = new LockBitmap(b);
            lBmp.LockBits();
            var targetColor = lBmp.GetPixel(pt.X, pt.Y);

            Stack<PointF> borders = new Stack<PointF>();

            while (pixels.Count > 0)
            {
                Point a = pixels.Pop();

                if (a.X < lBmp.Width && a.X > -1 && a.Y < lBmp.Height && a.Y > -1)
                {
                    System.Drawing.Color clr = lBmp.GetPixel(a.X, a.Y);

                    if (Math.Abs(clr.R - targetColor.R) < tole
                        && Math.Abs(clr.B - targetColor.B) < tole
                        && Math.Abs(clr.G - targetColor.G) < tole)
                    {
                        res.Add(new PointF(a.X, a.Y));
                        lBmp.SetPixel(a.X, a.Y, System.Drawing.Color.Red);

                        Point[] nearest = new Point[] {new Point(a.X - 1, a.Y), new Point(a.X + 1, a.Y),
                            new Point(a.X, a.Y - 1), new Point(a.X, a.Y + 1)};

                        foreach (Point p in nearest)
                            pixels.Push(p);

                        if (nearest.Any(p => {
                            System.Drawing.Color c = lBmp.GetPixel(p.X, p.Y);
                            return Math.Abs(c.R - targetColor.R) >= tole
                                || Math.Abs(c.B - targetColor.B) >= tole
                                || Math.Abs(c.G - targetColor.G) >= tole;
                        }))
                            borders.Push(a);
                    }
                }
            }

            FloodFillBorders = borders.ToArray();
            lBmp.UnlockBits();
            b.Dispose();

            return res.ToArray();
        }
        
        public static Bitmap OverlayBitmap(this Bitmap baseImage, string overlay_file)
        {
            Bitmap overlayImage = (Bitmap)Bitmap.FromFile(overlay_file);

            //baseImage = (Bitmap)Image.FromFile(@"C:\temp\base.jpg");

            //overlayImage = (Bitmap)Image.FromFile(@"C:\temp\overlay.png");

            var finalImage = new Bitmap(overlayImage.Width, overlayImage.Height, PixelFormat.Format32bppArgb);
            var graphics = Graphics.FromImage(finalImage);
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

            graphics.DrawImage(baseImage, 0, 0);
            graphics.DrawImage(overlayImage, 0, 0);

            overlayImage.Dispose();

            //show in a winform picturebox
            return finalImage;

            //save the final composite image to disk
            //finalImage.Save(@"C:\temp\final.jpg", ImageFormat.Jpeg);
        }

        public static Bitmap ScaleBitmap(this Bitmap image, double scale)
        {
            if (scale == 0 || scale == 1)
                return image;
            else
            {
                int scaleWidth = (int)(image.Width * scale);
                int scaleHeight = (int)(image.Height * scale);

                var bmp = new Bitmap(scaleWidth, scaleHeight);

                using (var graph = Graphics.FromImage(bmp))
                {
                    graph.DrawImage(image, 0, 0, scaleWidth, scaleHeight);
                }

                return bmp;
            }
        }
        
        public static void RotateBitmap(this Bitmap b, double angle)
        {
            if (Math.Abs(90 - angle) < 1)
                b.RotateFlip(RotateFlipType.Rotate90FlipNone);
            else if (Math.Abs(180 - angle) < 1)
                b.RotateFlip(RotateFlipType.Rotate180FlipNone);
            else if (Math.Abs(270 - angle) < 1)
                b.RotateFlip(RotateFlipType.Rotate270FlipNone);
        }

        public static void FillTexturePointArray(this Bitmap bmp, 
            PointF[] pfs, string texture_file,
            double texture_scale=1, double texture_angle = 0)
        {
            //label1.Text = "OK1";
            Bitmap texture = (Bitmap)Bitmap.FromFile(texture_file);

            if (texture_scale != 1)
                texture = texture.ScaleBitmap(texture_scale);

            if (texture_angle != 0)
                texture.RotateBitmap(texture_angle);

            pPos[] pts = pfs.Select(p => new pPos(p.X, p.Y)).ToArray();
            pPos[] bb = pts.Boundary();

            Bitmap dest_texture = new Bitmap((int)bb.Size().X + 1, (int)bb.Size().Y + 1);
            int bx = (int)bb[0].X, by = (int)bb[0].Y;
            //label1.Text = "OK2";

            using (Graphics g = Graphics.FromImage(dest_texture))
            {
                TextureBrush tb = new TextureBrush(texture);

                g.FillRectangle(new TextureBrush(texture), 0, 0, dest_texture.Width, dest_texture.Height);
                //g.FillRectangle(tb, 45, 45, 70, 150);
                //bmp.Dispose();
                //tb.Dispose();)
            }
            //label1.Text = "OK3";
            texture.Dispose();

            LockBitmap lTxt = new LockBitmap(dest_texture);
            lTxt.LockBits();

            LockBitmap lBmp = new LockBitmap(bmp);
            lBmp.LockBits();
            //label1.Text = "OK4";

            foreach (PointF p in pfs)
            {
                //lblInfo.Text = String.Format("\n{0},{1},{2},{3}({4},{5})", 
                //    (int)p.X, (int)p.Y, (int)p.X - bx + 1, 
                //    (int)p.Y - by + 1, (int)bb.Size().X, (int)bb.Size().Y);
                lBmp.SetPixel((int)p.X, (int)p.Y, lTxt.GetPixel((int)p.X - bx, (int)p.Y - by));
            }

            //label1.Text = "OK5";
            lBmp.UnlockBits();
            lTxt.UnlockBits();

            dest_texture.Dispose();
        }

        public static void FillPointArray(this Bitmap bmp, PointF[] pfs, System.Drawing.Color clr)
        {
            LockBitmap lBmp = new LockBitmap(bmp);
            lBmp.LockBits();

            foreach (PointF p in pfs)
                lBmp.SetPixel((int)p.X, (int)p.Y, clr);

            lBmp.UnlockBits();
        }

    }

    public class LockBitmap
    {
        public Bitmap source = null;
        IntPtr Iptr = IntPtr.Zero;
        BitmapData bitmapData = null;

        public byte[] Pixels { get; set; }
        public int Depth { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        /*public static bool IsColor(System.Drawing.Color c1, System.Drawing.Color c2, int tolerance)
        {
            return Math.Abs(c1.R - c2.R) < tolerance &&
                   Math.Abs(c1.G - c2.G) < tolerance &&
                   Math.Abs(c1.B - c2.B) < tolerance;
        }*/

        public bool IsColor(int x, int y, System.Drawing.Color checkColor, int tolerance = 30)
        {
            System.Drawing.Color c1 = GetPixel(x, y);
            return Math.Abs(c1.R - checkColor.R) < tolerance &&
                   Math.Abs(c1.G - checkColor.G) < tolerance &&
                   Math.Abs(c1.B - checkColor.B) < tolerance;
        }

        public LockBitmap(Bitmap source)
        {
            this.source = source;
        }

        /// <summary>
        /// Lock bitmap data
        /// </summary>
        public void LockBits()
        {
            try
            {
                // Get width and height of bitmap
                Width = source.Width;
                Height = source.Height;

                // get total locked pixels count
                int PixelCount = Width * Height;

                // Create rectangle to lock
                Rectangle rect = new Rectangle(0, 0, Width, Height);

                // get source bitmap pixel format size
                Depth = System.Drawing.Bitmap.GetPixelFormatSize(source.PixelFormat);

                // Check if bpp (Bits Per Pixel) is 8, 24, or 32
                if (Depth != 8 && Depth != 24 && Depth != 32)
                {
                    throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
                }

                // Lock bitmap and return bitmap data
                bitmapData = source.LockBits(rect, ImageLockMode.ReadWrite, source.PixelFormat);

                // create byte array to copy pixel values
                int step = Depth / 8;
                Pixels = new byte[PixelCount * step];
                Iptr = bitmapData.Scan0;

                // Copy data from pointer to array
                Marshal.Copy(Iptr, Pixels, 0, Pixels.Length);
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Unlock bitmap data
        /// </summary>
        public void UnlockBits()
        {
            try
            {
                // Copy data from byte array to pointer
                Marshal.Copy(Pixels, 0, Iptr, Pixels.Length);

                // Unlock bitmap data
                source.UnlockBits(bitmapData);
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Get the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public System.Drawing.Color GetPixel(int x, int y)
        {
            System.Drawing.Color clr = System.Drawing.Color.Empty;

            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = ((y * Width) + x) * cCount;

            if (i > Pixels.Length - cCount)
                throw new IndexOutOfRangeException();

            if (Depth == 32) // For 32 bpp get Red, Green, Blue and Alpha
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                byte a = Pixels[i + 3]; // a
                clr = System.Drawing.Color.FromArgb(a, r, g, b);
            }
            if (Depth == 24) // For 24 bpp get Red, Green and Blue
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                clr = System.Drawing.Color.FromArgb(r, g, b);
            }
            if (Depth == 8)
            // For 8 bpp get color value (Red, Green and Blue values are the same)
            {
                byte c = Pixels[i];
                clr = System.Drawing.Color.FromArgb(c, c, c);
            }
            return clr;
        }

        /// <summary>
        /// Set the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void SetPixel(int x, int y, System.Drawing.Color color)
        {
            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = ((y * Width) + x) * cCount;

            if (Depth == 32) // For 32 bpp set Red, Green, Blue and Alpha
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
                Pixels[i + 3] = color.A;
            }
            if (Depth == 24) // For 24 bpp set Red, Green and Blue
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
            }
            if (Depth == 8)
            // For 8 bpp set color value (Red, Green and Blue values are the same)
            {
                Pixels[i] = color.B;
            }
        }
    }
}