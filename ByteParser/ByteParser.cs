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

    public class ByteParser : IByteParser
    {
        private static readonly SmartLogger log = new SmartLogger();

        protected static readonly byte CR = 0x0D; // '\r'
        protected static readonly byte LF = 0x0A; // '\n'
        protected static readonly byte Space = 0x20; // ' '
        protected static readonly byte Plus = 0x2B; // '+'
        protected static readonly byte Dash = 0x2D; // '-'
        protected static readonly byte Colon = 0x3A; // ':'
        protected static readonly byte Comma = 0x2C; // ','
        protected static readonly byte Dot = 0x2E; // '.'
        protected static readonly string FalconKey3 = "\"_message";
        protected static readonly string DockerKey4 = "Attaching";

        public virtual bool IsFormatOK(byte[] bytes)
        {
            return false;
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
        public int CurrentPosition => lastPos;

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
            int i = GetIndexOfNext(CR, LF);
            string result = GetString(i - lastPos);
            lastPos = GetIndexOfNext(CR, LF, true);
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
        protected int GetIndexOfNext(byte b, bool invert = false)
        {
            return GetIndexOfNext(b, b, invert);
        }

        /// <summary>
        /// 
        /// </summary>
        protected int GetIndexOfNext(byte b1, byte b2, bool invert = false)
        {
            int i = lastPos;

            for (; i < bytes.Length; i++)
            {
                bool found = bytes[i] == b1 || bytes[i] == b2;

                if (invert)
                    found = !found;

                if (found)
                    break;
            }

            return i;
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
        /// Check if an expected string is at a certain position of a bytes array.
        /// </summary>
        protected bool CheckForString(string expected, byte[] bytes, int position)
        {
            if (bytes == null || position + expected.Length > bytes.Length)
                return false;

            string extracted = Utils.BytesToString(bytes, position, expected.Length);
            return extracted.equals(expected);
        }
    }
}
