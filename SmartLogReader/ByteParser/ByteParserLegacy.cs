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

using SmartLogging;
using SmartLogReader.Common;

namespace SmartLogReader
{
    public class ByteParserLegacy : ByteParser
    {
        private static readonly string LegacyKey1 = "TrimbleNo";
        private static readonly string LegacyKey2 = "novaSuite";

        public ByteParserLegacy()
        {
        }

        public ByteParserLegacy(byte[] bytes)
        {
            Bytes = bytes;
        }

        public override bool CheckFormat(byte[] bytes, out string newFileName)
        {
            newFileName = null;
            return CheckForString(LegacyKey1, bytes, 0) || CheckForString(LegacyKey2, bytes, 0);
        }

        protected override LogEntry ReadEntry()
        {
            var entry = new LogEntry();
            GetLegacyRecord(entry);
            return entry;
        }

        private void GetLegacyRecord(LogEntry entry)
        {
            string token = GetNext();
#if false
            if (!token.StartsWith(LegacyKey1) && !token.StartsWith(LegacyKey2))
            {
                record.Message = GetNextLine();
                return;
            }
#endif
            token = GetNext();
            switch (token[0])
            {
                case 'C': entry.Level = "Fatal"; break;
                case 'E': entry.Level = "Error"; break;
                case 'W': entry.Level = "Warning"; break;
                case 'I': entry.Level = "Information"; break;
                case 'V': entry.Level = "Debug"; break;
            }

            token = GetNext();
            token = GetNext();
            entry.Context = GetNext().TrimEnd(new char[] { ':' });

            entry.Message = GetNextLine();
            entry.Method = " ";
            int index = entry.Message.IndexOf(':');
            if (index > 0)
            {
                token = entry.Message.Substring(0, index);
                if (token.IndexOf(' ') < 0)
                {
                    entry.Method = token;
                    entry.Message = entry.Message.Substring(index + 1).TrimStart();
                }
            }

            while (true)
            {
                // is there a next line?
                if (bytes.Length - lastPos < LegacyKey1.Length)
                    return;

                // is it a new log entry?
                string test = Utils.BytesToString(bytes, lastPos, LegacyKey1.Length);
                if (test == LegacyKey1 || test == LegacyKey2)
                    return;

                // read the line
                var line = GetNextLine();

                // is it the log entry time record?
                var str = "    DateTime=";
                if (line.StartsWith(str))
                {
                    entry.Time = line.Substring(str.Length);
                    return;
                }
                else
                {
                    entry.Message += "\r\n" + line;
                }
            }
        }
    }
}
