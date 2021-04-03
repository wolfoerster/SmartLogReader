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
    using SmartLogging;

    public class ByteParser
    {
        private static readonly SmartLogger log = new SmartLogger();

        protected static readonly byte CR = 0x0D;//= '\r'
        protected static readonly byte LF = 0x0A;//= '\n'
        protected static readonly byte Space = 0x20;//= ' '
        protected static readonly byte Plus = 0x2B;//= '+'
        protected static readonly byte Dash = 0x2D;//= '-'
        protected static readonly byte Colon = 0x3A;//= ':'
        protected static readonly byte Comma = 0x2C;//= ','
        protected static readonly byte FullStop = 0x2E;//= '.'
        protected static readonly string LegacyKey1 = "novaSuite";
        protected static readonly string LegacyKey2 = "TrimbleNo";

        /// <summary>
        /// 
        /// </summary>
        public ByteParser(byte[] bytes)
        {
            Bytes = bytes;
        }

        /// <summary>
        /// 
        /// </summary>
        public static ByteParser CreateParser(byte[] bytes)
        {
            if (bytes.Length > LegacyKey1.Length)
            {
                string result = Utils.BytesToString(bytes, 0, LegacyKey1.Length);
                if (result == LegacyKey1 || result == LegacyKey2)
                {
                    return new ByteParserLegacy(bytes);
                }
                else if (result == "\"_message")
                {
                    return new ByteParserJson3(bytes);
                }
            }

            var parser = new ByteParser(bytes);
            if (parser.CheckTime(0))
            {
                if (parser.isJson1)
                    return new ByteParserJson1(bytes);

                if (parser.isJson2)
                    return new ByteParserJson2(bytes);

                if (parser.isLocalTime)
                    return new ByteParserSerilog(bytes);

                return new ByteParserSmartlog(bytes);
            }

            //--- unknown format
            return parser;
        }

        /// <summary>
        /// 
        /// </summary>
        public byte[] Bytes
        {
            get => bytes;
            set
            {
                bytes = value;
                lastPos = 0;
            }
        }
        protected byte[] bytes;
        protected int lastPos;

        /// <summary>
        /// 
        /// </summary>
        public double CurrentPosition => lastPos;

        /// <summary>
        /// 
        /// </summary>
        public Record GetNextRecord()
        {
            int nRemain = bytes.Length - lastPos;
            if (nRemain < 1)
                return null;

            Record record = new Record();
            try
            {
                FillRecord(record);
                return record;
            }
            catch (Exception ex)
            {
                log.Smart(ex.Message, LogLevel.Error, ex);
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void FillRecord(Record record)
        {
            record.Message = GetNextLine();
        }

        /// <summary>
        /// 
        /// </summary>
        protected string GetNextLine()
        {
            int i = GetIndexOfNext(LF, CR);
            string result = GetString(i - lastPos);
            lastPos = GetIndexOfNext(LF, CR, true);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        protected string GetString(int numBytes)
        {
            string result = Utils.BytesToString(bytes, lastPos, numBytes);
            lastPos += numBytes;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        protected int GetIndexOfNext(byte b1, byte b2, bool invSearch = false)
        {
            int i = lastPos;
            for (; i < bytes.Length; i++)
            {
                bool found = bytes[i] == b1 || bytes[i] == b2;

                if (invSearch)
                    found = !found;

                if (found)
                    break;
            }
            return i;
        }

        protected string GetTime()
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
        int minTimeLength = 19;
        int timeLength;
        protected bool isLocalTime;
        protected bool isJson1, isJson2;

        /// <summary>
        /// 
        /// </summary>
        protected bool CheckTime(int index)
        {
            if (bytes.Length - index > 12)
            {
                string str = Utils.BytesToString(bytes, index, 12);
                if (str.startsWith("{\"time\""))
                {
                    isJson1 = true;
                    return true;
                }

                if (str.startsWith("{\"timestamp\""))
                {
                    isJson2 = true;
                    return true;
                }
            }

            //--- string should at least look like "2017-07-23 16:48:18"

            if (bytes.Length - index < minTimeLength)
                return false;

            int i = index + 4;
            if (bytes[i] != Dash)
                return false;

            i += 3;
            if (bytes[i] != Dash)
                return false;

            i += 3;
            //if (bytes[i] != Space)
            //	return false;

            i += 3;
            if (bytes[i] != Colon)
                return false;

            i += 3;
            if (bytes[i] != Colon)
                return false;

            timeLength = minTimeLength;
            isLocalTime = false;

            try
            {
                //--- "2017-07-23 16:48:18.123" ?
                i += 3;
                if (bytes[i] == FullStop || bytes[i] == Comma)
                {
                    //--- move on to next space
                    while (bytes[++i] != Space) ;
                    timeLength = i - index;

                    //--- "2017-07-23 16:48:18.123 +01:00" ?
                    if (bytes[i + 1] == Plus || bytes[i + 1] == Dash)
                    {
                        if (bytes[i + 4] == Colon)
                        {
                            if (bytes[i + 7] == Space)
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

        /// <summary>
        /// 
        /// </summary>
        protected string GetNext(int numBytes = -1)
        {
            if (lastPos == bytes.Length)
                return null;

            int i = numBytes < 0 ? GetIndexOfNext(Space, CR) : lastPos + numBytes;
            string result = GetString(i - lastPos);

            if (numBytes > 0)
                result = result.Trim();

            if (bytes[i] == CR)
                lastPos = GetIndexOfNext(LF, CR, true);
            else
                lastPos = GetIndexOfNext(Space, Space, true);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        protected string GetText()
        {
            //--- look for CR followed by LF
            int i = lastPos;

            while (i < bytes.Length)
            {
                int prev_i = i - 1;

                if (noCR && bytes[i] == LF) // no CR???
                {
                    string result = GetString(prev_i - lastPos);
                    lastPos = i + 1;
                    return bytes[prev_i] == CR ? result.Substring(0, result.Length - 1) : result;
                }

                //--- are we in between CR and LF?
                if (i > 0 && bytes[prev_i] == CR && bytes[i] == LF)
                {
                    int next_i = i + 1;

                    //--- check for end of bytes or next time string
                    if (next_i == bytes.Length || CheckTime(next_i))
                    {
                        string result = GetString(prev_i - lastPos);
                        lastPos = next_i;
                        return result;
                    }
                }

                i++;
            }

            //--- no CR-LF found; just return whatever is there
            return GetString(i - lastPos);
        }

        protected bool noCR;
    }
}
