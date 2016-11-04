using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    // Used codes from
    // https://social.msdn.microsoft.com/Forums/en-US/bfc75b57-df16-48c6-92af-ea0a34f540ae/how-to-get-the-handle-of-a-window-that-i-click?forum=csharplanguage


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(int xPoint, int yPoint);

        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(POINT Point);

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
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);


        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        private const int BM_CLICK = 0x00F5;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        public enum WMessages : int
        {
            WM_LBUTTONDOWN = 0x201,
            WM_LBUTTONUP = 0x202,
            WM_LBUTTONDBLCLK = 0x203,
            WM_RBUTTONDOWN = 0x204,
            WM_RBUTTONUP = 0x205,
            WM_RBUTTONDBLCLK = 0x206
        }

        private const int WH_MOUSE_LL = 14;

        private int MAKELPARAM(int p, int p_2)
        {
            return ((p_2 << 16) | (p & 0xFFFF));
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static LowLevelMouseProc _proc = HookCallback;
        static IntPtr hHook = IntPtr.Zero;
        public static IntPtr currentlySelectedWindow = IntPtr.Zero;
        private static IntPtr _hookID = IntPtr.Zero;
        Thread cursorThread;
        bool threadShouldEnd = false;

        public List<Command> commands = new List<Command>();

        public MainWindow()
        {
            InitializeComponent();

            commands.Add(new Command(CommandType.Wait, "Command 1", "f", "a", "c"));
            commands.Add(new Command(CommandType.Click, "Command 2", "a", "fds", "das", "fds"));

            commandDataGrid.DataContext = commands;

            cursorThread = new Thread(new ThreadStart(LabelUpdate));
            cursorThread.Start();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DoMouseClickAtWindow(currentlySelectedWindow, int.Parse(xCoordinateBox.Text), int.Parse(yCoordinateBox.Text));
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

        public void DoMouseClickAtWindow(IntPtr window, int x, int y)
        {
            IntPtr myHandle = new WindowInteropHelper(this).Handle;

            SetForegroundWindow(window);

            SendMessage(window, (int)WMessages.WM_LBUTTONDOWN, 0, MAKELPARAM(x, y));
            SendMessage(window, (int)WMessages.WM_LBUTTONUP, 0, MAKELPARAM(x, y));
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

        private void LabelUpdate()
        {
            while (!threadShouldEnd)
            {
                //Logic
                Point p = GetMousePosition();
                Color c = GetPixelColor((int)p.X, (int)p.Y);

                IntPtr handle = WindowFromPoint((int)p.X, (int)p.Y);
                Rect windowRect = new Rect();
                GetWindowRect(currentlySelectedWindow, ref windowRect);

                //Update UI
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    absoluteLabel.Content = p.X + ", " + p.Y + "      color: " + c.R + " " + c.G + " " + c.B;

                    windowCoordinateLabel.Content = "Window handle is " + currentlySelectedWindow + " X: " + windowRect.Left + "  Y: " + windowRect.Top;

                    Color windowColor = GetPixelColorFromWindow(currentlySelectedWindow, int.Parse(xCoordinateBox.Text), int.Parse(yCoordinateBox.Text));
                    relativeLabel.Content = xCoordinateBox.Text + ", " + yCoordinateBox.Text + "      color: " + windowColor.R + " " + windowColor.G + " " + windowColor.B;

                    Color windowColor2 = GetPixelColorFromWindow(IntPtr.Zero, int.Parse(xCoordinateBox.Text) + windowRect.Left, int.Parse(yCoordinateBox.Text) + windowRect.Top);
                    referenceLabel.Content = xCoordinateBox.Text + ", " + yCoordinateBox.Text + "      color: " + windowColor2.R + " " + windowColor2.G + " " + windowColor2.B;
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

        static public Color GetPixelColorFromWindow(IntPtr window, int x, int y)
        {
            IntPtr hdc = GetDC(window);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(window, hdc);
            Color color = Color.FromRgb(
                (byte)(pixel & 0x000000FF),
                (byte)((pixel & 0x0000FF00) >> 8),
                (byte)((pixel & 0x00FF0000) >> 16));
            return color;
        }

        private void FindWindowButton_Click(object sender, RoutedEventArgs e)
        {
            if (IntPtr.Zero == hHook)
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    hHook = SetWindowsHookEx(WH_MOUSE_LL, _proc,
                        GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && WMessages.WM_LBUTTONDOWN == (WMessages)wParam)
            {
                //  The application runs to here when you click on the window whose handle you  want to get                
                POINT cusorPoint;
                bool ret = GetCursorPos(out cusorPoint);
                // cusorPoint contains your cusor’s position when you click on the window


                // Then use cusorPoint to get the handle of the window you clicked

                IntPtr winHandle = WindowFromPoint(cusorPoint);

                // winHandle is the Hanle you need


                // Now you have get the handle, do what you want here
                // …………………………………………………. 

                Console.WriteLine("Setting window to " + winHandle);
                currentlySelectedWindow = winHandle;

                // Because the hook may occupy much memory, so remember to uninstall the hook after
                // you finish your work, and that is what the following code does.
                UnhookWindowsHookEx(hHook);
                hHook = IntPtr.Zero;


                // Here I do not use the GetActiveWindow(). Let's call the window you clicked "DesWindow" and explain my reason.
                // I think the hook intercepts the mouse click message before the mouse click message delivered to the DesWindow's 
                // message queue. The application came to this function before the DesWindow became the active window, so the handle 
                // abtained from calling GetActiveWindow() here is not the DesWindow's handle, I did some tests, and What I got is always 
                // the Form's handle, but not the DesWindow's handle. You can do some test too.

                //IntPtr handle = GetActiveWindow();


            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void textBoxValue_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !TextBoxTextAllowed(e.Text);
        }

        private void textBoxValue_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string Text1 = (string)e.DataObject.GetData(typeof(string));
                if (!TextBoxTextAllowed(Text1))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private bool TextBoxTextAllowed(string Text2)
        {
            return Array.TrueForAll<char>(Text2.ToCharArray(),
                delegate (char c) { return char.IsDigit(c) || char.IsControl(c); });
        }
    }
}
