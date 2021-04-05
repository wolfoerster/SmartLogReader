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

    public static class ParserFactory
    {
        public static IByteParser CreateParser(byte[] bytes)
        {
            IByteParser parser;

            if (IsOK(parser = new ByteParserJson1(), bytes))
                return parser;

            if (IsOK(parser = new ByteParserJson2(), bytes))
                return parser;

            if (IsOK(parser = new ByteParserSmartlog(), bytes))
                return parser;

            if (IsOK(parser = new ByteParserLegacy(), bytes))
                return parser;

            return new ByteParser { Bytes = bytes };
        }

        private static bool IsOK(IByteParser parser, byte[] bytes)
        {
            if (parser.IsFormatOK(bytes))
            {
                parser.Bytes = bytes;
                return true;
            }

            return false;
        }
    }
}
