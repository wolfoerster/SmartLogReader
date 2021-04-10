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

    public class SimpleLogger
    {
        #region Private stuff

        private static readonly long MaxLength = 8 * 1024 * 1024; // new log file at 8 MB
        private static readonly ConcurrentQueue<string> LogEntries = new ConcurrentQueue<string>();
        private static readonly object Locker = new Object();
        private static Task writerTask;
        private readonly string className;
        private readonly int appDomainId;
        private readonly int processId;

        #endregion Private stuff

        public SimpleLogger(object context = null)
        {
            if (context == null)
            {
                var stackTrace = new StackTrace();
                var method = stackTrace.GetFrame(1).GetMethod();
                context = method.DeclaringType;
            }

            this.className = GetClassName(context);

            using (var process = Process.GetCurrentProcess())
            {
                this.processId = process.Id;
                this.appDomainId = AppDomain.CurrentDomain.Id;
            }
        }

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

            var log = new SimpleLogger(typeof(SimpleLogger));
            log.None("Start logging");
        }

        public void Verbose(string message = null, [CallerMemberName] string methodName = null)
        {
            this.Write(message, LogLevel.Verbose, methodName);
        }

        public void Debug(string message = null, [CallerMemberName] string methodName = null)
        {
            this.Write(message, LogLevel.Debug, methodName);
        }

        public void Information(string message = null, [CallerMemberName] string methodName = null)
        {
            this.Write(message, LogLevel.Information, methodName);
        }

        public void Warning(string message = null, [CallerMemberName] string methodName = null)
        {
            this.Write(message, LogLevel.Warning, methodName);
        }

        public void Error(string message = null, [CallerMemberName] string methodName = null)
        {
            this.Write(message, LogLevel.Error, methodName);
        }

        public void Fatal(string message = null, [CallerMemberName] string methodName = null)
        {
            this.Write(message, LogLevel.Fatal, methodName);
        }

        public void None(string message = null, [CallerMemberName] string methodName = null)
        {
            this.Write(message, LogLevel.None, methodName);
        }

        public void Exception(Exception exception, [CallerMemberName] string methodName = null)
        {
            this.Fatal($"EXCEPTION: {GetMessage(exception)}\r\n{exception.StackTrace}", methodName);
        }

        public void Write(string message, LogLevel level, [CallerMemberName] string methodName = null)
        {
            if (level < MinimumLogLevel)
                return;

            try
            {
                if (FileName == null)
                    Init();

                var entry = CreateLogEntry(level, methodName, message);
                LogEntries.Enqueue(entry);
            }
            catch
            {
            }
        }

        #region Private stuff

        private string CreateLogEntry(LogLevel level, string methodName, string message)
        {
            string threadIds = string.Format("{0}/{1}/{2}", 
                this.processId, 
                this.appDomainId, 
                Thread.CurrentThread.ManagedThreadId);

            return string.Format("{0} {1} {2} {3} {4} {5}",
                DateTime.UtcNow.ToString("o"),
                threadIds,
                level.ToString(),
                this.className,
                methodName,
                message);
        }

        private static string GetClassName(object context)
        {
            if (context is Type type)
                return type.FullName;

            if (context is string sourceContext)
                return string.IsNullOrWhiteSpace(sourceContext) ? "-?-" : sourceContext;

            return context.GetType().FullName;
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

        #endregion Private stuff

        #region WriterLoop

        private static void WriterLoop()
        {
            DateTime t0 = DateTime.UtcNow;
            while (true)
            {
                while (LogEntries.TryDequeue(out string entry))
                {
                    try
                    {
                        using (StreamWriter sw = File.AppendText(FileName))
                        {
                            sw.WriteLine(entry);
                        }
                    }
                    catch (Exception exception)
                    {
                        var str = GetMessage(exception);
                        Trace.WriteLine(str);
                    }
                }

                Thread.Sleep(30);

                if ((DateTime.UtcNow - t0).TotalSeconds > 10)
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
                    lock (Locker)
                    {
                        var backupName = FileName + ".log";
                        File.Delete(backupName);
                        File.Move(FileName, backupName);
                    }
                }
            }
        }

        #endregion WriterLoop
    }
}
