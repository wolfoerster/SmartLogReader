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

namespace SmartLogReader.ByteParser
{
    /// <summary>
    /// A byte parser for a plain text based logger with messages in JSON format.
    /// 
    /// A log entry has to look like this:
    /// 2025-10-28T11:16:04.2881241Z|1|Info|Basics.Logging.LogManager|CreateLoggerFactory|Start logging
    /// 
    /// In general:
    /// UTC DateTime|ThreadId|LogLevel|ClassName|MethodName|Message
    /// 
    /// The message might span several lines. Everything from the begin of the message up to
    /// the next DateTime field is considered as message (including line feeds and spaces).
    /// </summary>
    public class ByteParserPlainJson : ByteParser
    {
        public ByteParserPlainJson(byte[] bytes)
        {
            if (false)
            {
                Bytes = bytes;
            }
        }
    }
}
