//******************************************************************************************
// Copyright © 2022 - 2025 Wolfgang Foerster (wolfoerster@gmx.de)
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
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartLogging;
using SmartLogReader.Common;

namespace SmartLogReader
{
    /// <summary>
    /// A byte parser for JSON based logger (here: JsonLogger exported from NewRelic)
    /// </summary>
    public class ByteParserNewRelic : ByteParser
    {
        public ByteParserNewRelic()
        {
        }

        public ByteParserNewRelic(byte[] bytes)
        {
            Bytes = bytes;
        }

        public override bool CheckFormat(byte[] bytes, out string newFileName)
        {
            newFileName = null;

            var text = Utils.BytesToString(bytes, 0, bytes.Length);
            if (!text.startsWith("[{\"Timestamp\":"))
                return false;

            var jtok = JToken.Parse(text);
            if (jtok.Type != JTokenType.Array)
                return false;

            var lines = new List<string>();

            foreach (var item in jtok)
            {
                if (item is JObject jobj)
                {
                    lines.Add(JsonConvert.SerializeObject(jobj, Formatting.None));
                }
            }

            newFileName = Path.GetTempFileName();

            using (var stream = File.OpenWrite(newFileName))
            using (var writer = new StreamWriter(stream))
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    writer.WriteLine(lines[i]);
                }
            }

            return true;
        }

        protected override LogEntry ReadEntry()
        {
            var entry = new LogEntryJson();
            var json = GetNextLine();
            var jobj = JObject.Parse(json);

            string GetValue(string name)
            {
                if (jobj.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out JToken value))
                    return value.ToString();

                return string.Empty;
            }

            if (long.TryParse(GetValue("timestamp"), out long timestamp))
            {
                var offSet = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                var dateTime = offSet.LocalDateTime;
                entry.Time = dateTime.ToUniversalTime().ToStringN();
            }

            entry.Level = GetValue("level");
            entry.Context = GetValue("SourceContext");
            entry.Method = GetValue("MethodName");
            entry.Message = GetValue("message");
            entry.Annex = GetValue("ConnectionId");
            entry.Json = jobj;

            return entry;
        }
    }
}
