//******************************************************************************************
// Copyright © 2021 - 2026 Wolfgang Foerster (wolfoerster@gmx.de)
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

using System.Windows.Data;
using Newtonsoft.Json;

namespace SmartLogReader.Utilities
{
    internal static class Extensions
    {
        public static string ToJson(this object obj) => JsonConvert.SerializeObject(obj, Formatting.None);

        public static Record FindMatchingRecord(this ListCollectionView view, Record record)
        {
            Record match = null;

            for (int i = 0; i < view.Count; i++)
            {
                var candidate = view.GetItemAt(i) as Record;

                if (candidate.UtcTime >= record.UtcTime)
                {
                    match = candidate;

                    for (var j = i + 1; j < view.Count; j++)
                    {
                        candidate = view.GetItemAt(j) as Record;

                        if (candidate.UtcTime > record.UtcTime)
                            break;

                        if (candidate.Equals(record))
                        {
                            match = candidate;
                            break;
                        }
                    }

                    break;
                }
            }

            if (match == null && view.Count > 0)
                match = view.GetItemAt(view.Count - 1) as Record;

            return match;
        }
    }
}
