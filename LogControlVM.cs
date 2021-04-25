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
using System.Windows.Data;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Windows;

namespace SmartLogReader
{
    /// <summary>
    /// 
    /// </summary>
    public class LogControlVM : ViewModel
    {
        /// <summary>
        /// 
        /// </summary>
        public LogControlVM()
        {
            log = new SimpleLogger($"{GetType().Name}.{++instanceCounter}");
            IncludeList = new FilterCollection();
            ExcludeList = new FilterCollection();
        }
        private readonly SimpleLogger log;
        private static int instanceCounter;

        /// <summary>
        /// 
        /// </summary>
        public override string ToString()
        {
            return "LogControlVM[" + DisplayName + "]";
        }

        /// <summary>
        /// 
        /// </summary>
        public LogControlVM Clone()
        {
            log.Debug();
            string xml = Utils.ToXML(this);
            LogControlVM vm = Utils.FromXML<LogControlVM>(xml);
            return vm;
        }

        /// <summary>
        /// There is only one active (or current) viewmodel.
        /// </summary>
        public static LogControlVM CurrentVM { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public void SetAsCurrentVM()
        {
            log.Debug();
            if (CurrentVM != this)
            {
                CurrentVM = this;
                OnPropertyChanged("CurrentVM");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public RecordCollection Records
        {
            get { return records; }
            set
            {
                if (records != null)
                {
                    RecordsView.Filter = null;
                    RecordsView = null;
                }

                records = value;

                if (records != null)
                {
                    RecordsView = new ListCollectionView(records);
                    RecordsView.Filter = Test;
                }

                CheckListEmptyReason();
                OnPropertyChanged("RecordsView");
            }
        }
        RecordCollection records;

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public ListCollectionView RecordsView { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public string ListEmptyReason { get; set; }

        /// <summary>
        /// 
        /// </summary>
        void CheckListEmptyReason()
        {
            string reason = "";

            if (Records == null)
                reason = "Split is disabled";

            else if (Records.Count == 0)
                reason = "No records";

            else if (RecordsView.Count == 0 && Records.Count > 0)
                reason = "No records passing filter";

            if (ListEmptyReason != reason)
            {
                ListEmptyReason = reason;
                OnPropertyChanged("ListEmptyReason");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Refresh()
        {
            if (RecordsView != null)
            {
                try
                {
                    RecordsView.Refresh();
                }
                catch (Exception e)
                {
                    log.Exception(e);
                }
            }

            CheckListEmptyReason();
        }

        /// <summary>
        /// 
        /// </summary>
        public void ScrollToBottom()
        {
            if (RecordsView != null)
            {
                try
                {
                    RecordsView.MoveCurrentToLast();
                }
                catch (Exception e)
                {
                    log.Exception(e);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void ScrollCurrentIntoView()
        {
            OnPropertyChanged("ScrollSelectedIntoView");
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetFocusOnSelected()
        {
            OnPropertyChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        public void RefreshAll()
        {
            OnPropertyChanged("RefreshAll");
        }

        #region Filtering

        /// <summary>
        /// 
        /// </summary>
        public bool IsFilterEnabled
        {
            get { return isFilterEnabled; }
            set
            {
                if (isFilterEnabled != value)
                {
                    isFilterEnabled = value;
                    OnPropertyChanged();
                }
            }
        }
        bool isFilterEnabled = true;

        public void CheckIsFilterEnabled()
        {
            if (!IsFilterEnabled)
            {
                var dlg = new MyMessageBox("Currently filtering is not enabled.\r\nClick OK to enable it.");
                if (dlg.ShowDialog("Filtering disabled"))
                {
                    OnPropertyChanged("MakeFilterEnabled");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void NotifyFilterChanged()
        {
            //--- Tell the SplitLogControlVM that IsFilterEnabled has been changed. This will trigger the correct action.
            OnPropertyChanged("IsFilterEnabled");
        }

        /// <summary>
        /// 
        /// </summary>
        public FilterCollection IncludeList { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public FilterCollection ExcludeList { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public void ReadFilterSettings(LogControlVM vm)
        {
            if (vm != null)
            {
                IncludeList = new FilterCollection(vm.IncludeList);
                ExcludeList = new FilterCollection(vm.ExcludeList);
                NotifyFilterChanged();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Test(object obj)
        {
            Record record = obj as Record;

            if (!IsFilterEnabled || record == null)
                return true;

            if (IncludeList.Count != 0)
            {
                bool result = false;

                IEnumerator<Filter> e = IncludeList.GetEnumerator();
                while (e.MoveNext())
                {
                    if (TestAllAnds(e, record))
                    {
                        result = true;
                        break;
                    }
                }

                if (!result)
                    return false;
            }

            if (ExcludeList.Count != 0)
            {
                IEnumerator<Filter> e = ExcludeList.GetEnumerator();
                while (e.MoveNext())
                {
                    if (TestAllAnds(e, record))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        bool TestAllAnds(IEnumerator<Filter> e, Record record)
        {
            Filter filter = e.Current;
            bool result = filter.Test(record);

            while (filter.AndNext)
            {
                if (!e.MoveNext())
                    return result;

                filter = e.Current;
                result &= filter.Test(record);
            }

            return result;
        }

        #endregion Filtering

        #region Searching

        [XmlIgnore]
        public string SearchText { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public void Search(string text, bool forward)
        {
            if (string.IsNullOrEmpty(text) || RecordsView == null || RecordsView.Count == 0)
                return;

            int index = -1;
            SearchText = text;

            if (forward)
            {
                int start = Math.Max(RecordsView.CurrentPosition + 1, 0);
                index = SearchBetween(start, RecordsView.Count);
                if (index < 0)
                    index = SearchBetween(0, start);
            }
            else
            {
                int start = Math.Max(RecordsView.CurrentPosition - 1, 0);
                index = SearchBetween(start, -1);
                if (index < 0)
                    index = SearchBetween(RecordsView.Count - 1, start);
            }

            if (index < 0)
            {
                OnPropertyChanged("StringNotFound");
            }
            else
            {
                RecordsView.MoveCurrentToPosition(index);
                SetFocusOnSelected();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        int SearchBetween(int start, int stop)
        {
            if (start < stop)
            {
                for (int i = start; i < stop; i++)
                {
                    Record record = RecordsView.GetItemAt(i) as Record;
                    if (record.Contains(SearchText))
                        return i;
                }
            }
            else
            {
                for (int i = start; i > stop; i--)
                {
                    Record record = RecordsView.GetItemAt(i) as Record;
                    if (record.Contains(SearchText))
                        return i;
                }
            }
            return -1;
        }

        #endregion Searching
    }
}
