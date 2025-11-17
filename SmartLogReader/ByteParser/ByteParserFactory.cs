//******************************************************************************************
// Copyright © 2021 - 2025 Wolfgang Foerster (wolfoerster@gmx.de)
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
using SmartLogging;
using SmartLogReader.Common;

namespace SmartLogReader
{
    public static class ByteParserFactory
    {
        private static readonly SmartLogger Log = new SmartLogger();

        public static string CreateParser(string path, out IByteParser byteParser)
        {
            byteParser = null;
            var bytes = Utils.ReadBytes(path);

            if (bytes.Length == 0)
                return path;

            string newPath = null;

            bool Check(Type type)
            {
                var obj = Activator.CreateInstance(type);

                if (obj is IByteParser parser) 
                    return parser.CheckFormat(bytes, out newPath);

                return false;
            }

            if (Check(typeof(ByteParserSmartLogger)))
            {
                byteParser = new ByteParserSmartLogger(bytes);
                return path;
            }

            if (Check(typeof(ByteParserJsonLogger)))
            {
                byteParser = new ByteParserJsonLogger(bytes);
                return path;
            }

            if (Check(typeof(ByteParserNewRelic)))
            {
                bytes = Utils.ReadBytes(newPath);
                byteParser = new ByteParserNewRelic(bytes);
                return newPath;
            }

            if (Check(typeof(ByteParserSumoLogic)))
            {
                bytes = Utils.ReadBytes(newPath);
                byteParser = new ByteParserSumoLogic(bytes);
                return newPath;
            }

            if (Check(typeof(ByteParserDocker)))
            {
                byteParser = new ByteParserDocker(bytes);
                return path;
            }

            if (Check(typeof(ByteParserPlainJson)))
            {
                byteParser = new ByteParserPlainJson(bytes);
                return path;
            }

            if (Check(typeof(ByteParserPlainText)))
            {
                byteParser = new ByteParserPlainText(bytes);
                return path;
            }

            if (Check(typeof(ByteParserLegacy)))
            {
                byteParser = new ByteParserLegacy(bytes);
                return path;
            }

            byteParser = new ByteParser(bytes);
            return path;
        }
    }
}
