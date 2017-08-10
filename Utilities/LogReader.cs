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
using System.Threading;
using System.ComponentModel;
using SmartLogging;
using System.Diagnostics;

namespace SmartLogReader
{
    /// <summary>
    /// 
    /// </summary>
    public enum ReaderStatus
    {
        FileChanged,
        StartedWork,
        RecordsChanged,
        ProgressChanged,
        FinishedWork,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum LogReadMode
    {
        AllRecords,
        LastSession,
        Last24Hours,
        Last8Hours,
        LastHour
    }

    /// <summary>
    /// 
    /// </summary>
    public class LogReader : RollingFileReader
    {
        /// <summary>
        /// 
        /// </summary>
        static LogReader()
        {
            ReadMode = LogReadMode.LastSession;
        }

        /// <summary>
        /// 
        /// </summary>
        public LogReader()
        {
            Records = new RecordCollection();

            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += DoWork;
            worker.ProgressChanged += ProgressChanged;
            worker.RunWorkerCompleted += RunWorkerCompleted;
        }
        private readonly BackgroundWorker worker;

        /// <summary>
        /// 
        /// </summary>
        public override string ToString()
        {
            string name = Path.GetFileName(fileName);
            return "LogReader[" + name + "]";
        }

        /// <summary>
        /// 
        /// </summary>
        public static LogLevel Level;

        /// <summary>
        /// 
        /// </summary>
        public static LogReadMode ReadMode
        {
            get { return readMode; }
            set
            {
                hours = 0;
                readMode = value;
                switch (readMode)
                {
                    case LogReadMode.LastHour: hours = 1; break;
                    case LogReadMode.Last8Hours: hours = 8; break;
                    case LogReadMode.Last24Hours: hours = 24; break;
                }
            }
        }
        static LogReadMode readMode;
        static int hours;

        /// <summary>
        /// 
        /// </summary>
        public RecordCollection Records { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        void CheckFile(string logFile = null)
        {
            bool hasChanges = false;
            Record record = null;
            byte[] bytes = null;

            if (logFile == null)
            {
                if (NoFileToReadFrom())
                    return;

                bytes = ReadNextBytes();
            }
            else
            {
                bytes = ReadAllBytes(logFile);
            }

            //--- extract records
            if (bytes != null)
            {
                //log.Smart($"read {bytes.Length} new bytes");
                Stopwatch watch = Stopwatch.StartNew();

                if (byteParser == null)
                    byteParser = new ByteParser(bytes);
                else
                    byteParser.Bytes = bytes;

                while (true)
                {
                    Record next = byteParser.GetNextRecord();
                    if (next == null)
                        break;

                    record = next;
                    if (watch.ElapsedMilliseconds > 60)
                    {
                        watch.Restart();
                        Progress = byteParser.CurrentPosition / bytes.Length;
                        ReportStatus(ReaderStatus.ProgressChanged);
                    }

                    if (ReadMode == LogReadMode.LastSession && IsNewSession(record) && Records.Count > 0)
                        Records.Clear();

                    if (CheckLevelAndTime(record))
                    {
                        hasChanges = true;
                        Records.Add(record);
                    }
                }

                if (hasChanges)
                {
                    ReportStatus(ReaderStatus.RecordsChanged);
                }
            }

            //--- if this is the first call and there are no changes, report RecordsChanged anyway
            if (firstCall && !hasChanges)
                ReportStatus(ReaderStatus.RecordsChanged);

            firstCall = false;
        }
        bool firstCall;
        ByteParser byteParser;

        /// <summary>
        /// 
        /// </summary>
        private bool NoFileToReadFrom()
        {
            bool didExist = fileExists;
            fileExists = File.Exists(FileName);

            //--- clear everything except FileName if the file does not exist anymore
            if (!fileExists)
            {
                if (didExist)
                {
                    Reset();
                    log.Smart($"Lost file for {FileName}");
                }
            }
            else
            {
                if (!didExist)
                    log.Smart($"Found again file for {FileName}");
            }

            //--- another file might exist now
            return !fileExists;
        }
        protected bool fileExists;

        /// <summary>
        /// 
        /// </summary>
        protected void Reset(string newFileName = null)
        {
            byteParser = null;

            if (newFileName != null)
            {
                fileName = "???";
                FileName = newFileName;
                ReportStatus(ReaderStatus.FileChanged);
            }

            if (Records.Count > 0)
            {
                Records.Clear();
                if (!firstCall)
                    ReportStatus(ReaderStatus.RecordsChanged);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal void LoadFile(string path)
        {
            log.Smart($"path = {path}");
            if (IsBusy)
                throw new Exception("LoadFile IsBusy");

            fileExists = File.Exists(path);
            firstCall = true;
            Reset(path);
            worker.RunWorkerAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        public double Progress { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        protected virtual bool CheckLevelAndTime(Record record)
        {
            if (record.Level < Level)
                return false;

            if (hours == 0)
                return true;

            DateTime now = DateTime.UtcNow;
            TimeSpan span = now - record.UtcTime;
            return span.TotalHours < hours;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual bool IsNewSession(Record record)
        {
            return record.Message.StartsWith("Start logging");
        }

        #region BackgroundWorker

        /// <summary>
        /// 
        /// </summary>
        public delegate void ReaderStatusChangedEventHandler(object sender, ReaderStatus code, string text);

        /// <summary>
        /// 
        /// </summary>
        public event ReaderStatusChangedEventHandler StatusChanged;

        /// <summary>
        /// 
        /// </summary>
        public bool IsBusy
        {
            get { return worker.IsBusy; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Stop(string reason)
        {
            if (!IsBusy)
                return false;

            cancelReason = reason;
            log.Smart($"reason = '{reason}'");
            worker.CancelAsync();
            return true;
        }
        string cancelReason;

        /// <summary>
        /// 
        /// </summary>
        void DoWork(object sender, DoWorkEventArgs e)
        {
            log.Smart("begin");
            ReportStatus(ReaderStatus.StartedWork);

            //--- first check if there is a rolled file
            string rolledFile = fileName + ".1";
            if (File.Exists(rolledFile))
                CheckFile(rolledFile);

            //--- go into an endless loop and check the file every second
            for (int count = 0; ; ++count)
            {
                if (count == 0)
                    CheckFile();

                if (worker.CancellationPending)
                {
                    log.Smart("break");
                    e.Cancel = true;
                    break;
                }

                Thread.Sleep(50);
                if (count > 19)
                    count = -1;
            }

            log.Smart("end");
        }

        /// <summary>
        /// 
        /// </summary>
        void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string msg = "Completed";

            if (e.Cancelled == true)
                msg = "Cancelled";

            else if (e.Error != null)
                msg = "Error: " + e.Error.Message;

            log.Smart($"reason: '{cancelReason}', message: {msg}");
            ReportStatus(ReaderStatus.FinishedWork, cancelReason);
        }

        void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            StatusChanged?.Invoke(this, (ReaderStatus)e.ProgressPercentage, e.UserState as string);
        }

        void ReportStatus(ReaderStatus code, string text = null)
        {
            if (worker.IsBusy)
            {
                try
                {
                    worker.ReportProgress((int)code, text);
                    return;
                }
                catch (Exception e)
                {
                    log.Exception(e);
                }
            }

            StatusChanged?.Invoke(this, code, text);
        }

        #endregion BackgroundWorker
    }
}
