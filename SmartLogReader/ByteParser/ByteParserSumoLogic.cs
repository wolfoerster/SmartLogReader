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
    /// <summary>
    /// A byte parser for JSON based logger (here: JsonLogger exported from Sumologic)
    /// </summary>
    public class ByteParserSumoLogic : ByteParserJsonLogger
    {
        public ByteParserSumoLogic(byte[] bytes)
        {
            if (CheckForString("\"_messagetimems\"", bytes, 0))
            {
                Bytes = bytes;
                _ = GetNextLine();
            }
        }

        protected override void FillRecord(Record record)
        {
            GetJsonRecord3(record, GetNextLine());
        }

        private void GetJsonRecord3(Record record, string json)
        {
            var i = json.IndexOf("{\"\"Timestamp");
            json = json.Substring(i, json.Length - i - 1);
            json = json.Replace("\"\"", "\"");
            json = json.TrimEnd(new[] { '\"' });
            GetJsonRecord2(record, json);
        }
    }
}
