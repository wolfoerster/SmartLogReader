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
    /// A byte parser for the SmartLogger found in my github repository.
    /// </summary>
    public class ByteParserSmartlog : ByteParser
    {
        public override bool IsFormatOK(byte[] bytes)
        {
            return CheckTime(bytes, 0);
        }

        protected override void FillRecord(Record record)
        {
            record.TimeString = GetTime();
            record.LevelString = GetNext();
            record.Logger = GetNext();
            record.ThreadIds = GetNext();
            record.Method = GetNext();
            record.Message = GetRest();
        }

        private string GetTime()
        {
            string s = GetNext(timeLength);
            if (isLocalTime)
            {
                if (DateTime.TryParse(s, out DateTime t))
                {
                    t = DateTime.SpecifyKind(t, DateTimeKind.Local);
                    t = t.ToUniversalTime();
                    s = t.ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
            }
            return s;
        }
        private int timeLength;
        private bool isLocalTime;

        private bool CheckTime(byte[] array, int index)
        {
            //--- string should at least look like "2017-07-23 16:48:18"

            int minTimeLength = 19;
            if (array.Length - index < minTimeLength)
                return false;

            int i = index + 4;
            if (array[i] != Dash)
                return false;

            i += 3;
            if (array[i] != Dash)
                return false;

            i += 3;
            //if (array[i] != Space) // wrong! can also be 'T'
            //	return false;

            i += 3;
            if (array[i] != Colon)
                return false;

            i += 3;
            if (array[i] != Colon)
                return false;

            timeLength = minTimeLength;
            isLocalTime = false;

            try
            {
                //--- are there fractions of seconds "2017-07-23 16:48:18.123" ?
                i += 3;
                if (array[i] == Dot || array[i] == Comma)
                {
                    //--- move on to next space
                    while (array[++i] != Space) ;
                    timeLength = i - index;

                    //--- is there a time zone shift "2017-07-23 16:48:18.123 +01:00" ?
                    if (array[i + 1] == Plus || array[i + 1] == Dash)
                    {
                        if (array[i + 4] == Colon)
                        {
                            if (array[i + 7] == Space)
                            {
                                timeLength = i - index + 7;
                                isLocalTime = true;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return true;
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
