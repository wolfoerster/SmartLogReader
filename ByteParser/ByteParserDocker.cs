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

namespace SmartLogReader
{
    public class ByteParserDocker : ByteParserJson2
    {
        private readonly byte bracket = (byte)'{';

        internal ByteParserDocker(byte[] bytes) : base(bytes)
        {
            noCR = true;
            var firstLine = GetText();
            noCR = false;

            //MoveToTimestamp();
            //CheckTime(lastPos);
        }

        protected override void FillRecord(Record record)
        {
            lastPos = GetIndexOfNext(bracket, bracket);
            base.FillRecord(record);
        }

        protected override bool CheckTime(int index)
        {
            var savedPos = lastPos;
            lastPos = index;
            var index2 = GetIndexOfNext(bracket, bracket);

            bool result = base.CheckTime(index2);

            lastPos = savedPos;
            return result;
        }
    }
}
