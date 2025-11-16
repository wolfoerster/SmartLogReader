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
using System.Linq;
using MessageTemplates.Core;
using MessageTemplates.Parsing;
using MessageTemplates.Structure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartLogging;
using SmartLogReader.Common;

namespace SmartLogReader
{
    /// <summary>
    /// A byte parser for JSON based logger (e.g. JsonLogger). Example:
    /// {"Timestamp":"2025-11-16 10:32:58.278","Level":"Warning","MessageTemplate":"{@Message}","Properties":{"SourceContext":"MyClass","MethodName":"MyMethod","ConnectionId":"4711","Message":"{\"IsValid\":true,\"HasChild\":false,\"Result\":3.14}"}}
    /// </summary>
    public class ByteParserJsonLogger : ByteParserSmartLogger
    {
        public ByteParserJsonLogger()
        {
        }

        public ByteParserJsonLogger(byte[] bytes)
        {
            Bytes = bytes;
        }

        public override bool CheckFormat(byte[] bytes, out string newFileName)
        {
            newFileName = null;
            return CheckForString("{\"Timestamp\"", bytes, 0);
        }

        protected override LogEntry ReadEntry()
        {
            var entry = new LogEntryJson();
            GetJsonRecord2(entry, GetNextLine());
            return entry;
        }

        protected void GetJsonRecord2(LogEntryJson entry, string json)
        {
            var logEntry = JsonConvert.DeserializeObject<LogEntry2>(json);

            if (logEntry?.Timestamp == null)
            {
                return;
            }

            DateTime t = DateTime.Parse(logEntry.Timestamp);
            entry.Time = t.ToUniversalTime().ToStringN();
            entry.Level = logEntry.Level;
            entry.Context = logEntry.GetProperty("SourceContext");
            entry.Method = logEntry.GetProperty("MethodName");
            entry.Message = logEntry.GetMessage();
            entry.Annex = logEntry.GetProperty("ConnectionId");
            entry.Json = JObject.Parse(json);
        }

        private class LogEntry2
        {
            public string Timestamp { get; set; }
            public string Level { get; set; }
            public string MessageTemplate { get; set; }
            public JObject Properties { get; set; }

            public string GetProperty(string name)
            {
                if (Properties == null)
                    return "";

                if (Properties.TryGetValue(name, out JToken value))
                    return value.ToString();

                return "";
            }

            public string GetMessage()
            {
                if (MessageTemplate.Equals("{@Message}")) // JsonLogger
                {
                    var obj = Properties["Message"];
                    return JsonConvert.SerializeObject(obj);
                }

                if (MessageTemplate.StartsWith("Message: {")) // Chimera Logging Middleware
                {
                    var json = MessageTemplate.Substring(9);
                    var jobj = JObject.Parse(json);
                    return JsonConvert.SerializeObject(jobj);
                }

                if (Properties == null)
                    return MessageTemplate;

                var parser = new MessageTemplateParser();
                var parsed = parser.Parse(MessageTemplate);

                var templateProperties = new TemplatePropertyValueDictionary(new TemplatePropertyList(
                    Properties.Properties().Select(p => CreateProperty(p.Name, p.Value)).ToArray()));

                var rendered = parsed.Render(templateProperties);
                return rendered;
            }

            static TemplateProperty CreateProperty(string name, JToken value)
            {
                return new TemplateProperty(name, CreatePropertyValue(value));
            }

            static TemplatePropertyValue CreatePropertyValue(JToken value)
            {
                if (value.Type == JTokenType.Null)
                    return new ScalarValue(null);

                var obj = value as JObject;
                if (obj != null)
                {
                    var properties = obj.Properties()
                        .Select(kvp => CreateProperty(kvp.Name, kvp.Value));

                    return new StructureValue(properties);
                }

                var arr = value as JArray;
                if (arr != null)
                {
                    return new SequenceValue(arr.Select(CreatePropertyValue));
                }

                return new ScalarValue(value.Value<object>());
            }
        }
    }
}
