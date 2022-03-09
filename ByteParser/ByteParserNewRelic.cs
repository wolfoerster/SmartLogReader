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

using System;
using Newtonsoft.Json.Linq;

namespace SmartLogReader
{
    /// <summary>
    /// A byte parser for JSON based logger (here: JsonLogger exported from NewRelic)
    /// </summary>
    public class ByteParserNewRelic : ByteParser
    {
        public ByteParserNewRelic(byte[] bytes)
        {
            if (CheckForString("extracted from NewRelic", bytes, 0))
            {
                Bytes = bytes;
                _ = GetNextLine();
            }
        }

        protected override void FillRecord(Record record)
        {
            var json = GetNextLine();
            var jobj = JObject.Parse(json);

            if (long.TryParse(GetValue(jobj, "timestamp"), out long timestamp))
            {
                var offSet = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                var dateTime = offSet.LocalDateTime;
                record.TimeString = dateTime.ToUniversalTime().ToStringN();
            }

            record.LevelString = GetValue(jobj, "level");
            record.Logger = GetValue(jobj, "SourceContext");
            record.Method = GetValue(jobj, "MethodName");
            record.Message = GetValue(jobj, "message");
            record.Json = jobj;
        }

        private string GetValue(JObject jobj, string name)
        {
            if (jobj.TryGetValue(name, out JToken value))
                return value.ToString();

            return "";
        }
    }
}
