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
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using SmartLogging;
using SmartLogReader.Common;

namespace SmartLogReader
{
    public static class ByteParserManager
    {
        private static readonly SmartLogger Log = new SmartLogger();
        private static readonly List<IByteParser> byteParsers = new List<IByteParser>();

        public static void Initialize()
        {
            var byteParsers = GetByteParsers(Assembly.GetExecutingAssembly());
            byteParsers.AddRange(GetByteParsers("."));
            byteParsers.AddRange(GetByteParsers("Plugins"));   // warum ist SmartLogReader.Common.dll auch darin???
        }

        private static List<Type> GetByteParsers(Assembly assembly)
        {
            var list = new List<Type>();
            var types = assembly.GetExportedTypes();
            var targetName = typeof(IByteParser).FullName;

            foreach (var type in types)
            {
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (interfaceType.FullName == targetName)
                    {
                        Log.Information(new { type.FullName });
                        list.Add(type);
                    }
                }
                //if (type.GetInterfaces().Contains(typeof(IByteParser)))
                ////if (typeof(IByteParser).IsAssignableFrom(type))
                //{
                //    Log.Information(new { type.FullName });
                //    list.Add(type);
                //}
            }

            return list;
        }

        private static List<Type> GetByteParsers(string subDir)
        {
            var dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), subDir);
            var files = Directory.GetFiles(dir, "*.dll");
            var list = new List<Type>();

            foreach (var file in files)
            {
                try
                {
                    var assembly = Assembly.LoadFile(file);
                    list.AddRange(GetByteParsers(assembly));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }

            return list;
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

    }
}
