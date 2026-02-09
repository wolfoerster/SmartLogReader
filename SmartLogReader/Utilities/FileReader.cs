//******************************************************************************************
// Copyright © 2017 - 2025 Wolfgang Foerster (wolfoerster@gmx.de)
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
using SmartLogging;
using SmartLogReader.Common;

namespace SmartLogReader
{
    public class FileReader
    {
        public FileReader()
        {
            log = new SmartLogger($"{GetType().Name}.{++instanceCounter}");
            log.Debug();
        }
        private static int instanceCounter;
        protected readonly SmartLogger log;

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

                //--- if the file is smaller now it most probably has been deleted in the meantime
                if (fileInfo.Length < prevLength)
                {
                    IsNewLogFile = true;
                    prevLength = 0;
                }

                //--- now read the next bytes
                int count = (int)(fileInfo.Length - prevLength);
                var bytes = Utils.ReadBytes(path, prevLength, count);
                prevLength = fileInfo.Length;
                return bytes;
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
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
                return Utils.ReadBytes(path, 0, fileInfo.Length);
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }
            return null;
        }
    }
}
