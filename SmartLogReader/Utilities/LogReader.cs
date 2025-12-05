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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SmartLogging;
using SmartLogReader.Common;

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

    internal enum FileOrigin
    {
        Local,
        NewRelic,
        SumoLogic,
    }

    /// <summary>
    /// 
    /// </summary>
    public class LogReader : FileReader
    {
        private readonly BackgroundWorker worker;

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
        public static LogReadMode ReadMode = LogReadMode.LastSession;

        /// <summary>
        /// 
        /// </summary>
        public RecordCollection Records { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        void ExtractRecords(byte[] bytes)
        {
            if (IsNewLogFile && ReadMode == LogReadMode.LastSession && Records.Count > 0)
                Records.Clear();

            bool hasChanges = false;
            if (bytes != null)
            {
                byteParser.Bytes = bytes;
                var watch = Stopwatch.StartNew();

                while (true)
                {
                    var entry = byteParser.ReadNextEntry();
                    if (entry == null)
                        break;

#warning do we still need it?
                    //if (entry.Time == null && byteParser is ByteParserJsonLogger)
                    //    continue;

                    if (watch.ElapsedMilliseconds > 60)
                    {
                        watch.Restart();
                        Progress = byteParser.CurrentPosition / (double)bytes.Length;
                        ReportStatus(ReaderStatus.ProgressChanged);
                    }

                    var record = new Record(entry);

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
            if (firstCall)
            {
                if (Records.Count > 0)
                    Record.UtcTime0 = Records[0].UtcTime;

                if (!hasChanges)
                    ReportStatus(ReaderStatus.RecordsChanged);
            }

            firstCall = false;
        }
        bool firstCall;
        IByteParser byteParser;

        /// <summary>
        /// 
        /// </summary>
        private bool FileExists()
        {
            bool didExist = fileExists;
            fileExists = File.Exists(FileName);

            //--- clear everything except FileName if the file does not exist anymore
            if (!fileExists)
            {
                if (didExist)
                {
                    Reset(FileName);
                    log.Debug($"Lost file for {FileName}");
                }
            }
            else
            {
                if (!didExist)
                {
                    log.Debug($"Found again file for {FileName}");
                    if (byteParser == null)
                        ByteParserManager.CreateParser(FileName, out byteParser);
                }
            }

            return fileExists;
        }
        private bool fileExists;

        /// <summary>
        /// 
        /// </summary>
        protected void Reset(string path)
        {
            byteParser = null;
            fileName = null;
            FileName = path;
            ReportStatus(ReaderStatus.FileChanged);

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
            log.Debug($"path = {path}");
            if (IsBusy)
                throw new Exception("LoadFile IsBusy");

            IByteParser parser = null;
            fileExists = File.Exists(path);

            if (fileExists)
                path = ByteParserManager.CreateParser(path, out parser);

            firstCall = true;
            Reset(path);

            byteParser = parser;
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

            double hours = (DateTime.UtcNow - record.UtcTime).TotalHours;
            switch (ReadMode)
            {
                case LogReadMode.LastHour: return hours < 1;
                case LogReadMode.Last8Hours: return hours < 8;
                case LogReadMode.Last24Hours: return hours < 24;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual bool IsNewSession(Record record)
        {
            if (record.Message.StartsWith("Start logging"))
                return true;

            return record.Message.StartsWith("Application started");
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
            log.Debug($"reason = '{reason}'");
            worker.CancelAsync();
            return true;
        }
        string cancelReason;

        /// <summary>
        /// 
        /// </summary>
        void DoWork(object sender, DoWorkEventArgs e)
        {
            log.Debug("begin");
            ReportStatus(ReaderStatus.StartedWork);

            //--- first check if there is a backup file
            string backupFile = GetBackupFileName();
            if (File.Exists(backupFile))
            {
                byte[] bytes = ReadBytes(backupFile);
                ExtractRecords(bytes);
            }

            //--- go into an endless loop and check the file every second
            for (int count = 0; ; ++count)
            {
                if (count == 0 && FileExists())
                {
                    byte[] bytes = ReadNextBytes();
                    ExtractRecords(bytes);
                }

                if (worker.CancellationPending)
                {
                    log.Debug("break");
                    e.Cancel = true;
                    break;
                }

                Thread.Sleep(50);
                if (count > 19)
                    count = -1;
            }

            log.Debug("end");
        }

        private string GetBackupFileName()
        {
            var ext = Path.GetExtension(fileName);

            if (string.IsNullOrEmpty(ext))
                ext = ".log";

            return fileName + ext;
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

            log.Debug($"reason: '{cancelReason}', message: {msg}");
            ReportStatus(ReaderStatus.FinishedWork, cancelReason);
        }

        /// <summary>
        /// 
        /// </summary>
        void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            StatusChanged?.Invoke(this, (ReaderStatus)e.ProgressPercentage, e.UserState as string);
        }

        /// <summary>
        /// 
        /// </summary>
        void ReportStatus(ReaderStatus status, string text = null)
        {
            if (worker.IsBusy)
            {
                try
                {
                    worker.ReportProgress((int)status, text);
                    return;
                }
                catch (Exception e)
                {
                    log.Error(e.ToString());
                }
            }

            StatusChanged?.Invoke(this, status, text);
        }

#endregion BackgroundWorker
    }
}
