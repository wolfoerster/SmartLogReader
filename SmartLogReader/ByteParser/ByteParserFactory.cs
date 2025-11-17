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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SmartLogging;
using SmartLogReader.Common;

namespace SmartLogReader
{
    internal static class ByteParserFactory
    {
        private static readonly SmartLogger Log = new SmartLogger();
        private static readonly List<IByteParser> byteParsers = new List<IByteParser>();

        public static void Initialize()
        {
            var plugins = LoadPlugins();

            void Add(Type parsertType)
            {
                var instance = Activator.CreateInstance(parsertType);
                if (instance is IByteParser parser)
                {
                    byteParsers.Add(parser);
                }
            }

            void AddIf(string name)
            {
                var plugin = plugins.FirstOrDefault(x => x.Name == name);
                if (plugin != null)
                {
                    Add(plugin);
                }
            }

            Add(typeof(ByteParserSmartLogger));
            AddIf("ByteParserJsonLogger");
            AddIf("ByteParserNewRelic");
            AddIf("ByteParserSumoLogic");
            AddIf("ByteParserDocker");
            Add(typeof(ByteParserPlainJson));
            Add(typeof(ByteParserPlainText));
            AddIf("ByteParserLegacy");
        }

        private static List<Type> LoadPlugins()
        {
            var dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Plugins");
            var files = Directory.GetFiles(dir, "*.dll");
            var list = new List<Type>();

            foreach (var file in files)
            {
                try
                {
                    var assembly = Assembly.LoadFile(file);
                    var types = assembly.GetExportedTypes();

                    foreach (var type in types)
                    {
                        if (typeof(IByteParser).IsAssignableFrom(type))
                        {
                            Log.Information(new { type.FullName });
                            list.Add(type);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }

            return list;
        }

        public static string CreateParser(string path, out IByteParser byteParser)
        {
            byteParser = null;
            var bytes = Utils.ReadBytes(path);

            if (bytes.Length == 0)
                return path;

            foreach(var parser in byteParsers)
            {
                var ok = parser.CheckFormat(bytes, out var newPath);
                if (ok)
                {
                    byteParser = parser;
                    return newPath ?? path;
                }
            }

            byteParser = new ByteParser() { Bytes = bytes };
            return path;
        }
    }
}
