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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                Stopwatch watch = Stopwatch.StartNew();

                if (byteParser == null)
                    byteParser = ParserFactory.CreateParser(bytes);
                else
                    byteParser.Bytes = bytes;

                while (true)
                {
                    Record record = byteParser.GetNextRecord();
                    if (record == null)
                        break;

                    if (watch.ElapsedMilliseconds > 60)
                    {
                        watch.Restart();
                        Progress = byteParser.CurrentPosition / (double)bytes.Length;
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

            fileExists = File.Exists(path);
            firstCall = true;
            Reset(path);

            //--- check if this is an extracted SumoLogic or NewRelic file
            var tempFile = FileIsExtracted();
            if (tempFile == null)
            {
                worker.RunWorkerAsync();
            }
            else
            {
                byte[] bytes = ReadBytes(tempFile);
                ExtractRecords(bytes);
                File.Delete(tempFile);
            }
        }

        string FileIsExtracted()
        {
            if (!File.Exists(fileName))
                return null;

            var ext = Path.GetExtension(fileName);

            if (ext.equals(".csv"))
                return FileIsExtractedFromSumoLogic();

            if (ext.equals(".json"))
                return FileIsExtractedFromNewRelic();

            return null;
        }

        /// <summary>
        /// NewRelic files have log entries in reverse order (last first)
        /// </summary>
        string FileIsExtractedFromNewRelic()
        {
            try
            {
                var json = File.ReadAllText(fileName);
                var jobj = JObject.Parse(json);
                if (jobj == null)
                    return null;

                var results = jobj["results"] as JArray;
                if (results == null)
                    return null;

                jobj = results.FirstOrDefault() as JObject;
                if (jobj == null)
                    return null;

                var events = jobj["events"] as JArray;
                if (events == null)
                    return null;

                var lines = new List<string>();
                foreach (var item in events)
                {
                    jobj = item as JObject;
                    if (jobj != null)
                    {
                        lines.Add(JsonConvert.SerializeObject(jobj, Formatting.None));
                    }
                }

                var newFile = Path.GetTempFileName();

                using (var stream = File.OpenWrite(newFile))
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine("extracted from NewRelic");
                    for (int i = lines.Count - 1; i >= 0; i--)
                    {
                        writer.WriteLine(lines[i]);
                    }
                }

                return newFile;
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// SumoLogic files have log entries in reverse order (last first)
        /// </summary>
        string FileIsExtractedFromSumoLogic()
        {
            try
            {
                var lines = File.ReadAllLines(fileName);
                if (lines.Length < 2)
                    return null;

                if (!lines[0].startsWith("\"_messagetimems"))
                    return null;

                var newFile = Path.GetTempFileName();
                var list = lines.Reverse().ToList();
                int i = list.Count - 1;
                string line = list[i];
                list.RemoveAt(i);
                list.Insert(0, line);
                File.WriteAllLines(newFile, list);
                return newFile;
            }
            catch
            {
            }

            return null;
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

            //--- first check if there is a rolled file
            string rolledFile = fileName + ".1";
            if (File.Exists(rolledFile))
            {
                byte[] bytes = ReadBytes(rolledFile);
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
                    log.Exception(e);
                }
            }

            StatusChanged?.Invoke(this, status, text);
        }

#endregion BackgroundWorker
    }
}
