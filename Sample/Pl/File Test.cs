using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;


using System.Windows.Forms;
//using SyncObject;
using System.Runtime.InteropServices;


namespace AcadScript
{
    public class TestCLS
    {
        //[System.Runtime.InteropServices.DllImport("user32.dll")]
        

        [DllImport("user32.dll")]
        public static extern void mouse_event(long dwFlags, long dx, long dy, long cButtons, long dwExtraInfo);
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_MOVE = 0x0001;
        //static extern bool SetCursorPos(int x, int y);

        private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);


        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        public static IntPtr[] GetAllChildHandles(IntPtr mainwindow)
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(mainwindow, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildhandlesList.Free();
            }

            return childHandles.ToArray();
        }

        static bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);

            if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
            {
                return false;
            }

            List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
            childHandles.Add(hWnd);

            return true;
        }

        //extern static private int acedPostCommand(string strExpr);
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                System.Windows.Point win = ACD.DOC.Window.DeviceIndependentLocation;
                System.Windows.Size size = ACD.DOC.Window.DeviceIndependentSize;
                
                //pPos[] r = ACD.bRect;
                //double sc = size.Width / r.Size().X;

                System.Windows.Point winpt = new System.Windows.Point(100, 200);
                
                Point3d pt = ACD.ED.PointToWorld(new System.Windows.Point(Cursor.Position.X,Cursor.Position.Y), 0);
                

                ACD.DB.DrawCircle(pt.ToPos(), 1000);
                IntPtr hWnd = Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Handle;

                IntPtr[] childs = GetAllChildHandles(hWnd);
                List<Rect> rects = new List<Rect>();

                foreach (IntPtr child in childs)
                {
                    Rect rct = new Rect();
                    GetWindowRect(child, ref rct);
                    rects.Add(rct);
                    //ACD.WR("RECT {0},{1},{2},{3}", rct.Left, rct.Top, rct.Right - rct.Left, rct.Bottom - rct.Top );
                }
                rects = rects.OrderBy(r => -r.Right + r.Left).ThenBy(r => -r.Bottom + r.Top).ToList();

                Rect rr = rects.First();
                ACD.WR("RECT {0},{1},{2},{3}", rr.Left, rr.Top, rr.Right - rr.Left, rr.Bottom - rr.Top);


                //Cursor.Position = new System.Drawing.Point((int)pt.X,(int)pt.Y);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                ACD.Focus();
            }
        }
    }
}

