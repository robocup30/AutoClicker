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

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int Width, int Height, bool Repaint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

        public const UInt32 FLASHW_ALL = 3;

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

        public struct IntPoint
        {
            public int x;
            public int y;

            public IntPoint(int v1, int v2)
            {
                this.x = v1;
                this.y = v2;
            }

            public static IntPoint LerpPoint(IntPoint p1, IntPoint p2, float amount)
            {
                int newX = (int)Lerp(p1.x, p2.x, amount);
                int newY = (int)Lerp(p1.y, p2.y, amount);
                return new IntPoint(newX, newY);
            }
        }

        public enum WMessages : int
        {
            MK_LBUTTON = 0x0001,
            WM_MOUSEMOVE = 0x200,
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

        private int MAKEWPARAM(int p, int p_2)
        {
            return ((p_2 << 16) | (p & 0xFFFF));
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static LowLevelMouseProc _proc = HookCallback;
        static IntPtr hHook = IntPtr.Zero;
        public static IntPtr currentlySelectedWindow = IntPtr.Zero;
        private static IntPtr _hookID = IntPtr.Zero;
        Thread cursorThread;
        Thread macroThread;
        bool programClosing = false;
        bool macroShouldEnd = false;

        public ObservableCollection<Command> commands = new ObservableCollection<Command>();
        public Dictionary<string, int> macroVariables = new Dictionary<string, int>();

        public CSVHandler csvHandler = new CSVHandler();

        public MainWindow()
        {
            InitializeComponent();

            commands.Add(new Command(CommandType.Wait, "1000", "f", "a", "c"));
            commands.Add(new Command(CommandType.Flash, "50, 50", "1000", "fds", "das", "fds"));
            commands.Add(new Command(CommandType.Click, "334, 223", "1000", "fds", "das", "fds"));

            commandDataGrid.DataContext = commands;

            cursorThread = new Thread(new ThreadStart(LabelUpdate));
            cursorThread.Start();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(macroThread == null || !macroThread.IsAlive)
            {
                startButton.Content = "Stop Macro";
                startFromSelectedButton.Content = "Stop Macro";
                macroThread = new Thread(() => MacroUpdate(0));
                macroThread.Start();
            }
            else
            {
                startButton.Content = "Start Macro";
                startFromSelectedButton.Content = "Start From Here";
                macroShouldEnd = true;
            }

            //DoMouseClickAtWindow(currentlySelectedWindow, 250, 450);
            //DoMouseClickAtWindow(currentlySelectedWindow, int.Parse(xCoordinateBox.Text), int.Parse(yCoordinateBox.Text));
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

        // X and Y is relative to the window
        public void DoMouseClickAtWindow(IntPtr window, int x, int y)
        {
            //IntPtr myHandle = new WindowInteropHelper(this).Handle;

            //SetForegroundWindow(window);
            SendMessage(window, (int)WMessages.WM_LBUTTONDOWN, 0, MAKELPARAM(x, y));
            Thread.Sleep(25);
            SendMessage(window, (int)WMessages.WM_LBUTTONUP, 0, MAKELPARAM(x, y));
        }

        // X and Y is relative to the window
        public void LeftMouseDownAtWindow(IntPtr window, int x, int y)
        {
            SendMessage(window, (int)WMessages.WM_LBUTTONDOWN, 0, MAKELPARAM(x, y));
        }

        // X and Y is relative to the window
        public void LeftMouseUpAtWindow(IntPtr window, int x, int y)
        {
            SendMessage(window, (int)WMessages.WM_LBUTTONUP, 0, MAKELPARAM(x, y));
        }

        // X and Y is relative to the window
        public void MoveMouseAtWindow(IntPtr window, int x, int y)
        {
            SendMessage(window, (int)WMessages.WM_MOUSEMOVE, 0, MAKELPARAM(x, y));
        }

        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            programClosing = true;
        }

        private void MacroUpdate(int startingIndex = 0)
        {
            int currentCommandIndex = startingIndex;
            while (!programClosing && !macroShouldEnd)
            {
                bool shouldMoveToNext = true;

                if(currentCommandIndex >= commands.Count)
                {
                    break;
                }

                Command currentCommand = commands[currentCommandIndex];

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    commandDataGrid.SelectedItem = commandDataGrid.Items[currentCommandIndex];
                }));

                if (currentCommand.commandType == CommandType.Wait)
                {
                    Thread.Sleep(int.Parse(currentCommand.data0));
                }
                else if(currentCommand.commandType == CommandType.Click)
                {
                    IntPoint point = GetPointFromString(currentCommand.data0);
                    DoMouseClickAtWindow(currentlySelectedWindow, point.x, point.y);
                    Thread.Sleep(int.Parse(currentCommand.data1));
                }
                else if (currentCommand.commandType == CommandType.Label)
                {
                    // Nothing for label
                }
                else if (currentCommand.commandType == CommandType.JumpToLabel)
                {
                    TryJumpToLabel(currentCommand.data0, ref currentCommandIndex, ref shouldMoveToNext);
                }
                else if (currentCommand.commandType == CommandType.IfColorGoToLabel)
                {
                    Rect windowRect = new Rect();
                    GetWindowRect(currentlySelectedWindow, ref windowRect);
                    IntPoint point = GetPointFromString(currentCommand.data0);
                    Color pixelColor = GetPixelColor(point.x + windowRect.Left, point.y + windowRect.Top);
                    Color desiredColor = ParseStringToColor(currentCommand.data1);

                    if(IsColorSimilar(pixelColor, desiredColor, int.Parse(currentCommand.data2)))
                    {
                        TryJumpToLabel(currentCommand.data3, ref currentCommandIndex, ref shouldMoveToNext);
                    }
                    else
                    {
                        Console.WriteLine("COLOR WAS DIFFERENT AT " + currentCommandIndex + "  " + colorToString(pixelColor) + "  " + colorToString(desiredColor));
                    }

                }
                else if (currentCommand.commandType == CommandType.WaitForColor)
                {
                    Color desiredColor = ParseStringToColor(currentCommand.data1);

                    while (true)
                    {
                        Rect windowRect = new Rect();
                        GetWindowRect(currentlySelectedWindow, ref windowRect);
                        IntPoint point = GetPointFromString(currentCommand.data0);
                        Color pixelColor = GetPixelColor(point.x + windowRect.Left, point.y + windowRect.Top);

                        if (IsColorSimilar(pixelColor, desiredColor, int.Parse(currentCommand.data2)))
                        {
                            break;
                        }
                        else
                        {
                            Console.WriteLine("COLOR WAS DIFFERENT" + currentCommandIndex + "  " + colorToString(pixelColor) + "  " + colorToString(desiredColor));
                            Thread.Sleep(1000);
                        }
                    }
                }
                else if (currentCommand.commandType == CommandType.SetVariable)
                {
                    if(macroVariables.ContainsKey(currentCommand.data0))
                    {
                        macroVariables[currentCommand.data0] = int.Parse(currentCommand.data1);
                    }
                    else
                    {
                        macroVariables.Add(currentCommand.data0, int.Parse(currentCommand.data1));
                    }
                }
                else if (currentCommand.commandType == CommandType.ChangeVariableBy)
                {
                    if (macroVariables.ContainsKey(currentCommand.data0))
                    {
                        macroVariables[currentCommand.data0] += int.Parse(currentCommand.data1);
                    }
                    else
                    {
                        macroVariables.Add(currentCommand.data0, int.Parse(currentCommand.data1));
                    }
                }
                else if (currentCommand.commandType == CommandType.IfVariableGoToLabel)
                {
                    if (!macroVariables.ContainsKey(currentCommand.data0))
                    {
                        macroVariables.Add(currentCommand.data0, 0);
                    }

                    /*
                        EQ,
                        NE,
                        GT,
                        LT,
                        GE,
                        LE
                    */

                    if(currentCommand.data1 == "EQ")
                    {
                        if(macroVariables[currentCommand.data0] == int.Parse(currentCommand.data2))
                        {
                            TryJumpToLabel(currentCommand.data3, ref currentCommandIndex, ref shouldMoveToNext);
                        }
                    }
                    else if (currentCommand.data1 == "NE")
                    {
                        if (macroVariables[currentCommand.data0] != int.Parse(currentCommand.data2))
                        {
                            TryJumpToLabel(currentCommand.data3, ref currentCommandIndex, ref shouldMoveToNext);
                        }
                    }
                    else if (currentCommand.data1 == "GT")
                    {
                        if (macroVariables[currentCommand.data0] > int.Parse(currentCommand.data2))
                        {
                            TryJumpToLabel(currentCommand.data3, ref currentCommandIndex, ref shouldMoveToNext);
                        }
                    }
                    else if (currentCommand.data1 == "LT")
                    {
                        if (macroVariables[currentCommand.data0] < int.Parse(currentCommand.data2))
                        {
                            TryJumpToLabel(currentCommand.data3, ref currentCommandIndex, ref shouldMoveToNext);
                        }
                    }
                    else if (currentCommand.data1 == "GE")
                    {
                        if (macroVariables[currentCommand.data0] >= int.Parse(currentCommand.data2))
                        {
                            TryJumpToLabel(currentCommand.data3, ref currentCommandIndex, ref shouldMoveToNext);
                        }
                    }
                    else if (currentCommand.data1 == "LE")
                    {
                        if (macroVariables[currentCommand.data0] <= int.Parse(currentCommand.data2))
                        {
                            TryJumpToLabel(currentCommand.data3, ref currentCommandIndex, ref shouldMoveToNext);
                        }
                    }
                }
                else if(currentCommand.commandType == CommandType.Drag)
                {
                    // Drag sometimes need mouse to be on top of window
                    IntPoint startPoint = GetPointFromString(currentCommand.data0);
                    IntPoint endPoint = GetPointFromString(currentCommand.data1);

                    LeftMouseDownAtWindow(currentlySelectedWindow, startPoint.x, startPoint.y);

                    int duration = int.Parse(currentCommand.data2);

                    float amount = 0;

                    while(amount < 1)
                    {
                        IntPoint dragPoint = IntPoint.LerpPoint(startPoint, endPoint, amount);
                        Console.WriteLine("DRAGGING TO " + dragPoint.x + "  " + dragPoint.y);
                        MoveMouseAtWindow(currentlySelectedWindow, dragPoint.x, dragPoint.y);
                        amount += 15f / duration;
                        Thread.Sleep(15);
                    }

                    MoveMouseAtWindow(currentlySelectedWindow, endPoint.x, endPoint.y);
                    Thread.Sleep(100);

                    LeftMouseUpAtWindow(currentlySelectedWindow, endPoint.x, endPoint.y);

                }
                else if(currentCommand.commandType == CommandType.ScreenShot)
                {
                    string dateString = DateTime.Now.ToString("MM-dd-yyyy h-mm-tt");
                    TakeScreenShot(currentCommand.data0 + dateString + ".png");
                }
                else if (currentCommand.commandType == CommandType.Flash)
                {
                    FlashWindow();
                }

                if (shouldMoveToNext)
                {
                    currentCommandIndex++;
                }

                Thread.Sleep(50);
            }

            macroShouldEnd = false;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                startButton.Content = "Start Macro";
                startFromSelectedButton.Content = "Start From Here";
            }));
        }

        private void TryJumpToLabel(string label, ref int currentCommandIndex, ref bool shouldMoveToNext)
        {
            int labelIndex = GetLabelIndex(label);

            if (labelIndex != -1)
            {
                currentCommandIndex = labelIndex;
                shouldMoveToNext = false;
            }
            else
            {
                Console.WriteLine("COULD NOT FIND LABEL " + label);
            }
        }

        // Return commands index of the label, returns -1 if not found
        private int GetLabelIndex(string labelStr)
        {
            for (int i = 0; i < commands.Count; ++i)
            {
                Command labelCommand = commands[i];
                if (labelCommand.commandType == CommandType.Label && labelCommand.data0 == labelStr)
                {
                    return i;
                }
            }

            return -1;
        }

        private void LabelUpdate()
        {
            while (!programClosing)
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
                    absoluteRect.Fill = new SolidColorBrush(c);

                    int width = windowRect.Right - windowRect.Left;
                    int height = windowRect.Bottom - windowRect.Top;
                    windowCoordinateLabel.Content = "Handle is " + currentlySelectedWindow + " X: " + windowRect.Left + "  Y: " + windowRect.Top + "  " + width + "  " + height;

                    /*
                    Color windowColor = GetPixelColorFromWindow(currentlySelectedWindow, int.Parse(xCoordinateBox.Text), int.Parse(yCoordinateBox.Text));
                    relativeLabel.Content = xCoordinateBox.Text + ", " + yCoordinateBox.Text + "      color: " + windowColor.R + " " + windowColor.G + " " + windowColor.B;
                    */

                    Color windowColor = GetPixelColorFromWindow(IntPtr.Zero, int.Parse(xCoordinateBox.Text) + windowRect.Left, int.Parse(yCoordinateBox.Text) + windowRect.Top);
                    relativeLabel.Content = xCoordinateBox.Text + ", " + yCoordinateBox.Text + "      color: " + windowColor.R + " " + windowColor.G + " " + windowColor.B;
                    relativeRect.Fill = new SolidColorBrush(windowColor);

                    Color windowColor2 = GetPixelColorFromWindow(IntPtr.Zero, (int)p.X, (int)p.Y);
                    referenceLabel.Content = ((int)p.X - windowRect.Left) + ", " + ((int)p.Y - windowRect.Top) + "      color: " + windowColor2.R + " " + windowColor2.G + " " + windowColor2.B;
                    referenceRect.Fill = new SolidColorBrush(windowColor2);

                    /*
                    Console.Write((200 + windowRect.Left) + ",  " + (200 + windowRect.Top) + "   ");
                    Color pixelColor = GetPixelColor(200 + windowRect.Left, 200 + windowRect.Top);
                    Console.WriteLine(pixelColor.R + " " + pixelColor.G + " " + pixelColor.B);
                    */
                }));

                Thread.Sleep(500);
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

        public static IntPoint GetPointFromString(string str)
        {
            string[] values = str.Split(',');
            return new IntPoint(int.Parse(values[0]), int.Parse(values[1]));
        }

        public static Color ParseStringToColor(string str)
        {
            string[] values = str.Split(',');
            return Color.FromRgb(byte.Parse(values[0]), byte.Parse(values[1]), byte.Parse(values[2]));
        }

        public static bool IsColorSimilar(Color c1, Color c2, int tolerance)
        {
            int diffR = Math.Abs(c1.R - c2.R);
            int diffG = Math.Abs(c1.G - c2.G);
            int diffB = Math.Abs(c1.B - c2.B);

            return diffR <= tolerance && diffG <= tolerance && diffB <= tolerance;
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".ayy";
            dlg.Filter = "AYYYY Files (*.ayy)|*.ayy";


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                Console.WriteLine("OPENING FILE IS " + filename);
                csvHandler.OpenFile(filename);
                commands = csvHandler.ParseCurrentFile();
                commandDataGrid.DataContext = commands;
            }
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(csvHandler.currentFileName))
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = "Document"; // Default file name
                dlg.DefaultExt = ".ayy"; // Default file extension
                dlg.Filter = "AYYYY Files (*.ayy)|*.ayy";

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    // Save document
                    string filename = dlg.FileName;
                    csvHandler.SaveCurrentCommandsAs(filename, commands);
                }
            }
            else
            {
                csvHandler.SaveCurrentCommands(commands);
            }
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".ayy"; // Default file extension
            dlg.Filter = "AYYYY Files (*.ayy)|*.ayy";

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string filename = dlg.FileName;
                csvHandler.SaveCurrentCommandsAs(filename, commands);
            }
        }

        private void commandDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex()).ToString();
        }

        private void insertRowButton_Click(object sender, RoutedEventArgs e)
        {
            if(commandDataGrid.SelectedIndex <= commands.Count && commandDataGrid.SelectedIndex >= 0)
            {
                commands.Insert(commandDataGrid.SelectedIndex, new Command());

                for (int i = 0; i < commandDataGrid.Items.Count; i++)
                {
                    DataGridRow row = (DataGridRow)commandDataGrid.ItemContainerGenerator.ContainerFromIndex(i);
                    if(row != null)
                    {
                        row.Header = row.GetIndex().ToString();
                    }
                }
            }
        }

        private void commandDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            for (int i = 0; i < commandDataGrid.Items.Count; i++)
            {
                DataGridRow row = (DataGridRow)commandDataGrid.ItemContainerGenerator.ContainerFromIndex(i);
                if (row != null)
                {
                    row.Header = row.GetIndex().ToString();
                }
            }
        }

        private void commandDataGrid_UnloadingRow(object sender, DataGridRowEventArgs e)
        {
            for (int i = 0; i < commandDataGrid.Items.Count; i++)
            {
                DataGridRow row = (DataGridRow)commandDataGrid.ItemContainerGenerator.ContainerFromIndex(i);
                if (row != null)
                {
                    row.Header = row.GetIndex().ToString();
                }
            }
        }

        private void startFromSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (macroThread == null || !macroThread.IsAlive)
            {
                startButton.Content = "Stop Macro";
                startFromSelectedButton.Content = "Stop Macro";
                int tempInt = commandDataGrid.SelectedIndex;
                if(tempInt < 0)
                {
                    tempInt = 0;
                }
                macroThread = new Thread(() => MacroUpdate(tempInt));
                macroThread.Start();
            }
            else
            {
                startButton.Content = "Start Macro";
                startFromSelectedButton.Content = "Start From Here";
                macroShouldEnd = true;
            }
        }

        public static float Lerp(float value1, float value2, float amount)
        {
            return value1 + (value2 - value1) * amount;
        }

        public void TakeScreenShot()
        {
            string dateString = DateTime.Now.ToString("MM-dd-yyyy h-mm-tt");
            TakeScreenShot("c:\\Users\\Joseph\\Desktop\\bots\\screenshots\\" + dateString + ".png");
        }

        public void TakeScreenShot(string fileName)
        {
            Rect rect = new Rect();
            GetWindowRect(currentlySelectedWindow, ref rect);

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bmp);
            graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height), System.Drawing.CopyPixelOperation.SourceCopy);

            bmp.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
        }



        private void screenShotButton_Click(object sender, RoutedEventArgs e)
        {
            TakeScreenShot();
        }

        public string colorToString(Color c)
        {
            return "" + c.R + " " + c.G + " " + c.B;
        }

        private void setWindowSize_Click(object sender, RoutedEventArgs e)
        {
            Rect rect = new Rect();
            GetWindowRect(currentlySelectedWindow, ref rect);
            // MoveWindow(currentlySelectedWindow, rect.Left, rect.Top, 651, 437, true);
            MoveWindow(currentlySelectedWindow, rect.Left, rect.Top, 371, 800, true);
        }

        private void FlashWindow()
        {
            Console.WriteLine("FLASHING WINDOW " + Process.GetCurrentProcess().MainWindowHandle);
            FLASHWINFO fInfo = new FLASHWINFO();

            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = Process.GetCurrentProcess().MainWindowHandle;
            fInfo.dwFlags = FLASHW_ALL;
            fInfo.uCount = 1;
            fInfo.dwTimeout = 0;

            FlashWindowEx(ref fInfo);
        }
    }
}
