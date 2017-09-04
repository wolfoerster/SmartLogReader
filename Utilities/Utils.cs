//******************************************************************************************
// Copyright © 2017 Wolfgang Foerster (wolfoerster@gmx.de)
//
// This file is part of the SmartLogReader project which can be found on github.com
//
// SmartLogReader is free software: you can redistribute it and/or modify it under the terms 
// of the GNU General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version.
// 
// SmartLogReader is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
//******************************************************************************************
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using SmartLogging;

namespace SmartLogReader
{
    public class Screen
    {
        public Rect ScreenArea;
        public Rect WorkArea;
        public bool IsPrimary;
        public string Name;
    }

    public static class Utils
    {
        private static readonly SmartLogger log = new SmartLogger();

        /// <summary>
        /// Gets a value indicating if Left-Shift or Right-Shift is down.
        /// </summary>
        public static bool IsShiftDown()
        {
            return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        }

        /// <summary>
        /// Gets a value indicating if Left-Ctrl or Right-Ctrl is down.
        /// </summary>
        public static bool IsCtrlDown()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        /// <summary>
        /// Walks up the visual tree and finds the parent of desired type.
        /// </summary>
        public static T FindParent<T>(DependencyObject targetObject)
            where T : DependencyObject
        {
            if (targetObject == null)
                return null;

            DependencyObject parent = VisualTreeHelper.GetParent(targetObject);
            while (parent != null)
            {
                if (parent is T)
                    return parent as T;
                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        /// <summary>
        /// Walks down the visual tree and finds the child of desired type.
        /// </summary>
        public static T FindChild<T>(DependencyObject targetObject, string name)
            where T : DependencyObject
        {
            if (targetObject == null)
                return null;

            int noOfChildren = VisualTreeHelper.GetChildrenCount(targetObject);
            for (int i = 0; i < noOfChildren; ++i)
            {
                DependencyObject child = VisualTreeHelper.GetChild(targetObject, i);
                if (child is T)
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        return child as T;
                    }
                    else if (child is FrameworkElement && (child as FrameworkElement).Name.Equals(name))
                    {
                        return child as T;
                    }
                }
                T result = FindChild<T>(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public static string ToXML<T>(T instance) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StringWriter writer = new StringWriter();
            serializer.Serialize(writer, instance);
            string xml = writer.ToString();
            return xml;
        }

        /// <summary>
        /// 
        /// </summary>
        public static T FromXML<T>(string xml) where T : class
        {
            object obj = default(T);
            if (!string.IsNullOrWhiteSpace(xml))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    using (StringReader reader = new StringReader(xml))
                    {
                        T data = serializer.Deserialize(reader) as T;
                        return data;
                    }
                }
                catch (Exception e)
                {
                    log.Exception(e);
                }
            }
            return obj as T;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool DeleteFile(string path)
        {
            if (!File.Exists(path))
                return true;

            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception e)
            {
                log.Exception(e);
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool CopyFile(string source, string dest)
        {
            try
            {
                File.Copy(source, dest, true);
                return true;
            }
            catch (Exception e)
            {
                log.Exception(e);
            }
            return false;
        }

        /// <summary>
        /// Clamps the specified x value to the given range.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="xMin">The minimum x value.</param>
        /// <param name="xMax">The maximum x value.</param>
        /// <returns></returns>
        public static double Clamp(double x, double xMin, double xMax)
        {
            return Math.Min(Math.Max(x, xMin), xMax);
        }

        public static int Clamp(int x, int xMin, int xMax)
        {
            return Math.Min(Math.Max(x, xMin), xMax);
        }

        #region ExtendFrameIntoClientArea

        public static bool ExtendFrameIntoClientArea(Window window, Thickness margin)
        {
            try
            {
                if (!DwmIsCompositionEnabled())
                    return false;
            }
            catch
            {
                return false;
            }

            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
                //throw new InvalidOperationException("The Window must be shown before extending glass.");
                return false;

            // Set the background to transparent from both the WPF and Win32 perspectives
            window.Background = Brushes.Transparent;
            HwndSource.FromHwnd(hwnd).CompositionTarget.BackgroundColor = Colors.Transparent;

            MARGINS margins = new MARGINS(margin);
            DwmExtendFrameIntoClientArea(hwnd, ref margins);
            return true;
        }
        [DllImport("dwmapi.dll", PreserveSig = false)]
        static extern void DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        static extern bool DwmIsCompositionEnabled();

        struct MARGINS
        {
            public MARGINS(Thickness t)
            {
                Left = (int)t.Left;
                Right = (int)t.Right;
                Top = (int)t.Top;
                Bottom = (int)t.Bottom;
            }
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        #endregion ExtendFrameIntoClientArea

        #region GetAllScreens

        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
           MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

        // size of a device name string
        const int CCHDEVICENAME = 32;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MonitorInfoEx
        {
            public int Size;

            public RectStruct Monitor;

            public RectStruct WorkArea;

            public uint Flags;//--- first bit = MONITORINFOF_PRIMARY

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string DeviceName;

            public void Init()
            {
                this.Size = 40 + 2 * CCHDEVICENAME;
                this.DeviceName = string.Empty;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RectStruct
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        static public List<Screen> GetAllScreens()
        {
            List<Screen> screens = new List<Screen>();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                    delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
                    {
                        MonitorInfoEx mi = new MonitorInfoEx();
                        mi.Size = (int)Marshal.SizeOf(mi);
                        bool success = GetMonitorInfo(hMonitor, ref mi);
                        if (success)
                        {
                            Screen screen = new Screen()
                            {
                                ScreenArea = new Rect(mi.Monitor.Left, mi.Monitor.Top, mi.Monitor.Right - mi.Monitor.Left, mi.Monitor.Bottom - mi.Monitor.Top),
                                WorkArea = new Rect(mi.WorkArea.Left, mi.WorkArea.Top, mi.WorkArea.Right - mi.WorkArea.Left, mi.WorkArea.Bottom - mi.WorkArea.Top),
                                IsPrimary = (mi.Flags & 1) == 1,
                                Name = mi.DeviceName
                            };
                            screens.Add(screen);
                        }
                        return true;
                    }, IntPtr.Zero);

            return screens;
        }

        static public Screen GetScreenByName(string name)
        {
            log.Smart($"name = {name}");
            if (string.IsNullOrWhiteSpace(name))
                return null;

            List<Screen> screens = GetAllScreens();
            foreach (var screen in screens)
            {
                log.Smart($"screen.Name = {screen.Name}");
                if (screen.Name == name)
                    return screen;
            }
            log.Smart("not found");
            return null;
        }

        static public Screen GetScreenByPixel(Point pt)
        {
            log.Smart($"pt = {pt}");
            List<Screen> screens = GetAllScreens();
            foreach (var screen in screens)
            {
                log.Smart($"{screen.Name}.WorkArea = {screen.WorkArea}");
                if (screen.WorkArea.Contains(pt))
                    return screen;
            }
            log.Smart("not found");
            return null;
        }

        static public Screen GetScreenByPixel(double x, double y)
        {
            return GetScreenByPixel(new Point(x, y));
        }

        static public Screen GetPrimaryScreen()
        {
            List<Screen> screens = GetAllScreens();
            foreach (var screen in screens)
            {
                if (screen.IsPrimary)
                    return screen;
            }
            return null;
        }

        #endregion GetAllScreens

        #region GetObjectId

        /// <summary>
        /// 
        /// </summary>
        static public string GetObjectIdString(object obj)
        {
            int id = GetObjectId(obj);
            return "Object#" + id.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        static public int GetObjectId(object obj)
        {
            int code = obj.GetHashCode();
            if (IDs.ContainsKey(code))
                return IDs[code];

            return IDs[code] = ++count;
        }
        static int count;

        static Dictionary<int, int> IDs = new Dictionary<int, int>();

        #endregion GetObjectId

        #region ScrollIntoCenter

        public static void ScrollIntoCenter(this ListBox listBox, object item, int index)
        {
            //--- 1. do it immediately if possible
            if (TryScrollIntoCenter(listBox, item, index))
                return;

            //--- 2. scroll into view
            listBox.ScrollIntoView(item);

            //--- 3. do it delayed
            listBox.Dispatch(() => TryScrollIntoCenter(listBox, item, index));
        }

        private static bool TryScrollIntoCenter(this ListBox listBox, object item, int index)
        {
            // Find the container
            var listBoxItem = listBox.ItemContainerGenerator.ContainerFromItem(item) as UIElement;
            if (listBoxItem == null)
                return false;

            // Find the ScrollContentPresenter
            ScrollContentPresenter presenter = null;
            for (Visual vis = listBoxItem; vis != null && vis != listBox; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if ((presenter = vis as ScrollContentPresenter) != null)
                    break;
            if (presenter == null)
                return false;

            // Find the IScrollInfo
            var scrollInfo =
                !presenter.CanContentScroll ? presenter :
                presenter.Content as IScrollInfo ??
                FirstVisualChild(presenter.Content as ItemsPresenter) as IScrollInfo ??
                presenter;

            //--- is item already visible?
            int firstVisibleIndex = (int)scrollInfo.VerticalOffset;
            int numVisibleItems = (int)scrollInfo.ViewportHeight;
            if (index >= firstVisibleIndex && index < firstVisibleIndex + numVisibleItems)
                return true;

            // Compute the center point of the container relative to the scrollInfo 
            Size size = listBoxItem.RenderSize;
            Point center = listBoxItem.TransformToVisual((UIElement)scrollInfo).Transform(new Point(size.Width / 2, size.Height / 2));
            center.Y += scrollInfo.VerticalOffset;
            center.X += scrollInfo.HorizontalOffset;

            // Adjust for logical scrolling 
            if (scrollInfo is StackPanel || scrollInfo is VirtualizingStackPanel)
            {
                double logicalCenter = listBox.ItemContainerGenerator.IndexFromContainer(listBoxItem) + 0.5;
                Orientation orientation = scrollInfo is StackPanel ? ((StackPanel)scrollInfo).Orientation : ((VirtualizingStackPanel)scrollInfo).Orientation;
                if (orientation == Orientation.Horizontal)
                    center.X = logicalCenter;
                else
                    center.Y = logicalCenter;
            }

            // Scroll the center of the container to the center of the viewport 
            if (scrollInfo.CanVerticallyScroll)
            {
                double offset = CenteringOffset(center.Y, scrollInfo.ViewportHeight, scrollInfo.ExtentHeight);
                scrollInfo.SetVerticalOffset(offset);
            }

            return true;
        }

        private static double CenteringOffset(double center, double viewport, double extent)
        {
            return Math.Min(extent - viewport, Math.Max(0, center - viewport / 2));
        }

        private static DependencyObject FirstVisualChild(UIElement visual)
        {
            if (visual == null) return null;
            if (VisualTreeHelper.GetChildrenCount(visual) == 0) return null;
            return VisualTreeHelper.GetChild(visual, 0);
        }

#if OriginalCode

http://stackoverflow.com/questions/2946954/make-listview-scrollintoview-scroll-the-item-into-the-center-of-the-listview-c

https://social.msdn.microsoft.com/Forums/vstudio/en-US/9efbbd24-9780-4381-90cc-a555d4782cd8/how-can-i-know-if-a-listbox-item-is-visible?forum=wpf

		public static void ScrollToCenterOfView(this ItemsControl itemsControl, object item)
		{
			// Scroll immediately if possible
			if (!itemsControl.TryScrollToCenterOfView(item))
			{
				// Otherwise wait until everything is loaded, then scroll
				if (itemsControl is ListBox) ((ListBox)itemsControl).ScrollIntoView(item);

				itemsControl.Dispatcher.BeginInvoke(new Action(() =>
				{
					itemsControl.TryScrollToCenterOfView(item);
				}));
			}
		}

		private static bool TryScrollToCenterOfView(this ItemsControl itemsControl, object item)
		{
			// Find the container
			var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as UIElement;
			if (container == null) return false;

			// Find the ScrollContentPresenter
			ScrollContentPresenter presenter = null;
			for (Visual vis = container; vis != null && vis != itemsControl; vis = VisualTreeHelper.GetParent(vis) as Visual)
				if ((presenter = vis as ScrollContentPresenter) != null)
					break;
			if (presenter == null) return false;

			// Find the IScrollInfo
			var scrollInfo =
				!presenter.CanContentScroll ? presenter :
				presenter.Content as IScrollInfo ??
				FirstVisualChild(presenter.Content as ItemsPresenter) as IScrollInfo ??
				presenter;

			// Compute the center point of the container relative to the scrollInfo 
			Size size = container.RenderSize;
			Point center = container.TransformToVisual((UIElement)scrollInfo).Transform(new Point(size.Width / 2, size.Height / 2));
			center.Y += scrollInfo.VerticalOffset;
			center.X += scrollInfo.HorizontalOffset;

			// Adjust for logical scrolling 
			if (scrollInfo is StackPanel || scrollInfo is VirtualizingStackPanel)
			{
				double logicalCenter = itemsControl.ItemContainerGenerator.IndexFromContainer(container) + 0.5;
				Orientation orientation = scrollInfo is StackPanel ? ((StackPanel)scrollInfo).Orientation : ((VirtualizingStackPanel)scrollInfo).Orientation;
				if (orientation == Orientation.Horizontal)
					center.X = logicalCenter;
				else
					center.Y = logicalCenter;
			}

			// Scroll the center of the container to the center of the viewport 
			if (scrollInfo.CanVerticallyScroll)
				scrollInfo.SetVerticalOffset(CenteringOffset(center.Y, scrollInfo.ViewportHeight, scrollInfo.ExtentHeight));

			if (scrollInfo.CanHorizontallyScroll)
				scrollInfo.SetHorizontalOffset(CenteringOffset(center.X, scrollInfo.ViewportWidth, scrollInfo.ExtentWidth));

			return true;
		}
#endif

        #endregion ScrollIntoCenter

        /// <summary>
        /// 
        /// </summary>
        public static void Dispatch(this DispatcherObject theObject, Action action, DispatcherPriority priority = DispatcherPriority.Render)
        {
            theObject.Dispatcher.BeginInvoke(priority,
                (DispatcherOperationCallback)delegate (object obj)
                {
                    action();
                    return null;
                },
                null);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void MoveToMouse(Window win, string title, bool useLastTopLeft = false)
        {
            Point pt = topLeft;
            if (!useLastTopLeft)
            {
                Window mainWindow = Application.Current.MainWindow;
                pt = Mouse.GetPosition(mainWindow);
                log.Smart($"Mouse.GetPosition(mainWindow) returned {pt}");
                pt = mainWindow.PointToScreen(pt);
                log.Smart($"mainWindow.PointToScreen(pt) returned {pt}");
                topLeft = pt;
            }

            Screen screen = GetScreenByPixel(pt);
            log.Smart($"screen.ScreenArea = {screen.ScreenArea}");

            double top = pt.Y - 80;
            if (top < screen.ScreenArea.Top)
                top = screen.ScreenArea.Top;

            double exceed = top + win.Height - screen.ScreenArea.Bottom + 40;
            if (exceed > 0)
                top -= exceed;

            double left = pt.X - 80;
            if (left < screen.ScreenArea.Left)
                left = screen.ScreenArea.Left;

            exceed = left + win.Width - screen.ScreenArea.Right;
            if (exceed > 0)
                left -= exceed;

            win.Top = top;
            win.Left = left;
            win.Title = title + "   (right click or Esc to cancel)";
        }
        static Point topLeft;

        /// <summary>
        /// 
        /// </summary>
        static public FileInfo GetFileInfo(string name)
        {
            if (!File.Exists(name))
            {
                log.Smart($"File >{name}< does not exist");
                return null;
            }
            try
            {
                return new FileInfo(name);
            }
            catch (Exception e)
            {
                log.Exception(e);
            }
            return null;
        }

        /// <summary>
        /// Performs a case insensitive Equals().
        /// </summary>
        static public bool equals(this string str, string value)
        {
            return str.Equals(value, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Performs a case insensitive StartsWith().
        /// </summary>
        static public bool startsWith(this string str, string value)
        {
            return str.StartsWith(value, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Performs a case insensitive Contains().
        /// </summary>
        static public bool contains(this string str, string value)
        {
            int index = str.IndexOf(value, StringComparison.InvariantCultureIgnoreCase);
            return index > -1;
        }

        internal static void OnlyOnce(object field, object value)
        {
            if (field != null)
                throw new Exception("Field must be null!");

            if (value == null)
                throw new Exception("Value must not be null!");
        }

        /// <summary>
        /// Rounds a double using the specified number of significant figures.
        /// </summary>
        /// <param name="value">A double value.</param>
        /// <param name="sigFigures">The number of significant figures.</param>
        /// <param name="roundingPosition">The rounding position.</param>
        /// <returns>The rounded double value.</returns>
        private static double RoundSignificantDigits(double value, int sigFigures, out int roundingPosition)
        {
            // this method will return a rounded double value at a number of signifigant figures.
            // the sigFigures parameter must be between 0 and 15, exclusive.
            roundingPosition = 0;

            if (double.IsNaN(value))
                return double.NaN;

            if (double.IsPositiveInfinity(value))
                return double.PositiveInfinity;

            if (double.IsNegativeInfinity(value))
                return double.NegativeInfinity;

            //--- WF: we have to set a limit somewhere
            if (Math.Abs(value) <= 1e-98)
                return 0;

            //--- WF: don't throw exceptions if sigFigures is out of range
            sigFigures = Clamp(sigFigures, 1, 14);

            // The resulting rounding position will be negative for rounding at whole numbers, and positive for decimal places.
            roundingPosition = sigFigures - 1 - (int)(Math.Floor(Math.Log10(Math.Abs(value))));

            // try to use a rounding position directly, if no scale is needed.
            // this is because the scale mutliplication after the rounding can introduce error, although 
            // this only happens when you're dealing with really tiny numbers, i.e 9.9e-14.
            if (roundingPosition > 0 && roundingPosition < 15)
                return Math.Round(value, roundingPosition, MidpointRounding.AwayFromZero);

            // Shouldn't get here unless we need to scale it.
            // Set the scaling value, for rounding whole numbers or decimals past 15 places
            double scale = Math.Pow(10, Math.Ceiling(Math.Log10(Math.Abs(value))));
            return Math.Round(value / scale, sigFigures, MidpointRounding.AwayFromZero) * scale;
        }

        /// <summary>
        /// Extended ToString() method. Converts the numeric value of this double to a string, using the specified format.<para/>
        /// There are two additional features on top of the basic double.ToString() implementation:<para/>
        /// 1. New format specifier 's' or 'S' to specify the number of significant figures.<para/>
        /// 2. Optimized scientific notation (remove dispensable characters, e.g. 1.2300e+004 will become 1.23e4)
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <param name="format">The format specifier. If this is null or empty, "g4" is used.</param>
        public static string ToStringExt(this double value, string format = null)
        {
            NumberFormatInfo currentInfo = CultureInfo.CurrentCulture.NumberFormat;

            //--- Do we have a special value?
            if (double.IsNaN(value))
                return currentInfo.NaNSymbol;

            if (double.IsPositiveInfinity(value))
                return currentInfo.PositiveInfinitySymbol;

            if (double.IsNegativeInfinity(value))
                return currentInfo.NegativeInfinitySymbol;

            if (string.IsNullOrWhiteSpace(format))
                format = "g4";

            if (format[0] == 's' || format[0] == 'S')
            {
                #region Significant figures

                // If you round '0.002' to 3 significant figures, the resulting string should be '0.00200'.
                int sigFigures;
                int.TryParse(format.Remove(0, 1), out sigFigures);

                int roundingPosition = 0;
                double roundedValue = RoundSignificantDigits(value, sigFigures, out roundingPosition);

                //--- 0 shall be formatted as 1 or any other integer < 10:
                if (roundedValue == 0.0d)
                {
                    sigFigures = Clamp(sigFigures, 1, 14);
                    return string.Format(currentInfo, "{0:F" + (sigFigures - 1) + "}", value);
                }

                // Check if the rounding position is positive and is not past 100 decimal places.
                // If the rounding position is greater than 100, string.format won't represent the number correctly.
                // ToDo:  What happens when the rounding position is greater than 100?
                if (roundingPosition > 0 && roundingPosition < 100)
                    return string.Format(currentInfo, "{0:F" + roundingPosition + "}", roundedValue);

                return roundedValue.ToString("F0", currentInfo);

                #endregion Significant figures
            }

            //--- Convert to string using format
            string text = value.ToString(format, currentInfo);

            //--- If text is not in scientific notation, just return it as is:
            int e = text.IndexOfAny(new char[] { 'e', 'E' });
            if (e < 0)
                return text;

            #region Optimize scientific notation

            //--- Remove trailing zeros and possibly decimal separator from the mantissa
            char sep = currentInfo.NumberDecimalSeparator[0];
            string mantissa = text.Substring(0, e);

            mantissa = mantissa.TrimEnd(new char[] { '0', sep });
            if (mantissa.Length == 0)
                return "0";

            //--- Remove leading zeros and possibly plus sign from the exponent
            char negativeSign = currentInfo.NegativeSign[0];
            char positiveSign = currentInfo.PositiveSign[0];

            string exponent = text.Substring(e + 1);
            bool isNegative = exponent[0] == negativeSign;

            exponent = exponent.Trim(new char[] { '0', positiveSign, negativeSign });
            if (exponent.Length == 0)
                return mantissa;

            //--- Build up the result
            if (isNegative)
                return mantissa + text[e] + negativeSign + exponent;

            return mantissa + text[e] + exponent;

            #endregion Optimise scientific notation
        }

        public static string GetFileSizeString(double fileSize)
        {
            string[] appendix = new string[] { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            for (int i = 0; i < appendix.Length; i++, fileSize /= 1e3)
            {
                if (fileSize < 1e3)
                {
                    string str = fileSize.ToStringExt("s3");
                    return $"{str} {appendix[i]}";
                }
            }

            return null;
        }

        public static string BytesToString(byte[] bytes, int index, int count)
        {
            //string result = ASCIIEncoding.ASCII.GetString(bytes, index, count);
            string result = Encoding.Default.GetString(bytes, index, count);
            return result;
        }
    }
}
