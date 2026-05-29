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

using SmartLogging;

namespace SmartLogReader.Common;

public interface IByteParser
{
    /// <summary>
    /// Check if there are log entries of known format.
    /// If yes, the ByteParser can preprocess the file and store the result in a new file
    /// which is then opened with the LogReader. This step is optional.
    /// </summary>
    bool CheckFormat(byte[] bytes, out string newFileName);

    /// <summary>
    /// The bytes which contain the log entries.
    /// </summary>
    byte[] Bytes { get; set; }

    /// <summary>
    /// Gets the current position, i.e. the position of the next log entry.
    /// </summary>
    int CurrentPosition { get; }

    /// <summary>
    /// Reads the next log entry and updates the current position.
    /// </summary>
    LogEntry ReadNextEntry();

    /// <summary>
    /// Create a copy of the current instance.
    /// </summary>
    IByteParser Clone();
}
