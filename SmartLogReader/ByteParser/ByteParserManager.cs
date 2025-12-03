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
using Newtonsoft.Json;
using SmartLogging;
using SmartLogReader.Common;

namespace SmartLogReader
{
    public static class ByteParserManager
    {
        private static readonly SmartLogger Log = new SmartLogger();
        private static readonly Dictionary<string, IByteParser> existingParsers = new Dictionary<string, IByteParser>();
        private static List<(string Name, bool IsSelected)> configuredParsers;

        public static void Initialize(string lastParsers)
        {
            configuredParsers = JsonConvert.DeserializeObject<List<(string, bool)>>(lastParsers);

            if (configuredParsers == null)
            {
                configuredParsers = new List<(string, bool)>
                {
                    ("SmartLogReader.Common.ByteParserSmartLogger", true),
                    ("ByteParserTrimble.ByteParserJsonLogger", true),
                    ("ByteParserTrimble.ByteParserNewRelic", true),
                    ("ByteParserTrimble.ByteParserSumoLogic", true),
                    ("ByteParserTrimble.ByteParserDocker", true),
                    ("SmartLogReader.ByteParserPlainJson", true),
                    ("SmartLogReader.ByteParserPlainText", true),
                    ("ByteParserTrimble.ByteParserLegacy", true),
                    ("SmartLogReader.Common.ByteParser", true),
                };
            }

            if (string.IsNullOrEmpty(lastParsers))
                ConfigurePlugins();
            else
                InitConfiguration();
        }

        public static string LastParsers => JsonConvert.SerializeObject(configuredParsers, Formatting.None);

        public static string CreateParser(string path, out IByteParser byteParser)
        {
            byteParser = null;
            var bytes = Utils.ReadBytes(path);

            if (bytes.Length == 0)
                return path;

            foreach (var (name, isSelected) in configuredParsers)
            {
                if (isSelected)
                {
                    var parser = existingParsers[name];
                    var ok = parser.CheckFormat(bytes, out var newPath);
                    if (ok)
                    {
                        byteParser = parser;
                        return newPath ?? path;
                    }
                }
            }

            byteParser = new ByteParser() { Bytes = bytes };
            return path;
        }

        public static bool ConfigurePlugins()
        {
            InitConfiguration();

            var configureVM = new ConfigurePluginsVM(configuredParsers);
            var dlg = new ConfigurePluginsDialog { ViewModel = configureVM };

            if (!dlg.ShowDialog(ViewModel.ConfigurePluginsCmd.Text))
                return false;

            configuredParsers.Clear();

            foreach (var parserVM in configureVM.Plugins)
                configuredParsers.Add((parserVM.Name, parserVM.IsSelected));

            return true;
        }

        private static void InitConfiguration()
        {
            LookForExistingParsers();

            // remove not-existing parsers
            var toBeRemoved = new List<(string, bool)>();

            foreach (var parser in configuredParsers)
                if (!existingParsers.ContainsKey(parser.Name))
                    toBeRemoved.Add(parser);

            foreach (var parser in toBeRemoved)
                configuredParsers.Remove(parser);

            // add missing parsers
            foreach (var name in existingParsers.Keys)
            {
                var found = configuredParsers.Find(x => x.Name == name);
                if (found == default)
                    configuredParsers.Add((name, false));
            }
        }

        private static void LookForExistingParsers()
        {
            existingParsers.Clear();

            var smartLogReader = typeof(ByteParserManager).Assembly;
            GetParsers(smartLogReader);

            var smartLogReaderCommon = typeof(IByteParser).Assembly;
            GetParsers(smartLogReaderCommon);

            var dir = Path.Combine(Path.GetDirectoryName(smartLogReader.Location), "Plugins");
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

        private static void GetParsers(Assembly assembly)
        {
            Log.Information(new { assembly.Location });
            var types = assembly.GetExportedTypes();

            foreach (var type in types.Where(x => !x.IsInterface))
            {
                if (typeof(IByteParser).IsAssignableFrom(type))
                {
                    var instance = Activator.CreateInstance(type);
                    if (instance is IByteParser parser)
                    {
                        Log.Debug(new { type.FullName });
                        existingParsers[type.FullName] = parser;
                    }
                }
            }
        }
    }
}
