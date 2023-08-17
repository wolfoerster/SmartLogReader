﻿//******************************************************************************************
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

namespace SmartLogReader
{
    public class ByteParserDocker : ByteParserJsonLogger
    {
        public ByteParserDocker(byte[] bytes)
        {
            if (CheckForString("Attaching to", bytes, 0)
                || IsNewDocker(bytes))
            {
                Bytes = bytes;
                _ = GetNextLine();
            }
        }

        protected override void FillRecord(Record record)
        {
            lastPos = GetIndexOfNext((byte)'{');
            base.FillRecord(record);
        }

        private static bool IsNewDocker(byte[] bytes)
        {
            var numBytes = Math.Min(bytes.Length, 1000);
            var str = Utils.BytesToString(bytes, 0, numBytes);
            var index = str.IndexOf("  | {\"Timestamp\":");
            return index > 0;
        }
    }
}
