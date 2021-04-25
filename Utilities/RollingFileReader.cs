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
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SmartLogReader
{
    public class RollingFileReader
    {
        public RollingFileReader()
        {
            log = new SimpleLogger($"{GetType().Name}.{++instanceCounter}");
            log.Debug();
        }
        private static int instanceCounter;
        protected readonly SimpleLogger log;

        /// <summary>
        /// The name of the related file.
        /// </summary>
        public string FileName
        {
            get { return fileName; }
            set
            {
                if (fileName != value)
                {
                    fileName = value;
                    prevLength = 0;
                    log.Debug(fileName);
                }
            }
        }
        protected string fileName;

        /// <summary>
        /// The size of the related file.
        /// </summary>
        public long FileSize
        {
            get { return prevLength; }
        }
        private long prevLength;

        /// <summary>
        /// Read latest bytes from the related file.
        /// </summary>
        public byte[] ReadNextBytes()
        {
            return ReadNextBytes(fileName);
        }

        protected bool IsNewLogFile;

        /// <summary>
        /// Read latest bytes from a specified file.
        /// </summary>
        private byte[] ReadNextBytes(string path)
        {
            IsNewLogFile = false;
            try
            {
                //--- if the file size did not change, we're done
                FileInfo fileInfo = new FileInfo(path);
                if (fileInfo.Length == prevLength)
                    return null;

                //--- if the file is smaller now, check the rolled file
                byte[] rolledBytes = null;
                if (fileInfo.Length < prevLength)
                {
                    string rolledFile = path + ".1";
                    if (File.Exists(rolledFile))
                    {
                        log.Debug($"reading rolled file {rolledFile}");
                        rolledBytes = ReadNextBytes(rolledFile);
                        log.Debug($"found {rolledBytes?.Length} bytes in rolled file");
                    }
                    else
                    {
                        IsNewLogFile = true;
                    }
                    prevLength = 0;
                }

                //--- now go on with the current file
                int count = (int)(fileInfo.Length - prevLength);
                var bytes = ReadBytes(path, prevLength, count);
                prevLength = fileInfo.Length;

                if (rolledBytes == null || rolledBytes.Length == 0)
                    return bytes;

                return rolledBytes.Concat(bytes).ToArray();
            }
            catch (Exception e)
            {
                log.Exception(e);
            }
            return null;
        }

        /// <summary>
        /// Read some bytes from a specified file.
        /// </summary>
        private byte[] ReadBytes(string path, long startPosition, long count)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var reader = new BinaryReader(fs))
                    {
                        reader.BaseStream.Seek(startPosition, SeekOrigin.Begin);
                        return reader.ReadBytes((int)count);
                    }
                }
            }
            catch (Exception e)
            {
                log.Exception(e);
            }
            return null;
        }

        /// <summary>
        /// Read all bytes from a specified file.
        /// </summary>
        protected byte[] ReadBytes(string path)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                prevLength = fileInfo.Length;
                return ReadBytes(path, 0, fileInfo.Length);
            }
            catch (Exception e)
            {
                log.Exception(e);
            }
            return null;
        }
    }
}
