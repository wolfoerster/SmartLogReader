//******************************************************************************************
// Copyright © 2021 Wolfgang Foerster (wolfoerster@gmx.de)
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

namespace SmartLogReader
{
    using System;

    /// <summary>
    /// A byte parser for a plain text based logger (e.g. the SimpleLogger used in this app).
    /// 
    /// A log entry has to look like this:
    /// 2021-04-02 15:37:14.516 FATAL SmartLogReader.App 9760/1/1 .ctor Start logging
    /// 
    /// In general:
    /// Date Time Level ClassName ProcessId/AppDomainId/ThreadId MethodName Message
    /// 
    /// The message might span several lines. Everything from the begin of the message up to
    /// the next Date field is considered as message (including line feeds and spaces).
    /// </summary>
    public class ByteParserPlainText : ByteParser
    {
        public ByteParserPlainText(byte[] bytes)
        {
            if (CheckTime(bytes, 0))
            {
                Bytes = bytes;
            }
        }

        protected override void FillRecord(Record record)
        {
            record.TimeString = GetTime();
            record.ConnectionId = GetNext();
            record.LevelString = GetNext();
            record.Logger = GetNext();
            record.Method = GetNext();
            record.Message = GetRest();
        }

        protected override string GetNext(int numBytes = -1)
        {
            if (lastPos == bytes.Length)
                return null;

            int i = numBytes < 0 ? GetIndexOfNext(Space, CR, LF) : lastPos + numBytes;
            string result = GetString(i - lastPos);

            if (numBytes > 0)
                result = result.Trim();

            if (bytes[i] == Space)
                lastPos = GetIndexOfNextNot(Space);

            return result;
        }

        private string GetTime()
        {
            string s = GetNext(timeLength);
            if (isLocalTime)
            {
                if (DateTime.TryParse(s, out DateTime t))
                {
                    t = DateTime.SpecifyKind(t, DateTimeKind.Local);
                    s = t.ToUniversalTime().ToStringN();
                }
            }
            return s;
        }
        private int timeLength;
        private bool isLocalTime;

        private bool CheckTime(byte[] array, int index)
        {
            //--- check for one of the following
            //--- yyyy-MM-dd HH:mm:ss             (19 bytes)
            //--- yyyy-MM-dd HH:mm:ss.fff         (plus second fractions)
            //--- yyyy-MM-dd HH:mm:ss.fff ±hh:mm  (plus timezone shift)
            //--- yyyy-MM-ddTHH:mm:ss.ffffffZ     (Zulu time, i.e. UTC)

            int minTimeLength = 19;
            if (array.Length - index < minTimeLength)
                return false;

            int i = index + 4;
            if (array[i] != Minus)
                return false;

            i += 3;
            if (array[i] != Minus)
                return false;

            i += 3;
            if (array[i] != Space && array[i] != 'T')
                return false;

            i += 3;
            if (array[i] != Colon)
                return false;

            i += 3;
            if (array[i] != Colon)
                return false;

            timeLength = minTimeLength;
            isLocalTime = false;

            //--- are there fractions of seconds "2017-07-23 16:48:18.123" ?
            i += 3;
            if (IsAt(array, i, Point) || IsAt(array, i, Comma))
            {
                i = MoveToNextDelim(array, i);
                if (i < 0)
                    return false;

                timeLength = i - index;

                //--- is there a time zone shift "2017-07-23 16:48:18.123 +01:00" ?
                if (IsAt(array, i + 1, Plus) || IsAt(array, i + 1, Minus))
                {
                    if (IsAt(array, i + 4, Colon))
                    {
                        if (IsAt(array, i + 7, Space))
                        {
                            timeLength = i - index + 7;
                            isLocalTime = true;
                        }
                    }
                }
            }

            return true;
        }

        private bool IsAt(byte[] array, int i, byte b)
        {
            return i < array.Length && array[i] == b;
        }

        private int MoveToNextDelim(byte[] array, int i)
        {
            for (; i < array.Length; i++)
            {
                var b = array[i];
                if (b == Space || b == CR || b == LF)
                    return i;
            }

            return -1;
        }

        private string GetRest()
        {
            int i = lastPos;
            string result;

            while (i < bytes.Length)
            {
                if (IsTerminator(i))
                {
                    var j = BehindTerminator(i + 1);

                    if (j == bytes.Length || CheckTime(bytes, j))
                    {
                        result = GetString(i - lastPos);
                        lastPos = j;
                        return result;
                    }
                }

                i++;
            }

            //--- no LF found; just return whatever is there
            result = GetString(i - lastPos);
            lastPos = i;
            return result;
        }

        private bool IsTerminator(int i)
        {
            return bytes[i] == CR || bytes[i] == LF;
        }

        private int BehindTerminator(int i)
        {
            int j = i;

            while (j < bytes.Length)
            {
                if (!IsTerminator(j))
                    break;

                ++j;
            }

            return j;
        }
    }
}
