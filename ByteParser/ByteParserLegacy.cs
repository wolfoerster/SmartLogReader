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
    public class ByteParserLegacy : ByteParser
    {
        private static readonly string LegacyKey1 = "TrimbleNo";
        private static readonly string LegacyKey2 = "novaSuite";

        public override bool IsFormatOK(byte[] bytes)
        {
            if (CheckForString(LegacyKey1, bytes, 0))
                return true;

            return CheckForString(LegacyKey2, bytes, 0);
        }

        protected override void FillRecord(Record record)
        {
            GetLegacyRecord(record);
        }

        private void GetLegacyRecord(Record record)
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
                case 'C': record.LevelString = "Fatal"; break;
                case 'E': record.LevelString = "Error"; break;
                case 'W': record.LevelString = "Warn"; break;
                case 'I': record.LevelString = "Info"; break;
                case 'V': record.LevelString = "Debug"; break;
            }

            token = GetNext();
            token = GetNext();
            record.Logger = GetNext().TrimEnd(new char[] { ':' });

            record.Message = GetNextLine();
            record.Method = " ";
            int index = record.Message.IndexOf(':');
            if (index > 0)
            {
                token = record.Message.Substring(0, index);
                if (token.IndexOf(' ') < 0)
                {
                    record.Method = token;
                    record.Message = record.Message.Substring(index + 1).TrimStart();
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
                    record.TimeString = line.Substring(str.Length);
                    return;
                }
                else
                {
                    record.Message += "\r\n" + line;
                }
            }
        }
    }
}
