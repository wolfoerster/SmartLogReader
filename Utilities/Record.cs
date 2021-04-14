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
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;

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

        public string Json { get; set; }

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
                if (DateTime.TryParse(timeString, out DateTime time))
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
        public string TimeDiffString => (this.UtcTime - UtcTime0).TotalMilliseconds.ToString("F0");

        /// <summary>
        /// 
        /// </summary>
        public static DateTime UtcTime0;

        /// <summary>
        /// 
        /// </summary>
        public string ThreadIds
        {
            get { return threadIds; }
            set
            {
                threadIds = value;
                if (!string.IsNullOrWhiteSpace(threadIds))
                {
                    string[] s = threadIds.Split(new char[] { '/' });
                    if (s.Length == 3)
                    {
                        if (int.TryParse(s[0], out int processId)) ProcessId = processId;
                        if (int.TryParse(s[1], out int appDomainId)) AppDomainId = appDomainId;
                        if (int.TryParse(s[2], out int threadId)) ThreadId = threadId;
                    }
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
                if (Enum.TryParse(value, true, out LogLevel level))
                    Level = level;
                else
                    Level = TryParseLevel(value);
            }
        }

        private LogLevel TryParseLevel(string value)
        {
            if (value.contains("verbose") || value.contains("trace"))
                return LogLevel.Verbose;

            if (value.contains("dbg") || value.contains("debug"))
                return LogLevel.Debug;

            if (value.contains("inf") || value.contains("info"))
                return LogLevel.Information;

            if (value.contains("wrn") || value.contains("warn"))
                return LogLevel.Warning;

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
            get { return string.IsNullOrEmpty(Message) ? "no message" : JsonMessage; }
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
                if (Json != null)
                    return Beautify(Json);

                return GetPrefix(true) + JsonMessage;
            }
        }

        private string JsonMessage
        {
            get
            {
                if (Message.StartsWith("{"))
                    return Beautify(Message);

                return Message;
            }
        }

        private static string Beautify(string json)
        {
            try
            {
                var jToken = JValue.Parse(json);
                return jToken.ToString(Formatting.Indented);
            }
            catch (Exception exception)
            {
                var message = SimpleLogger.GetMessage(exception);
                Trace.WriteLine(message);
            }

            return json;
        }

        /// <summary>
        /// 
        /// </summary>
        private string GetPrefix(bool showAll)
        {
            StringBuilder sb = new StringBuilder();

            if (showAll || ShowTime)
                sb.Append(Align(TimeString, AmountOfTime, showAll));

            if (ShowTimeDiff)
                sb.Append(Align(TimeDiffString, AmountOfTimeDiff, showAll));

            if (showAll || ShowThreadIds)
                sb.Append(Align(ThreadIds, AmountOfThreadIds, showAll));

            if (showAll || ShowLevel)
                sb.Append(Align(LevelString, AmountOfLevel, showAll, true));

            if (showAll || ShowLogger)
                sb.Append(Align(Logger, AmountOfLogger, showAll));

            if (showAll || ShowMethod)
                sb.Append(Align(Method, AmountOfMethod, showAll));

            return sb.ToString();
        }

        public static bool ShowTime = true;
        public static bool ShowTimeDiff = true;
        public static bool ShowThreadIds = false;
        public static bool ShowLevel = false;
        public static bool ShowLogger = true;
        public static bool ShowMethod = true;

        public static int AmountOfTime = 12;
        public static int AmountOfTimeDiff = 6;
        public static int AmountOfThreadIds = 9;
        public static int AmountOfLevel = 5;
        public static int AmountOfLogger = 23;
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
            if (str == null)
                return string.Empty;

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
            ColorSpec newSpec(string logLevel, int colorIndex)
            {
                ColorSpec colorSpec = new ColorSpec() { PropertyIndex = 4, OpCodeIndex = 0, ExpectedValue = logLevel, ColorIndex = colorIndex };
                return colorSpec;
            }

            ColorSpecCollection colorSpecs = new ColorSpecCollection
            {
                newSpec("Debug", 1),
                newSpec("Information", 0),
                newSpec("Warning", 3),
                newSpec("Error", 2),
                newSpec("Fatal", 11),
                newSpec("None", 6)
            };
            return colorSpecs;
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
