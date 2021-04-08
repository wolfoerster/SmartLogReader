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
    using Newtonsoft.Json;

    /// <summary>
    /// A byte parser for JSON based logger (e.g. the SmartLogger found in WFTools).
    /// </summary>
    public class ByteParserSmartLogger : ByteParser
    {
        public ByteParserSmartLogger()
        {
        }

        public ByteParserSmartLogger(byte[] bytes)
        {
            if (CheckForString("{\"Time\"", bytes, 0))
            {
                Bytes = bytes;
            }
        }

        protected override void FillRecord(Record record)
        {
            GetJsonRecord1(record, GetNextLine());
        }

        private void GetJsonRecord1(Record record, string json)
        {
            var logEntry = JsonConvert.DeserializeObject<LogEntry>(json);

            DateTime t = DateTime.Parse(logEntry.Time);
            t = t.ToUniversalTime();
            record.TimeString = t.ToString("yyyy-MM-dd HH:mm:ss.fff");
            record.ThreadIds = logEntry.ThreadIds;
            record.LevelString = logEntry.Level;
            record.Logger = logEntry.Class;
            record.Method = logEntry.Method;
            record.Message = logEntry.Message ?? string.Empty;
        }

        private class LogEntry
        {
            public string Time { get; set; }
            public string ThreadIds { get; set; }
            public string Level { get; set; }
            public string Class { get; set; }
            public string Method { get; set; }
            public string Message { get; set; }
        }
    }
}
