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
using System.Text;
using System.Windows.Media;
using System.Collections.Generic;
using SmartLogging;

namespace SmartLogReader
{
    /// <summary>
    /// 
    /// </summary>
    public class Record : Notifier
    {
        /// <summary>
        /// 
        /// </summary>
        public Record()
        {
            RecordNum = ++count;
            Level = LogLevel.None;
        }
        static ulong count;

        /// <summary>
        /// The record time in UTC.
        /// </summary>
        public DateTime UtcTime { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public string Logger { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public LogLevel Level { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public int ProcessId { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public int AppDomainId { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public int ThreadId { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ulong RecordNum { get; set; }

        /// <summary>
        /// On set, the string is assumed to hold the time in UTC.
        /// On get, it will return the local time.
        /// </summary>
        public string TimeString
        {
            get { return timeString; }
            set
            {
                timeString = value.Replace(',', '.');
                timeString = timeString.Replace('T', ' ');
                timeString = timeString.Replace('Z', ' ');
                DateTime time;
                if (DateTime.TryParse(timeString, out time))
                {
                    UtcTime = DateTime.SpecifyKind(time, DateTimeKind.Utc);
                    time = UtcTime.ToLocalTime();
                    //--- even if the log file has more than 3 digits for the seconds, 
                    //--- the resolution will not be higher than a millisecond!!!
                    timeString = time.ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
            }
        }
        string timeString;

        /// <summary>
        /// 
        /// </summary>
        public string ThreadIds
        {
            get { return threadIds; }
            set
            {
                threadIds = value;
                string[] s = threadIds.Split(new char[] { '/' });
                if (s.Length == 3)
                {
                    int id;
                    if (int.TryParse(s[0], out id)) ProcessId = id;
                    if (int.TryParse(s[1], out id)) AppDomainId = id;
                    if (int.TryParse(s[2], out id)) ThreadId = id;
                }
            }
        }
        string threadIds;

        /// <summary>
        /// 
        /// </summary>
        public string LevelString
        {
            get { return levelString; }
            set
            {
                levelString = value;
                LogLevel level;
                if (Enum.TryParse(value, true, out level))
                    Level = level;
                else
                    Level = TryParseLevel(value);
            }
        }

        private LogLevel TryParseLevel(string value)
        {
            if (value.contains("dbg") || value.contains("debug"))
                return LogLevel.Debug;

            if (value.contains("inf") || value.contains("info"))
                return LogLevel.Info;

            if (value.contains("wrn") || value.contains("warn"))
                return LogLevel.Warn;

            if (value.contains("err"))
                return LogLevel.Error;

            if (value.contains("ftl") || value.contains("fatal"))
                return LogLevel.Fatal;

            return LogLevel.None;
        }

        string levelString;

        /// <summary>
        /// Shows up in QuickFilterDialog.
        /// </summary>
        public string ShortMessage
        {
            get { return FirstLine(Message); }
        }

        /// <summary>
        /// Shows up as tooltip.
        /// </summary>
        public string MessageToolTip
        {
            get { return string.IsNullOrEmpty(Message) ? "no message" : Message; }
        }

        /// <summary>
        /// Shows up in the main list.
        /// </summary>
        public string ShortString
        {
            get { return GetPrefix(false) + FirstLine(Message); }
        }

        /// <summary>
        /// Shows up in the DetailsWindow and the clipboard.
        /// </summary>
        public string LongString
        {
            get
            {
                string result = GetPrefix(true) + Message;
                return result;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private string GetPrefix(bool showAll)
        {
            StringBuilder sb = new StringBuilder();

            if (showAll || ShowTime)
                sb.Append(Align(TimeString, AmountOfTime, showAll));

            if (showAll || ShowLevel)
                sb.Append(Align(LevelString, AmountOfLevel, showAll, true));

            if (showAll || ShowThreadIds)
                sb.Append(Align(ThreadIds, AmountOfThreadIds, showAll));

            if (showAll || ShowLogger)
                sb.Append(Align(Logger, AmountOfLogger, showAll));

            if (showAll || ShowMethod)
                sb.Append(Align(Method, AmountOfMethod, showAll));

            return sb.ToString();
        }

        public static bool ShowTime = true;
        public static bool ShowLogger = true;
        public static bool ShowLevel = false;
        public static bool ShowThreadIds = false;
        public static bool ShowMethod = true;

        public static int AmountOfTime = 12;
        public static int AmountOfLogger = 23;
        public static int AmountOfLevel = 5;
        public static int AmountOfThreadIds = 9;
        public static int AmountOfMethod = 23;

        /// <summary>
        /// 
        /// </summary>
        private string Align(string str, int length, bool showAll, bool fromStart = false)
        {
            if (str == null)
                return "";

            if (!showAll)
            {
                if (str.Length > length)
                {
                    if (fromStart)
                        str = str.Substring(0, length);
                    else
                        str = str.Substring(str.Length - length);
                }

                if (str.Length < length)
                {
                    StringBuilder sb = new StringBuilder(str);
                    int maxi = length - str.Length;

                    for (int i = 0; i < maxi; i++)
                        sb.Append(" ");

                    str = sb.ToString();
                }
            }

            return str + "  ";
        }

        /// <summary>
        /// 
        /// </summary>
        public override string ToString()
        {
            return ShortString;
        }

        /// <summary>
        /// 
        /// </summary>
        string FirstLine(string str)
        {
            int cr = str.IndexOf('\r');
            int lf = str.IndexOf('\n');
            int limit = 240;

            //--- index not found?
            if (cr < 0 && lf < 0)
            {
                if (str.Length > limit)
                    return str.Substring(0, limit);

                return str;
            }

            //--- take the smaller index
            int index = cr < lf ? cr : lf;

            //--- if this is invalid, take the other
            if (index < 0)
                index = cr < 0 ? lf : cr;

            return str.Substring(0, Math.Min(limit, index));
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Contains(string text)
        {
            if (Message != null && Message.contains(text))
                return true;

            if (timeString != null && timeString.contains(text))
                return true;

            if (Logger != null && Logger.contains(text))
                return true;

            if (levelString != null && levelString.contains(text))
                return true;

            if (threadIds != null && threadIds.contains(text))
                return true;

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public Brush Background
        {
            get
            {
                Brush brush = Brushes.Transparent;
                if (!IsSelected && ColorSpecs != null)
                {
                    foreach (var colorSpec in ColorSpecs)
                    {
                        if (colorSpec.Test(this))
                        {
                            brush = ColorBox.GetBrush(colorSpec.ColorIndex);
                            break;
                        }
                    }
                }
                return brush;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged();
                    OnPropertyChanged("Background");
                }
            }
        }
        private bool isSelected;

        /// <summary>
        /// 
        /// </summary>
        static public ColorSpecCollection ColorSpecs { get; set; }

        /// <summary>
        /// 
        /// </summary>
        static public ColorSpecCollection GetDefaultColorSpecs()
        {
            ColorSpecCollection colorSpecs = new ColorSpecCollection();
            colorSpecs.Add(NewLevelColorSpec("Info", 0));
            colorSpecs.Add(NewLevelColorSpec("Warn", 3));
            colorSpecs.Add(NewLevelColorSpec("Error", 2));
            colorSpecs.Add(NewLevelColorSpec("Fatal", 11));
            return colorSpecs;
        }

        static ColorSpec NewLevelColorSpec(string logLevel, int colorIndex)
        {
            ColorSpec colorSpec = new ColorSpec() { PropertyIndex = 1, OpCodeIndex = 0, ExpectedValue = logLevel, ColorIndex = colorIndex };
            return colorSpec;
        }
    }

    public class RecordCollection : List<Record>
    {
        public RecordCollection()
        {
        }

        public RecordCollection(IEnumerable<Record> collection)
            : base(collection)
        {
        }

        public RecordCollection(List<Record> collection)
            : base(collection)
        {
        }
    }
}
