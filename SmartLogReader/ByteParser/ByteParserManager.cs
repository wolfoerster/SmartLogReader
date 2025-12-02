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
    public static class ByteParserManager
    {
        private static readonly SmartLogger Log = new SmartLogger();
        private static readonly List<IByteParser> byteParsers = new List<IByteParser>();
        private static string lastParsers;

        public static string LastParsers
        {
            get => lastParsers;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    ConfigurePlugins();
                    return;
                }

                lastParsers = value;
            }
        }

        public static void Initialize()
        {
            var executable = typeof(ByteParserManager).Assembly;
            GetParsers(executable);
            GetParsers(typeof(IByteParser).Assembly);

            var dir = Path.Combine(Path.GetDirectoryName(executable.Location), "Plugins");
            foreach (var file in Directory.GetFiles(dir, "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFile(file);
                    GetParsers(assembly);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }

        public static string CreateParser(string path, out IByteParser byteParser)
        {
            byteParser = null;
            var bytes = Utils.ReadBytes(path);

            if (bytes.Length == 0)
                return path;

            foreach (var parser in byteParsers)
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

        public static void ConfigurePlugins()
        {
            var dlg = new ConfigurePluginsDialog { ViewModel = new ConfigurePluginsVM(byteParsers, lastParsers) };
            dlg.ShowDialog(ViewModel.ConfigurePluginsCmd.Text);
        }

        private static void GetParsers(Assembly assembly)
        {
            Log.Information(new { assembly.Location });
            var types = assembly.GetExportedTypes();

            foreach (var type in types.Where(x => !x.IsInterface))
            {
                if (typeof(IByteParser).IsAssignableFrom(type))
                {
                    var existingParser = byteParsers.Find(x => x.GetType().FullName == type.FullName);
                    if (existingParser == null)
                    {
                        Log.Information(new { type.FullName });
                        var instance = Activator.CreateInstance(type);
                        if (instance is IByteParser parser)
                        {
                            byteParsers.Add(parser);
                        }
                    }
                }
            }
        }
    }
}
