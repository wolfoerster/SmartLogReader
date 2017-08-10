//******************************************************************************************
// Copyright © 2017 Wolfgang Foerster (wolfoerster@gmx.de)
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
    public enum LogFormats
	{
		Unknown,
        SmartLogger,
        LegacyLogger
	}

	public class ByteParser
	{
		private static readonly byte CR = 0x0D;//= '\r'
		private static readonly byte LF = 0x0A;//= '\n'
		private static readonly byte Space = 0x20;//= ' '
		private static readonly byte Dash = 0x2D;//= '-'
		private static readonly byte Colon = 0x3A;//= ':'
        private static readonly byte Comma = 0x2C;//= ','
		private static readonly byte FullStop = 0x2E;//= '.'
        private static readonly string LegacyKey = "Aurora";

		/// <summary>
		/// 
		/// </summary>
		public ByteParser(byte[] bytes)
		{
			Bytes = bytes;
			CheckFormat();
		}

		/// <summary>
		/// 
		/// </summary>
		void CheckFormat()
		{
            if (bytes.Length > LegacyKey.Length)
            {
                string result = Utils.BytesToString(bytes, 0, LegacyKey.Length);
                if (result == LegacyKey)
                {
                    Format = LogFormats.LegacyLogger;
                    return;
                }
            }

            //--- SmartLogger format
            if (CheckTime(0))
            {
                Format = LogFormats.SmartLogger;
                return;
            }

			//--- unknown format
			Format = LogFormats.Unknown;
		}

		/// <summary>
		/// 
		/// </summary>
		public LogFormats Format { get; protected set; }

		/// <summary>
		/// 
		/// </summary>
		public byte[] Bytes
		{
			get { return bytes; }
			set 
			{ 
				bytes = value;
				lastPos = 0;
			}
		}
		byte[] bytes;
		int lastPos;

		/// <summary>
		/// 
		/// </summary>
		public double CurrentPosition
		{
			get { return lastPos; }
		}

		/// <summary>
		/// 
		/// </summary>
		public Record GetNextRecord()
		{
			int nRemain = bytes.Length - lastPos;
			if (nRemain < 1)
				return null;

			Record record = new Record();

            switch (Format)
            {
                case LogFormats.SmartLogger:
                    record.TimeString = GetTime();
                    record.LevelString = GetNext();
                    record.Logger = GetNext();
                    record.ThreadIds = GetNext();
                    record.Method = GetNext();
                    record.Message = GetText();
                    break;

                case LogFormats.LegacyLogger:
                    GetLegacyRecord(record);
                    break;

                default:
                    record.Message = GetNextLine();
                    break;
            }

			return record;
		}

        private void GetLegacyRecord(Record record)
        {
            string token = GetNext();
            if (!token.StartsWith(LegacyKey))
            {
                record.Message = GetNextLine();
                return;
            }

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

            token = GetNextLine();
            int i = token.IndexOf('=');
            if (i != 12)
                return;

            record.TimeString = token.Substring(i + 1);
        }

        /// <summary>
        /// 
        /// </summary>
        string GetNextLine()
		{
			int i = GetIndexOfNext(LF, CR);
			string result = GetString(i - lastPos);
			lastPos = GetIndexOfNext(LF, CR, true);
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		string GetString(int numBytes)
		{
            string result = Utils.BytesToString(bytes, lastPos, numBytes);
            lastPos += numBytes;
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		int GetIndexOfNext(byte b1, byte b2, bool invSearch = false)
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

        string GetTime()
        {
            string s1 = GetNext(minTimeLength);
            if (bytes[lastPos] == Comma || bytes[lastPos] == FullStop)
            {
                string s2 = GetNext();//--- because we don't know the number of digits for the seconds
                return s1 + s2;
            }
            return s1;
        }
        int minTimeLength = 19;

		/// <summary>
		/// 
		/// </summary>
		bool CheckTime(int index)
		{
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

			//i += 3;
            //if (bytes[i] != FullStop && bytes[i] != Comma)
			//	return false;

			//i += 4;
			//if (bytes[i] != Space)
			//	return false;

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		string GetNext(int numBytes = -1)
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
		string GetText()
		{
			//--- look for CR followed by LF
			int i = lastPos;

			while (i < bytes.Length)
			{
				int prev_i = i - 1;

				//--- are we in between CR and LF?
				if (bytes[prev_i] == CR && bytes[i] == LF)
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
	}
}
