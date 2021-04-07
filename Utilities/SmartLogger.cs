//******************************************************************************************
// Copyright © 2021 Wolfgang Foerster (wolfoerster@gmx.de)
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

namespace SmartLogReader
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public enum LogLevel
    {
        Verbose,
        Debug,
        Information,
        Warning,
        Error,
        Fatal,
        None
    }

    public class SmartLogger
    {
        private static readonly long MaxLength = 4 * 1024 * 1024; // new log file at 4 MB
        private static readonly ConcurrentQueue<LogEntry> LogEntries = new ConcurrentQueue<LogEntry>();
        private static readonly object Locker = new Object();
        private static Task writerTask;
        private readonly string sourceContext;
        private readonly int appDomainId;
        private readonly int processId;

        public SmartLogger(object obj = null)
        {
            if (obj == null)
            {
                var stackTrace = new StackTrace();
                var method = stackTrace.GetFrame(1).GetMethod();
                obj = method.DeclaringType;
            }

            this.sourceContext = GetSourceContext(obj);

            using (var process = Process.GetCurrentProcess())
            {
                this.processId = process.Id;
                this.appDomainId = AppDomain.CurrentDomain.Id;
            }
        }

        public delegate string SerializeObjectFunc(object obj);

        public static SerializeObjectFunc SerializeObject;

        public static LogLevel MinimumLogLevel = LogLevel.Information;

        public static string FileName { get; private set; }

        public static void Init(string fileName = null)
        {
            if (writerTask != null)
                return;

            if (fileName == null)
            {
                var name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
                FileName = Path.Combine(Path.GetTempPath(), $"{name}.log");
            }
            else
            {
                FileName = fileName;
            }

            writerTask = Task.Run(() => WriterLoop());

            var log = new SmartLogger(typeof(SmartLogger));
            log.None("Start logging");
        }

        public void Verbose(object msg = null, [CallerMemberName] string methodName = null)
        {
            this.Write(msg, LogLevel.Verbose, methodName);
        }

        public void Debug(object msg = null, [CallerMemberName] string methodName = null)
        {
            this.Write(msg, LogLevel.Debug, methodName);
        }

        public void Information(object msg = null, [CallerMemberName] string methodName = null)
        {
            this.Write(msg, LogLevel.Information, methodName);
        }

        public void Warning(object msg = null, [CallerMemberName] string methodName = null)
        {
            this.Write(msg, LogLevel.Warning, methodName);
        }

        public void Error(object msg = null, [CallerMemberName] string methodName = null)
        {
            this.Write(msg, LogLevel.Error, methodName);
        }

        public void Fatal(object msg = null, [CallerMemberName] string methodName = null)
        {
            this.Write(msg, LogLevel.Fatal, methodName);
        }

        public void None(object msg = null, [CallerMemberName] string methodName = null)
        {
            this.Write(msg, LogLevel.None, methodName);
        }

        public void Exception(Exception exception, [CallerMemberName] string methodName = null)
        {
            this.Error(new { Message = GetMessage(exception), exception.StackTrace }, methodName);
        }

        public void Write(object msg, LogLevel level, [CallerMemberName] string methodName = null)
        {
            if (level < MinimumLogLevel)
                return;

            try
            {
                if (FileName == null)
                    Init();

                var entry = CreateLogEntry(level, methodName, msg);
                LogEntries.Enqueue(entry);
            }
            catch
            {
            }
        }

        private LogEntry CreateLogEntry(LogLevel level, string methodName, object msg)
        {
            string threadIds = string.Format("{0}/{1}/{2}", this.processId, this.appDomainId, Thread.CurrentThread.ManagedThreadId);

            return new LogEntry
            {
                Time = DateTime.UtcNow.ToString("o"),
                ThreadIds = threadIds,
                Level = level.ToString(),
                Class = this.sourceContext,
                Method = methodName,
                Message = ToJson(msg, true),
            };
        }

        private static string GetSourceContext(object obj)
        {
            if (obj is Type type)
                return type.FullName;

            if (obj is string sourceContext)
                return sourceContext;

            return obj.GetType().FullName;
        }

        private static string GetMessage(Exception exception)
        {
            var sb = new StringBuilder();
            sb.Append(exception.Message);

            exception = exception.InnerException;
            while (exception != null)
            {
                sb.Append(" InnerException: ");
                sb.Append(exception.Message);
                exception = exception.InnerException;
            }

            return sb.ToString();
        }

        #region WriterLoop

        private static void WriterLoop()
        {
            DateTime t0 = DateTime.UtcNow;
            while (true)
            {
                while (LogEntries.TryDequeue(out LogEntry entry))
                {
                    try
                    {
                        using (StreamWriter sw = File.AppendText(FileName))
                        {
                            var json = ToJson(entry, false);
                            sw.WriteLine(json);
                        }
                    }
                    catch (Exception exception)
                    {
                        var str = GetMessage(exception);
                        Trace.WriteLine(str);
                    }
                }

                Thread.Sleep(30);

                if ((DateTime.UtcNow - t0).TotalSeconds > 3)
                {
                    try
                    {
                        CheckFileSize();
                    }
                    catch (Exception exception)
                    {
                        var str = GetMessage(exception);
                        Trace.WriteLine(str);
                    }

                    t0 = DateTime.UtcNow;
                }
            }
        }

        private static void CheckFileSize()
        {
            if (File.Exists(FileName))
            {
                var fileInfo = new FileInfo(FileName);
                if (fileInfo.Length > MaxLength)
                {
                    var backupName = FileName + ".log";

                    lock (Locker)
                    {
                        File.Delete(backupName);
                        File.Move(FileName, backupName);
                    }

                    var log = new SmartLogger(typeof(SmartLogger));
                    log.None(new { logFileSize = fileInfo.Length, allowedSize = MaxLength, backupName });
                }
            }
        }

        #endregion WriterLoop

        #region JSON Serializer

        private static string ToJson(object obj, bool isInnerObject)
        {
            if (obj == null)
                return null;

            if (obj is Exception exception)
            {
                var msg = GetMessage(exception);
                return ToJson(msg, isInnerObject);
            }

            try
            {
                if (SerializeObject != null)
                {
                    return SerializeObject(obj);
                }

                return SerializeInternal(obj, isInnerObject);
            }
            catch
            {
                return obj.ToString();
            }
        }

        private static string SerializeInternal(object obj, bool isInnerObject)
        {
            if (obj is string json)
            {
                return isInnerObject ? Escape(json) : json;
            }

            var infos = obj.GetType().GetProperties();
            if (infos.Length == 0)
                return obj.ToString();

            var q = GetQuotes(isInnerObject);
            var sb = new StringBuilder();
            sb.Append("{");

            for (int i = 0; i < infos.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');

                var propertyName = infos[i].Name;
                sb.Append($"{q}{propertyName}{q}:");

                var propertyValue = infos[i].GetValue(obj);
                var valueString = GetString(propertyValue, isInnerObject);
                sb.Append(valueString);
            }

            sb.Append("}");
            return sb.ToString();
        }

        private static string Escape(string json)
        {
            if (!json.Contains("\\"))
                return json;

            var result = json.Replace("\\", "\\\\");
            return result;
        }

        private static string GetString(object value, bool isInnerObject)
        {
            if (value == null)
                return "null";

            if (value is bool b)
                return b.ToString().ToLowerInvariant();

            if (value is double d)
                return d.ToString();

            if (value is int i)
                return i.ToString();

            var q = GetQuotes(isInnerObject);

            return $"{q}{value}{q}";
        }

        private static string GetQuotes(bool isInnerObject)
        {
            return isInnerObject ? "\\\"" : "\"";
        }

        #endregion JSON Serializer

        private class LogEntry
        {
            public string Time { get; set; }
            public string ThreadIds { get; set; }
            public string Level { get; set; }
            public string Class { get; set; }
            public string Method { get; set; }
            public string Message { get; set; }
        }
    }
}
