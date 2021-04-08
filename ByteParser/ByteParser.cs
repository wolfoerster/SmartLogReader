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

        /// <summary>
        /// Gets or sets the bytes which contain the log entries.
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
        /// Gets the current position, i.e. the position of the next log entry.
        /// </summary>
        public int CurrentPosition => lastPos;

        /// <summary>
        /// Reads the next log entry and converts it to a Record.
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
                log.Exception(ex);
                return null;
            }
        }

        /// <summary>
        /// Override this method in derived classes to read the next log entry and fill the record.
        /// </summary>
        protected virtual void FillRecord(Record record)
        {
            record.Message = GetNextLine();
        }

        /// <summary>
        /// Reads from the current position to the end of the line and moves current position behind it.
        /// </summary>
        protected string GetNextLine()
        {
            int i = GetIndexOfNext(CR, LF);
            string result = GetString(i - lastPos);

            // move on to the next byte which is neither CR nor LF
            lastPos = GetIndexOfNext(CR, LF, true);
            return result;
        }

        /// <summary>
        /// Reads from the current position a number of bytes and moves current position behind it.
        /// </summary>
        protected string GetString(int numBytes)
        {
            string result = Utils.BytesToString(bytes, lastPos, numBytes);
            lastPos += numBytes;
            return result;
        }

        /// <summary>
        /// Looks for the first occurrence of a certain byte starting at the current position.
        /// If 'invert' is true, looks for the first occurrence of a byte which is NOT the specified one.
        /// </summary>
        protected int GetIndexOfNext(byte b1, bool invert = false)
        {
            return GetIndexOfNext(b1, b1, b1, invert);
        }

        /// <summary>
        /// Same as above but now one of two bytes are looked for.
        /// </summary>
        protected int GetIndexOfNext(byte b1, byte b2, bool invert = false)
        {
            return GetIndexOfNext(b1, b2, b2, invert);
        }

        /// <summary>
        /// Same as above but now one of three bytes are looked for.
        /// </summary>
        protected int GetIndexOfNext(byte b1, byte b2, byte b3, bool invert = false)
        {
            int i = lastPos;

            for (; i < bytes.Length; i++)
            {
                bool found = bytes[i] == b1 || bytes[i] == b2 || bytes[i] == b3;

                if (invert)
                    found = !found;

                if (found)
                    break;
            }

            return i;
        }

        /// <summary>
        /// Reads the next bytes until a Space character or a line terminator is found.
        /// </summary>
        protected string GetNext(int numBytes = -1)
        {
            if (lastPos == bytes.Length)
                return null;

            int i = numBytes < 0 ? GetIndexOfNext(Space, CR, LF) : lastPos + numBytes;
            string result = GetString(i - lastPos);

            if (numBytes > 0)
                result = result.Trim();

            if (bytes[i] == CR || bytes[i] == LF)
                lastPos = GetIndexOfNext(CR, LF, true);
            else
                lastPos = GetIndexOfNext(Space, true);

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
