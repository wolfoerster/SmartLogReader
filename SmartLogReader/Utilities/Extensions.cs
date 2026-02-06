using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
