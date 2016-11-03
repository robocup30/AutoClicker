using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoClicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(int xPoint, int yPoint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, IntPtr windowTitle);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string sClassName, string sAppName);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern int SetActiveWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        private const int BM_CLICK = 0x00F5;

        public enum WMessages : int
        {
            WM_LBUTTONDOWN = 0x201,
            WM_LBUTTONUP = 0x202,
            WM_LBUTTONDBLCLK = 0x203,
            WM_RBUTTONDOWN = 0x204,
            WM_RBUTTONUP = 0x205,
            WM_RBUTTONDBLCLK = 0x206
        }

        private int MAKELPARAM(int p, int p_2)
        {
            return ((p_2 << 16) | (p & 0xFFFF));
        }

        Thread cursorThread;
        bool threadShouldEnd = false;

        public MainWindow()
        {
            InitializeComponent();

            cursorThread = new Thread(new ThreadStart(CursorUpdate));
            cursorThread.Start();

            /*
            new Thread(() =>
            {
                while (true)
                {
                    //Logic
                    Point p = GetMousePosition();

                    //Update UI
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        mousePositionLabel.Content = p.X + ", " + p.Y;
                    }));

                    Thread.Sleep(100);
                }
            }).Start();
            */
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DoMouseClick(333, 333);
            //DoMouseClick(100, 50);
            //MessageBox.Show("Event handler was created manually.");
        }

        public void DoMouseClick(uint x, uint y)
        {
            IntPtr myHandle = new WindowInteropHelper(this).Handle;
            Point pt = new Point(x, y);
            IntPtr handle = WindowFromPoint((int)pt.X, (int)pt.Y);

            SetForegroundWindow(handle);

            SendMessage(handle, (int)WMessages.WM_LBUTTONDOWN, 0, MAKELPARAM((int)pt.X, (int)pt.Y));
            SendMessage(handle, (int)WMessages.WM_LBUTTONUP, 0, MAKELPARAM((int)pt.X, (int)pt.Y));
        }

        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            threadShouldEnd = true;
        }

        private void CursorUpdate()
        {
            while(!threadShouldEnd)
            {
                //Logic
                Point p = GetMousePosition();
                Color c = GetPixelColor((int)p.X, (int)p.Y);

                //Update UI
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    mousePositionLabel.Content = p.X + ", " + p.Y + " " + c.R + " " + c.G + " " + c.B;
                }));

                Thread.Sleep(100);
            }
        }

        static public Color GetPixelColor(int x, int y)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(IntPtr.Zero, hdc);
            Color color = Color.FromRgb(
                (byte)(pixel & 0x000000FF),
                (byte)((pixel & 0x0000FF00) >> 8),
                (byte)((pixel & 0x00FF0000) >> 16));
            return color;
        }

    }
}
