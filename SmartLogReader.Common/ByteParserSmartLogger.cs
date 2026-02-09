//******************************************************************************************
// Copyright © 2021 - 2025 Wolfgang Foerster (wolfoerster@gmx.de)
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
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using SmartLogging;

namespace SmartLogReader.Common;

/// <summary>
/// A byte parser for SmartLogging.SmartLogger.
/// </summary>
public class ByteParserSmartLogger : ByteParser
{
    enum LoggerVersion { V1, V2, V3, V4 }

    private LoggerVersion loggerVersion;

    public override bool CheckFormat(byte[] bytes, out string newFileName)
    {
        newFileName = null;

        if (!IsEntryStart(bytes, 0))
            return false;

        var text = Utils.BytesToString(bytes, 0, 16 * 1024);

        if (text.Contains("ThreadIds"))
            loggerVersion = LoggerVersion.V1;

        else if (text.Contains("ThreadId"))
            loggerVersion = IsV2(text) ? LoggerVersion.V2 : LoggerVersion.V4;

        else if (text.Contains("Annex"))
            loggerVersion = LoggerVersion.V3;

        else // no SmartLogger at all
            return false;

        return true;
    }

    private static bool IsV2(string text)
    {
        var str = "ThreadId\":";
        var index = text.IndexOf(str);
        return char.IsDigit(text[index + str.Length]);
    }

    protected override LogEntry ReadEntry()
    {
        var line = GetNextLine();
        var sb = new StringBuilder(line);

        while (this.lastPos < this.bytes.Length
            && !this.IsEntryStart(this.bytes, this.lastPos))
        {
            line = GetNextLine();
            sb.Append(" ");
            sb.Append(line);
        }

        var entry = new LogEntry();
        GetJsonRecord(entry, sb.ToString());
        return entry;
    }

    private bool IsEntryStart(byte[] bytes, int position)
    {
        return CheckForString("{\"Time\"", bytes, position);
    }

    private void GetJsonRecord(LogEntry entry, string json)
    {
        if (loggerVersion == LoggerVersion.V1)
        {
            var logEntry = JsonConvert.DeserializeObject<LogEntryV1>(json);
            DateTime t = DateTime.Parse(logEntry.Time);
            entry.Time = t.ToUniversalTime().ToStringN();
            entry.Level = logEntry.Level;
            entry.Context = logEntry.Class;
            entry.Method = logEntry.Method;
            entry.Message = logEntry.Message ?? string.Empty;
            entry.ThreadId = logEntry.ThreadIds;
            return;
        }

        if (loggerVersion == LoggerVersion.V2)
        {
            var logEntry = JsonConvert.DeserializeObject<LogEntryV2>(json);
            entry.Time = Convert(logEntry.Time);
            entry.Level = logEntry.Level;
            entry.Context = logEntry.Context;
            entry.Method = logEntry.Method;
            entry.Message = logEntry.Message ?? string.Empty;
            entry.ThreadId = logEntry.ThreadId.ToString();
            return;
        }

        if (loggerVersion == LoggerVersion.V3)
        {
            var logEntry = JsonConvert.DeserializeObject<LogEntryV3>(json);
            entry.Time = Convert(logEntry.Time);
            entry.Level = logEntry.Level;
            entry.Context = logEntry.Context;
            entry.Method = logEntry.Method;
            entry.Message = logEntry.Message ?? string.Empty;
            entry.ThreadId = logEntry.Annex;
            return;
        }

        if (loggerVersion == LoggerVersion.V4)
        {
            var logEntry = JsonConvert.DeserializeObject<LogEntryV4>(json);
            entry.Time = Convert(logEntry.Time);
            entry.Level = logEntry.Level;
            entry.Context = logEntry.Context;
            entry.Method = logEntry.Method;
            entry.Message = logEntry.Message ?? string.Empty;
            entry.ThreadId = logEntry.ThreadId;
            return;
        }

        static string Convert(string value)
        {
            var dt = DateTime.ParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            return dt.ToUniversalTime().ToStringN();
        }
    }

    private class LogEntryV1
    {
        public string Time { get; set; }
        public string ThreadIds { get; set; }
        public string Level { get; set; }
        public string Class { get; set; }
        public string Method { get; set; }
        public string Message { get; set; }
    }

    private class LogEntryV2
    {
        public string Time { get; set; }
        public int ThreadId { get; set; }
        public string Level { get; set; }
        public string Context { get; set; }
        public string Method { get; set; }
        public string Message { get; set; }
    }

    private class LogEntryV3
    {
        public string Time { get; set; }
        public string Level { get; set; }
        public string Context { get; set; }
        public string Method { get; set; }
        public string Message { get; set; }
        public string Annex { get; set; }
    }

    private class LogEntryV4
    {
        public string Time { get; set; }
        public string ThreadId { get; set; }
        public string Level { get; set; }
        public string Context { get; set; }
        public string Method { get; set; }
        public string Message { get; set; }
    }
}
