using Newtonsoft.Json.Linq;
using SmartLogging;

namespace SmartLogReader.Common;

public class LogEntryJson : LogEntry
{
    public JObject Json { get; set; }
}
