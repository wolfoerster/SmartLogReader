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
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace SmartLogReader
{
    /// <summary>
    /// 
    /// </summary>
    public class SplitLogControlVM : SplitGridViewModel2
    {
        /// <summary>
        /// 
        /// </summary>
        public SplitLogControlVM()
        {
            log = new SimpleLogger($"{GetType().Name}.{++instanceCounter}");
            GridLength0 = 1;
            GridLength2 = 0;
            InitCommands();
        }
        private readonly SimpleLogger log;
        private static int instanceCounter;

        #region Sub viewmodels

        /// <summary>
        /// 
        /// </summary>
        public LogControlVM MyLogControlVM1
        {
            get { return myLogControlVM1; }
            set
            {
                Utils.OnlyOnce(myLogControlVM1, value);
                myLogControlVM1 = value;
                myLogControlVM1.PropertyChanged += SubVMPropertyChanged;
            }
        }
        private LogControlVM myLogControlVM1;

        /// <summary>
        /// 
        /// </summary>
        public LogControlVM MyLogControlVM2
        {
            get { return myLogControlVM2; }
            set
            {
                Utils.OnlyOnce(myLogControlVM2, value);
                myLogControlVM2 = value;
                myLogControlVM2.PropertyChanged += SubVMPropertyChanged;
            }
        }
        private LogControlVM myLogControlVM2;

        /// <summary>
        /// 
        /// </summary>
        void SubVMPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MakeFilterEnabled")
            {
                IsFilterEnabled = true;
            }
            else if (e.PropertyName == "IsFilterEnabled")
            {
                HandleFilterChanged();
            }
            else if (e.PropertyName == "CurrentVM")
            {
                myCurrentVM = LogControlVM.CurrentVM;
                OnPropertyChanged("IsFilterEnabled");//--- to update the [E] button in the UI
            }
            else if (e.PropertyName == "RefreshAll")
            {
                OnPropertyChanged(e.PropertyName);
            }
        }
        LogControlVM myCurrentVM;

        #endregion Sub viewmodels

        #region Public Properties

        /// <summary>
        /// 
        /// </summary>
        public bool IsSplitLog
        {
            get { return isSplitLog; }
            set
            {
                if (isSplitLog != value)
                {
                    isSplitLog = value;
                    InitializeSubViewModel(2, value);
                    OnPropertyChanged();
                    if (value)
                        ApplySyncAndSplit();
                }
            }
        }
        private bool isSplitLog;

        /// <summary>
        /// 
        /// </summary>
        public bool IsSyncSelection
        {
            get { return isSyncSelection; }
            set
            {
                if (isSyncSelection != value)
                {
                    isSyncSelection = value;
                    OnPropertyChanged();
                    if (value)
                        ApplySyncAndSplit();
                }
            }
        }
        private bool isSyncSelection = true;

        /// <summary>
        /// 
        /// </summary>
        public string LastFile { get; set; }

        #endregion Public Properties

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public LogReader Reader
        {
            get { return reader; }
            set
            {
                log.Debug();
                Utils.OnlyOnce(reader, value);
                reader = value;
                reader.StatusChanged += ReaderStatusChanged;
                InitializeSubViewModel(1, true);
                InitializeSubViewModel(2, isSplitLog);
            }
        }
        LogReader reader;

        /// <summary>
        /// 
        /// </summary>
        private void InitializeSubViewModel(int vmID, bool mode)
        {
            log.Debug();
            LogControlVM subViewModel = vmID == 1 ? myLogControlVM1 : myLogControlVM2;
            if (mode)
            {
                if (subViewModel.Records != null)
                    InitializeSubViewModel(vmID, false);

                if (reader != null)
                {
                    subViewModel.Records = reader.Records;
                    subViewModel.RecordsView.CurrentChanged += RecordsViewCurrentChanged;
                }
            }
            else
            {
                if (subViewModel.Records != null)
                {
                    subViewModel.RecordsView.CurrentChanged -= RecordsViewCurrentChanged;
                    subViewModel.Records = null;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override string ToString()
        {
            return "SplitLogControlVM[" + DisplayName + "]";
        }

        /// <summary>
        /// 
        /// </summary>
        public bool LoadFile(string fileName)
        {
            log.Debug(fileName);
            if (!File.Exists(fileName))
            {
                HandleNoLastFile();
                return false;
            }

            LastFile = fileName;
            OnPropertyChanged("ShowWaitControl");

            if (reader.IsBusy)
                reader.Stop("LoadFile");
            else
                reader.LoadFile(LastFile);

            return true;
        }

        /// <summary>
        /// Stop the reader and wait for RunWorkerCompleted notification.
        /// </summary>
        public void ReloadFile()
        {
            if (!File.Exists(LastFile))
            {
                HandleNoLastFile();
                return;
            }

            LoadFile(LastFile);
        }

        /// <summary>
        /// 
        /// </summary>
        public void HandleNoLastFile()
        {
            LastFile = null;
            IsSplitLog = false;
            Reader.FileName = null;
            Reader.Records.Clear();
            HandleRecordsChanged();
            HandleReaderFileNameChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public string ReaderFileName
        {
            get { return reader.FileName ?? OpenCmd.Text; }
        }

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public string ReaderFileInfo
        {
            get
            {
                if (string.IsNullOrEmpty(DisplayName))
                    return "O";

                return DisplayName + " " + Utils.GetFileSizeString(reader.FileSize);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetGridLengths()
        {
            OnPropertyChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        void ApplySyncAndSplit()
        {
            log.Debug();
            if (!SmartLogControlVM.IsInitialized || myLogControlVM1.RecordsView == null)
                return;

            if (isSyncSelection)
            {
                //--- find the matching record in view2:
                SelectedRecord = myLogControlVM1.RecordsView.CurrentItem as Record;
                FindMatchingInternal(myLogControlVM2.RecordsView, false);

                //--- find the matching record in external views:
                OnPropertyChanged("FindMatchingExternal");
            }

            if (isSplitLog)
            {
                //--- now that the control is split, the current item might be out of view1:
                myLogControlVM1.RecordsView.Dispatch(() => myLogControlVM1.ScrollCurrentIntoView());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void ReaderStatusChanged(object sender, ReaderStatus status, string text)
        {
            switch (status)
            {
                case ReaderStatus.FileChanged:
                    HandleReaderFileNameChanged();
                    break;
                case ReaderStatus.ProgressChanged:
                    OnPropertyChanged("ShowProgress");
                    OnPropertyChanged("ReaderFileInfo");
                    break;
                case ReaderStatus.RecordsChanged:
                    HandleRecordsChanged();
                    break;
                case ReaderStatus.FinishedWork:
                    HandleFinishedWork(text);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void HandleFinishedWork(string reason)
        {
            if (reason == "LoadFile")
            {
                reader.LoadFile(LastFile);
            }
            else if (reason == "NoLastFile")
            {
                HandleNoLastFile();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void HandleReaderFileNameChanged()
        {
            DisplayName = Path.GetFileName(reader.FileName);
            myLogControlVM1.DisplayName = DisplayName + "-1";
            myLogControlVM2.DisplayName = DisplayName + "-2";
            OnPropertyChanged("ReaderFileName");
            OnPropertyChanged("ReaderFileInfo");
        }

        /// <summary>
        /// 
        /// </summary>
        public void HandleRecordsChanged(bool mode = true)
        {
            dontCare++;
            myLogControlVM1.Refresh();
            myLogControlVM2.Refresh();
            CheckScrolling();
            dontCare--;

            if (mode)
            {
                OnPropertyChanged("ReaderFileInfo");
                OnPropertyChanged("HideWaitControl");
            }

            if (!done)
            {
                done = true;
                myLogControlVM1.SetAsCurrentVM();
                myLogControlVM1.SetFocusOnSelected();
            }
        }
        bool done;
        int dontCare;

        /// <summary>
        /// 
        /// </summary>
        public void HandleFilterChanged()
        {
            LogControlVM vm = myCurrentVM;
            if (vm == null || vm.RecordsView == null)
                return;

            SelectedRecord = vm.RecordsView.CurrentItem as Record;
            HandleRecordsChanged(false);
            if (!FollowTail)
                FindMatchingInternal(vm.RecordsView, false);
        }

        /// <summary>
        /// 
        /// </summary>
        void RecordsViewCurrentChanged(object sender, EventArgs e)
        {
            ListCollectionView view = sender as ListCollectionView;
            if (view == null || dontCare != 0)
                return;

            int index = view.CurrentPosition;
            FollowTail = index < 0;
            //FollowTail = index < 0 || index >= view.Count - 1;

            if (!FollowTail)
            {
                SelectedRecord = view.CurrentItem as Record;
                if (IsSyncSelection)
                {
                    FindMatchingInternal(view, true);
                    OnPropertyChanged("FindMatchingExternal");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public Record SelectedRecord { get; protected set; }

        /// <summary>
        /// Looks for a record which matches SelectedRecord in the same view or the other view of the same viewmodel.
        /// </summary>
        void FindMatchingInternal(ListCollectionView view1, bool theOtherView)
        {
            ListCollectionView view2 = view1;
            if (theOtherView)
                view2 = view1 == MyLogControlVM1.RecordsView ? MyLogControlVM2.RecordsView : MyLogControlVM1.RecordsView;

            if (view2 == null || SelectedRecord == null)
                return;

            Record match = FindMatching(view2, x => x.RecordNum >= SelectedRecord.RecordNum);
            if (match != null)
                HighlightRecord(view2, match);
        }

        /// <summary>
        /// Looks for a record which matches extRecord from another viewmodel.
        /// </summary>
        public void FindMatchingExternal(Record extRecord)
        {
            if (extRecord == null || extRecord == SelectedRecord || !IsSyncSelection)
                return;

            FindMatchingExternal(myLogControlVM1.RecordsView, extRecord);
            FindMatchingExternal(myLogControlVM2.RecordsView, extRecord);
        }

        /// <summary>
        /// 
        /// </summary>
        private void FindMatchingExternal(ListCollectionView view, Record extRecord)
        {
            if (view != null)
            {
                Record match = FindMatching(view, x => x.UtcTime >= extRecord.UtcTime);
                if (match != null)
                    HighlightRecord(view, match);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private Record FindMatching(ListCollectionView view, Predicate<Record> predicate)
        {
            Record match = null;
            for (int i = 0; i < view.Count; i++)
            {
                Record record = view.GetItemAt(i) as Record;
                if (predicate.Invoke(record))
                {
                    match = record;
                    break;
                }
            }

            if (match == null && view.Count > 0)
                match = view.GetItemAt(view.Count - 1) as Record;

            return match;
        }

        /// <summary>
        /// 
        /// </summary>
        private void HighlightRecord(ListCollectionView view, Record record)
        {
            FollowTail = false;
            if (record == view.CurrentItem)//view.MoveCurrentTo() will not help to scroll current into view
            {
                LogControlVM vm = view == myLogControlVM1.RecordsView ? myLogControlVM1 : myLogControlVM2;
                vm.ScrollCurrentIntoView();
            }
            else
            {
                dontCare++;
                view.MoveCurrentTo(record);
                dontCare--;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public bool FollowTail
        {
            get { return followTail; }
            set
            {
                if (followTail != value)
                {
                    followTail = value;
                    OnPropertyChanged();
                    CheckScrolling();
                }
            }
        }
        bool followTail = true;

        /// <summary>
        /// 
        /// </summary>
        public void SyncFollowTail(SplitLogControlVM vm)
        {
            if (IsSyncSelection)
                FollowTail = vm.FollowTail;
        }

        /// <summary>
        /// 
        /// </summary>
        public void CheckScrolling()
        {
            if (FollowTail)
            {
                dontCare++;
                myLogControlVM1.ScrollToBottom();
                myLogControlVM2.ScrollToBottom();
                dontCare--;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public bool IsFilterEnabled
        {
            get
            {
                if (myCurrentVM == null)
                    myCurrentVM = myLogControlVM1;

                return myCurrentVM.IsFilterEnabled;
            }
            set
            {
                if (myCurrentVM.IsFilterEnabled != value)
                {
                    myCurrentVM.IsFilterEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        #region Commands

        void InitCommands()
        {
            CommandBindings.Add(new CommandBinding(OpenCmd, ExecuteOpenCmd, CanExecuteOpenCmd));
            CommandBindings.Add(new CommandBinding(CloseCmd, ExecuteCloseCmd, CanExecuteCloseCmd));
            CommandBindings.Add(new CommandBinding(SplitCmd, ExecuteSplitCmd, CanExecuteSplitCmd));
            CommandBindings.Add(new CommandBinding(ConfigureCmd, ExecuteConfigureCmd, CanExecuteConfigureCmd));
            CommandBindings.Add(new CommandBinding(TimeReferenceCmd, ExecuteTimeReferenceCmd, CanExecuteTimeReferenceCmd));
        }

        /// <summary>
        /// OpenCmd
        /// </summary>
        void CanExecuteOpenCmd(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void ExecuteOpenCmd(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "Log Files|*.log;*.csv|All Files|*.*", Title = "Select a file" };
            if (File.Exists(LastFile))
                dlg.InitialDirectory = Path.GetDirectoryName(LastFile);

            if (dlg.ShowDialog() == true)
                LoadFile(dlg.FileName);
        }

        /// <summary>
        /// CloseCmd
        /// </summary>
        void CanExecuteCloseCmd(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void ExecuteCloseCmd(object sender, ExecutedRoutedEventArgs e)
        {
            if (reader.IsBusy)
                reader.Stop("NoLastFile");
            else
                HandleNoLastFile();

            OnPropertyChanged("NoLastFile");
        }

        /// <summary>
        /// SplitCmd
        /// </summary>
        void CanExecuteSplitCmd(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void ExecuteSplitCmd(object sender, ExecutedRoutedEventArgs e)
        {
            IsSplitLog ^= true;
        }

        /// <summary>
        /// ConfigureCmd
        /// </summary>
        void CanExecuteConfigureCmd(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void ExecuteConfigureCmd(object sender, ExecutedRoutedEventArgs e)
        {
            LogControlVM clone = myCurrentVM.Clone();
            FilterDialog dlg = new FilterDialog(clone);

            if (dlg.ShowDialog("Configure filter"))
            {
                myCurrentVM.ReadFilterSettings(clone);
                ////needs to be dispatched:
                ////myCurrentVM.CheckIsFilterEnabled();
                myCurrentVM.SetFocusOnSelected();
            }
        }

        /// <summary>
        /// TimeReferenceCmd
        /// </summary>
        void CanExecuteTimeReferenceCmd(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void ExecuteTimeReferenceCmd(object sender, ExecutedRoutedEventArgs e)
        {
            var record = LogControlVM.CurrentVM?.RecordsView?.CurrentItem as Record;
            if (record != null)
            {
                Record.UtcTime0 = record.UtcTime;
                //UpdateUI();
                OnPropertyChanged("RefreshAll");
            }
        }

        #endregion Commands
    }
}
