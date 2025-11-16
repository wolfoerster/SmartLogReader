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
using System.IO;
using System.Windows;
using SmartLogging;
using SmartLogReader.Common;

namespace SmartLogReader
{
    public static class ParserFactory
    {
        private static readonly SmartLogger Log = new SmartLogger();

        public static string CreateParser(string path, out IByteParser parser)
        {
            parser = null;
            var bytes = new byte[0];

            try
            {
                var fileInfo = new FileInfo(path);
                if (fileInfo.Length > 0)
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = new BinaryReader(fs))
                        {
                            bytes = reader.ReadBytes(Math.Min((int)fileInfo.Length, 16 * 1024));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                return path;
            }

            if (bytes.Length == 0)
                return path;

            if (Check(typeof(ByteParserSmartLogger)))
                parser = new ByteParserSmartLogger(bytes);

            else if (Check(typeof(ByteParserJsonLogger)))
                parser = new ByteParserJsonLogger(bytes);

            else if (Check(typeof(ByteParserNewRelic)))
                parser = new ByteParserNewRelic(bytes);

            else if (Check(typeof(ByteParserSumoLogic)))
                parser = new ByteParserSumoLogic(bytes);

            else if (Check(typeof(ByteParserDocker)))
                parser = new ByteParserDocker(bytes);

            else if (Check(typeof(ByteParserPlainJson)))
                parser = new ByteParserPlainJson(bytes);

            else if (Check(typeof(ByteParserPlainText)))
                parser = new ByteParserPlainText(bytes);

            else if (Check(typeof(ByteParserLegacy)))
                parser = new ByteParserLegacy(bytes);

            else 
                parser = new ByteParser { Bytes = bytes };

            return path;

            bool Check(Type type)
            {
                var obj = Activator.CreateInstance(type);

                if (obj is IByteParser byteParser)
                    return byteParser.IsValidFormat(bytes);

                return false;
            }
        }

        public static IByteParser CreateParser(byte[] bytes)
        {
            IByteParser parser;

            if (IsOK(parser = new ByteParserSmartLogger(bytes)))
                return parser;

            if (IsOK(parser = new ByteParserJsonLogger(bytes)))
                return parser;

            if (IsOK(parser = new ByteParserNewRelic(bytes)))
                return parser;

            if (IsOK(parser = new ByteParserSumoLogic(bytes)))
                return parser;

            if (IsOK(parser = new ByteParserDocker(bytes)))
                return parser;

            if (IsOK(parser = new ByteParserPlainJson(bytes)))
                return parser;

            if (IsOK(parser = new ByteParserPlainText(bytes)))
                return parser;

            if (IsOK(parser = new ByteParserLegacy(bytes)))
                return parser;

            return new ByteParser { Bytes = bytes };
        }

        private static bool IsOK(IByteParser parser)
        {
            return parser.Bytes != null;
        }
    }
}
