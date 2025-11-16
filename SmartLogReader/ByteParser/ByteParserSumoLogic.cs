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
using System.IO;
using System.Linq;
using SmartLogging;
using SmartLogReader.Common;

namespace SmartLogReader
{
    /// <summary>
    /// A byte parser for JSON based logger (here: JsonLogger exported from Sumologic)
    /// </summary>
    public class ByteParserSumoLogic : ByteParserJsonLogger
    {
        public ByteParserSumoLogic(byte[] bytes)
        {
            Bytes = bytes;
            //_ = GetNextLine();
        }

        public override bool CheckFormat(byte[] bytes, out string newFileName)
        {
            newFileName = null;

            var text = Utils.BytesToString(bytes, 0, bytes.Length);
            if (!text.startsWith("\"_messagetimems\""))
                return false;

            newFileName = Path.GetTempFileName();

            var lines = text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var reversed = lines.Reverse().ToList();
            var i = reversed.Count - 1;
            var line = reversed[i];
            reversed.RemoveAt(i);
            //reversed.Insert(0, line);

            File.WriteAllLines(newFileName, reversed);
            return true;
        }

        protected override LogEntry ReadEntry()
        {
            var entry = new LogEntryJson();
            GetJsonRecord3(entry, GetNextLine());
            return entry;
        }

        private void GetJsonRecord3(LogEntryJson entry, string json)
        {
            var i = json.IndexOf("{\"\"Timestamp");
            json = json.Substring(i, json.Length - i - 1);
            json = json.Replace("\"\"", "\"");
            json = json.TrimEnd(new[] { '\"' });
            GetJsonRecord2(entry, json);
        }
    }
}
