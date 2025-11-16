//******************************************************************************************
// Copyright © 2017 - 2025 Wolfgang Foerster (wolfoerster@gmx.de)
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

using SmartLogReader.Common;

namespace SmartLogReader
{
    using System;
    using System.Globalization;
    using SmartLogging;

    /// <summary>
    /// A byte parser for a plain text based logger with messages in JSON format.
    /// 
    /// A log entry has to look like this:
    /// 2025-10-28T11:16:04.2881241Z|1|Info|Common.Queries.SimpleQuery|CreateQuery|{"name":"foo","depth"=4}
    /// 
    /// In general:
    /// UTC DateTime|ThreadId|LogLevel|ClassName|MethodName|Message
    /// 
    /// The message might span several lines. Everything from the begin of the message up to
    /// the next DateTime field is considered as message (including line feeds and spaces).
    /// </summary>
    public class ByteParserPlainJson : ByteParser
    {
        public ByteParserPlainJson(byte[] bytes)
        {
            var entry = new LogEntry();

            if (CheckBytes(bytes, 0, entry, out _))
            {
                Bytes = bytes;
            }
        }

        protected override LogEntry ReadEntry()
        {
            var entry = new LogEntry();

            if (CheckBytes(bytes, lastPos, entry, out int nextPos))
                lastPos = nextPos;
            else
                lastPos = MoveToNextDateTime(bytes, lastPos + 1);

            return entry;
        }

        private bool CheckTime(byte[] bytes, int index, out string timeString)
        {
            //var test = Utils.BytesToString(bytes, index, 200);
            timeString = "";
            var i0 = index;
            var i1 = MoveToNextPipe(bytes, i0);
            if (i1 - i0 < 0)
                return false;

            var text = Utils.BytesToString(bytes, i0, i1 - i0);
            if (!text.IsDateTimeOffset(out var _))
                return false;

            timeString = text;
            return true;
        }

        private bool CheckBytes(byte[] bytes, int index, LogEntry entry, out int nextPos)
        {
            nextPos = lastPos;
            entry.Message = "";

            var i0 = index;
            if (!CheckTime(bytes, index, out string timeString))
            {
                return false;
            }

            var i1 = i0 + timeString.Length;
            entry.Time = timeString;

            i0 = i1 + 1;
            i1 = MoveToNextPipe(bytes, i0); // thread id
            if (i1 - i0 < 0)
                return false;

            var text = Utils.BytesToString(bytes, i0, i1 - i0);
            entry.Annex = text;

            i0 = i1 + 1;
            i1 = MoveToNextPipe(bytes, i0); // log level
            if (i1 - i0 < 0)
                return false;

            text = Utils.BytesToString(bytes, i0, i1 - i0);
            if (Record.TryParseLevel(text) == LogLevel.None)
                return false;

            entry.Level = text;

            i0 = i1 + 1;
            i1 = MoveToNextPipe(bytes, i0); // class name
            if (i1 - i0 < 0)
                return false;

            entry.Context = Utils.BytesToString(bytes, i0, i1 - i0);

            i0 = i1 + 1;
            i1 = MoveToNextPipe(bytes, i0); // method name
            if (i1 - i0 < 0)
                return false;

            entry.Method = Utils.BytesToString(bytes, i0, i1 - i0);

            i0 = i1 + 1;
            i1 = MoveToNextDateTime(bytes, i0); // message
            if (i1 - i0 < 0)
                return false;

            entry.Message = Utils.BytesToString(bytes, i0, i1 - i0).TrimEnd('\r', '\n');

            nextPos = i1;
            return true;
        }

        private int MoveToNextDateTime(byte[] array, int i)
        {
            for (; i < array.Length; i++)
            {
                if (CheckTime(array, i, out _))
                    return i;
            }

            return i;
        }

        private int MoveToNextPipe(byte[] array, int i)
        {
            for (; i < array.Length; i++)
            {
                var b = array[i];
                if (b == Pipe || b == CR || b == LF)
                    return i;
            }

            return -1;
        }
    }
}
