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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;

namespace SmartLogReader
{
    /// <summary>
    /// The main viewmodel for a SmartLogControl.
    /// </summary>
    public class SmartLogControlVM : SplitGridViewModel3
    {
        private static readonly SmartLogger log = new SmartLogger();

        /// <summary>
        /// Fill some static lists.
        /// </summary>
        static SmartLogControlVM()
        {
            InitLists();
        }

        /// <summary>
        /// 
        /// </summary>
        public SmartLogControlVM()
        {
            GridLength0 = 0;
            GridLength2 = 1;
            GridLength4 = 0;
            InitCommands();
            SearchText = "Search for me";
        }

        /// <summary>
        /// A flag indicating that the main view model is initialized.
        /// </summary>
        public static bool IsInitialized { get; protected set; }

        /// <summary>
        /// Deserialize from XML.
        /// </summary>
        public static SmartLogControlVM FromWorkspace(string path)
        {
            if (!File.Exists(path))
                path = GetWorkspaceFile(DefaultWorkspace);

            string xml = File.Exists(path) ? File.ReadAllText(path) : null;
            log.Debug($"Create viewmodel from the following settings: {xml}");

            //--- There are property setters which call ReloadFiles() during XML deserialization. 
            //--- To prevent this, we use this static boolean value:
            IsInitialized = false;
            SmartLogControlVM viewModel = Utils.FromXML<SmartLogControlVM>(xml);

            if (viewModel == null)
            {
                //--- Do not create subVMs in the ctors or getters!
                viewModel = new SmartLogControlVM();
                viewModel.MyClientControlVM = NewSplitLogControlVM();
                viewModel.MyServerControlVM = NewSplitLogControlVM();
                viewModel.MyAdditionalControlVM = NewSplitLogControlVM();
            }

            if (viewModel.ColorSpecs == null)
                viewModel.ColorSpecs = Record.GetDefaultColorSpecs();

            viewModel.SelectWorkspace(Path.GetFileNameWithoutExtension(path));

            IsInitialized = true;
            return viewModel;
        }
        private static string DefaultWorkspace = "Default workspace";

        /// <summary>
        /// Create a new sub viewmodel with subsub viewmodels.
        /// </summary>
        static SplitLogControlVM NewSplitLogControlVM()
        {
            //--- Do not create subsubVMs in the ctors or getters!
            SplitLogControlVM vm = new SplitLogControlVM();
            vm.MyLogControlVM1 = new LogControlVM();
            vm.MyLogControlVM2 = new LogControlVM();
            return vm;
        }

        /// <summary>
        /// Serialize to XML.
        /// </summary>
        public string ToXML()
        {
            GetGridLengths();
            myClientControlVM.GetGridLengths();
            myServerControlVM.GetGridLengths();
            myAdditionalControlVM.GetGridLengths();
            return Utils.ToXML(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public override string ToString()
        {
            return "SmartLogControlVM[" + DisplayName + "]";
        }

        /// <summary>
        /// 
        /// </summary>
        static void InitLists()
        {
            Fonts = new List<string>();
            Fonts.Add("Consolas");
            Fonts.Add("Courier New");
            Fonts.Add("Lucida Console");

            ReadModes = new List<string>();
            ReadModes.Add("All records");
            ReadModes.Add("Last session");
            ReadModes.Add("Last 24 hours");
            ReadModes.Add("Last 8 hours");
            ReadModes.Add("Last hour");

            LogLevels = new List<string>();
            LogLevels.Add("Verbose");
            LogLevels.Add("Debug");
            LogLevels.Add("Information");
            LogLevels.Add("Warning");
            LogLevels.Add("Error");
            LogLevels.Add("Fatal");
            log.Debug(LogLevels);

            RecordDetails = new List<string>();
            RecordDetails.Add("Time");
            RecordDetails.Add("Logger");
            RecordDetails.Add("Level");
            RecordDetails.Add("ThreadIds");
            RecordDetails.Add("Method");

            InitWorkspaces();
        }
        static public List<string> Fonts { get; set; }
        static public List<string> ReadModes { get; set; }
        static public List<string> LogLevels { get; set; }
        static public List<string> RecordDetails { get; set; }

        #region Sub viewmodels

        /// <summary>
        /// 
        /// </summary>
        public SplitLogControlVM MyClientControlVM
        {
            get { return myClientControlVM; }
            set
            {
                Utils.OnlyOnce(myClientControlVM, value);
                myClientControlVM = value;
                myClientControlVM.Reader = new LogReader();
                myClientControlVM.PropertyChanged += SubVMPropertyChanged;
            }
        }
        SplitLogControlVM myClientControlVM;

        /// <summary>
        /// 
        /// </summary>
        public SplitLogControlVM MyServerControlVM
        {
            get { return myServerControlVM; }
            set
            {
                Utils.OnlyOnce(myServerControlVM, value);
                myServerControlVM = value;
                myServerControlVM.Reader = new LogReader();
                myServerControlVM.PropertyChanged += SubVMPropertyChanged;
            }
        }
        SplitLogControlVM myServerControlVM;

        /// <summary>
        /// 
        /// </summary>
        public SplitLogControlVM MyAdditionalControlVM
        {
            get { return myAdditionalControlVM; }
            set
            {
                Utils.OnlyOnce(myAdditionalControlVM, value);
                myAdditionalControlVM = value;
                myAdditionalControlVM.Reader = new LogReader();
                myAdditionalControlVM.PropertyChanged += SubVMPropertyChanged;
            }
        }
        SplitLogControlVM myAdditionalControlVM;

        /// <summary>
        /// 
        /// </summary>
        void SubVMPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SplitLogControlVM vm = sender as SplitLogControlVM;

            if (e.PropertyName == "FindMatchingExternal")
            {
                myClientControlVM.FindMatchingExternal(vm.SelectedRecord);
                myServerControlVM.FindMatchingExternal(vm.SelectedRecord);
                myAdditionalControlVM.FindMatchingExternal(vm.SelectedRecord);
            }
            else if (e.PropertyName == "FollowTail")
            {
                if (vm != myAdditionalControlVM || vm.IsSyncSelection)
                {
                    myClientControlVM.SyncFollowTail(vm);
                    myServerControlVM.SyncFollowTail(vm);
                    myAdditionalControlVM.SyncFollowTail(vm);
                }
            }
            else if (e.PropertyName == "NoLastFile")
            {
                ColumnIndex = vm == myClientControlVM ? 0 : vm == myServerControlVM ? 2 : 4;
                OnPropertyChanged(e.PropertyName);
            }
            else if (e.PropertyName == "RefreshAll")
            {
                UpdateUI();
            }
        }

        [XmlIgnore]
        internal int ColumnIndex { get; private set; }

        #endregion Sub viewmodels

        #region Public Properties

        /// <summary>
        /// 
        /// </summary>
        public int SelectedFont
        {
            get { return selectedFont; }
            set
            {
                if (selectedFont != value)
                {
                    selectedFont = value;
                    OnPropertyChanged();
                    SelectedFamily = new FontFamily(Fonts[value]);
                }
            }
        }
        int selectedFont = 0;

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public FontFamily SelectedFamily
        {
            get { return selectedFamily; }
            set
            {
                if (selectedFamily != value)
                {
                    selectedFamily = value;
                    OnPropertyChanged();
                }
            }
        }
        FontFamily selectedFamily = new FontFamily(Fonts[0]);

        /// <summary>
        /// 
        /// </summary>
        public int SelectedSize
        {
            get { return selectedSize; }
            set
            {
                if (selectedSize != value)
                {
                    FontSizeHack = selectedSize = value;
                    OnPropertyChanged();
                }
            }
        }
        int selectedSize = 12;

        public static int FontSizeHack = 12;

        /// <summary>
        /// 
        /// </summary>
        public int SelectedLogLevel
        {
            get { return (int)LogReader.Level; }
            set
            {
                if (SelectedLogLevel != value)
                {
                    LogReader.Level = (LogLevel)value;
                    OnPropertyChanged();
                    ReloadFiles();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int SelectedReadMode
        {
            get { return (int)LogReader.ReadMode; }
            set
            {
                if (SelectedReadMode != value)
                {
                    LogReader.ReadMode = (LogReadMode)value;
                    OnPropertyChanged();
                    ReloadFiles();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string SearchText { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ColorSpecCollection ColorSpecs
        {
            get { return Record.ColorSpecs; }
            set
            {
                if (Record.ColorSpecs != value)
                {
                    Record.ColorSpecs = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Size DetailsWindowSize
        {
            get { return DetailsWindow.LastSize; }
            set { DetailsWindow.LastSize = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool DetailsWindowWrap
        {
            get { return DetailsWindow.DoWrap; }
            set { DetailsWindow.DoWrap = value; }
        }

        /// <summary>
        ///
        /// </summary>
        public bool ShowTime
        {
            get { return Record.ShowTime; }
            set { Record.ShowTime = value; UpdateUI(); }
        }

        /// <summary>
        ///
        /// </summary>
        public bool ShowTimeDiff
        {
            get { return Record.ShowTimeDiff; }
            set { Record.ShowTimeDiff = value; UpdateUI(); }
        }

        /// <summary>
        ///
        /// </summary>
        public bool ShowLogger
        {
            get { return Record.ShowLogger; }
            set { Record.ShowLogger = value; UpdateUI(); }
        }

        /// <summary>
        ///
        /// </summary>
        public bool ShowLevel
        {
            get { return Record.ShowLevel; }
            set { Record.ShowLevel = value; UpdateUI(); }
        }

        /// <summary>
        ///
        /// </summary>
        public bool ShowThreadIds
        {
            get { return Record.ShowThreadIds; }
            set { Record.ShowThreadIds = value; UpdateUI(); }
        }

        /// <summary>
        ///
        /// </summary>
        public bool ShowMethod
        {
            get { return Record.ShowMethod; }
            set { Record.ShowMethod = value; UpdateUI(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public int AmountOfTime
        {
            get { return Record.AmountOfTime; }
            set { Record.AmountOfTime = value; UpdateUI(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public int AmountOfTimeDiff
        {
            get { return Record.AmountOfTimeDiff; }
            set { Record.AmountOfTimeDiff = value; UpdateUI(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public int AmountOfLogger
        {
            get { return Record.AmountOfLogger; }
            set { Record.AmountOfLogger = value; UpdateUI(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public int AmountOfLevel
        {
            get { return Record.AmountOfLevel; }
            set { Record.AmountOfLevel = value; UpdateUI(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public int AmountOfThreadIds
        {
            get { return Record.AmountOfThreadIds; }
            set { Record.AmountOfThreadIds = value; UpdateUI(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public int AmountOfMethod
        {
            get { return Record.AmountOfMethod; }
            set { Record.AmountOfMethod = value; UpdateUI(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public Size FilterDialogSize
        {
            get { return FilterDialog.LastSize; }
            set { FilterDialog.LastSize = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Size HighLightingDialogSize
        {
            get { return HighLightingDialog.LastSize; }
            set { HighLightingDialog.LastSize = value; }
        }

        #endregion Public Properties

        /// <summary>
        /// 
        /// </summary>
        void UpdateUI()
        {
            if (IsInitialized)
            {
                myClientControlVM.HandleRecordsChanged();
                myServerControlVM.HandleRecordsChanged();
                myAdditionalControlVM.HandleRecordsChanged();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Shutdown()
        {
            string reason = "Shutdown";
            myClientControlVM.Reader.Stop(reason);
            myServerControlVM.Reader.Stop(reason);
            myAdditionalControlVM.Reader.Stop(reason);
            return SaveWorkspace();
        }

        /// <summary>
        /// 
        /// </summary>
        public void LoadFiles()
        {
            myClientControlVM.LoadFile(myClientControlVM.LastFile);
            myServerControlVM.LoadFile(myServerControlVM.LastFile);
            myAdditionalControlVM.LoadFile(myAdditionalControlVM.LastFile);
        }

        /// <summary>
        /// 
        /// </summary>
        void ReloadFiles()
        {
            if (!IsInitialized)
                return;

            myClientControlVM.ReloadFile();
            myServerControlVM.ReloadFile();
            myAdditionalControlVM.ReloadFile();
        }

        /// <summary>
        /// 
        /// </summary>
        public void LoadFileFromCommandLine(string path)
        {
            var workspace = Path.GetFileNameWithoutExtension(path);
            if (SelectWorkspace(workspace))
                return;

            GridLength2 = 1;
            GridLength0 = GridLength4 = 0;
            OnPropertyChanged("SetGridLengths");

            myServerControlVM.LoadFile(path);
        }

        #region Commands

        void InitCommands()
        {
            CommandBindings.Add(new CommandBinding(CopyCmd, ExecuteCopyCmd, CanExecuteCopyCmd));
            CommandBindings.Add(new CommandBinding(FindCmd, ExecuteFindCmd, CanExecuteFindCmd));
            CommandBindings.Add(new CommandBinding(SearchUpCmd, ExecuteSearchUpCmd, CanExecuteSearchUpCmd));
            CommandBindings.Add(new CommandBinding(SearchDownCmd, ExecuteSearchDownCmd, CanExecuteSearchDownCmd));
            CommandBindings.Add(new CommandBinding(HighlightingCmd, ExecuteHighlightingCmd, CanExecuteHighlightingCmd));
            CommandBindings.Add(new CommandBinding(NewWorkspaceCmd, ExecuteNewWorkspaceCmd, CanExecuteNewWorkspaceCmd));
            CommandBindings.Add(new CommandBinding(DeleteWorkspaceCmd, ExecuteDeleteWorkspaceCmd, CanExecuteDeleteWorkspaceCmd));
        }

        /// <summary>
        /// FindCmd
        /// </summary>
        void CanExecuteFindCmd(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void ExecuteFindCmd(object sender, ExecutedRoutedEventArgs e)
        {
            OnPropertyChanged("SetFocusOnSearchBox");
        }

        /// <summary>
        /// SearchUpCmd. Cannot be moved to LogControlVM (although it would be nice) because
        /// the UIElements (search up and down buttons) will not find the command in the visual tree. 
        /// That's because the buttons are part of the SearchControl which is part of the SmartLogControl 
        /// and the LogControls are deep inside other branches of the main control.
        /// </summary>
        void CanExecuteSearchUpCmd(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(SearchText);
        }

        void ExecuteSearchUpCmd(object sender, ExecutedRoutedEventArgs e)
        {
            LogControlVM.CurrentVM.Search(SearchText, false);
        }

        /// <summary>
        /// SearchDownCmd
        /// </summary>
        void CanExecuteSearchDownCmd(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(SearchText);
        }

        void ExecuteSearchDownCmd(object sender, ExecutedRoutedEventArgs e)
        {
            LogControlVM.CurrentVM.Search(SearchText, true);
        }

        /// <summary>
        /// CopyCmd
        /// </summary>
        void CanExecuteCopyCmd(object sender, CanExecuteRoutedEventArgs e)
        {
            bool cannotExecute = LogControlVM.CurrentVM.RecordsView == null || LogControlVM.CurrentVM.RecordsView.CurrentItem == null;
            e.CanExecute = !cannotExecute;
        }

        void ExecuteCopyCmd(object sender, ExecutedRoutedEventArgs e)
        {
            Record record = LogControlVM.CurrentVM.RecordsView.CurrentItem as Record;
            Clipboard.SetText(record.LongString);
        }

        /// <summary>
        /// HighlightingCmd
        /// </summary>
        void CanExecuteHighlightingCmd(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void ExecuteHighlightingCmd(object sender, ExecutedRoutedEventArgs e)
        {
            ColorSpecCollection colorSpecs = new ColorSpecCollection(ColorSpecs);
            HighLightingDialog dlg = new HighLightingDialog(colorSpecs);
            Utils.MoveToMouse(dlg, HighlightingCmd.Text);

            if (dlg.ShowDialog() == true)
            {
                ColorSpecs = new ColorSpecCollection(colorSpecs);
                myClientControlVM.HandleRecordsChanged();
                myServerControlVM.HandleRecordsChanged();
                myAdditionalControlVM.HandleRecordsChanged();
            }
        }

        #endregion Commands

        #region Workspaces

        /// <summary>
        /// 
        /// </summary>
        public static ObservableCollection<string> Workspaces
        {
            get { return workspaces; }
        }
        private static ObservableCollection<string> workspaces = new ObservableCollection<string>();

        /// <summary>
        /// 
        /// </summary>
        private static string GetWorkspaceFile(string workspace)
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartLogReader");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return Path.Combine(dir, workspace + ".xml");
        }

        /// <summary>
        /// 
        /// </summary>
        private static void InitWorkspaces()
        {
            workspaces.Add(DefaultWorkspace);

            var path = GetWorkspaceFile(DefaultWorkspace);
            var dir = Path.GetDirectoryName(path);
            string[] files = Directory.GetFiles(dir, "*.xml");
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!name.equals(DefaultWorkspace))
                    workspaces.Add(name);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private bool SelectWorkspace(string value)
        {
            // if the workspace exist, just select it
            for (int i = 0; i < workspaces.Count; i++)
            {
                if (workspaces[i].equals(value))
                {
                    SelectedWorkspace = i;
                    return true;
                }
            }

            AddAndSelectNewWorkspace(value);
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
		[XmlIgnore]
        public int SelectedWorkspace
        {
            get { return selectedWorkspace; }
            set
            {
                log.Debug($"old = {selectedWorkspace}, new = {value}");
                if (selectedWorkspace != value)
                {
                    if (IsInitialized)
                        SaveWorkspace();

                    selectedWorkspace = value;
                    OnPropertyChanged();

                    if (IsInitialized && value > -1)
                        LoadWorkspace();
                }
            }
        }
        int selectedWorkspace = 0;

        /// <summary>
        /// 
        /// </summary>
        private void AddAndSelectNewWorkspace(string value)
        {
            // find the insertion index
            int i = 1;
            for (; i < workspaces.Count; i++)
            {
                if (workspaces[i].CompareTo(value) > 0)
                    break;
            }

            SelectedWorkspace = -1;
            workspaces.Insert(i, value);
            IsInitialized = false;
            SelectedWorkspace = i;
            IsInitialized = true;
        }

        /// <summary>
        /// 
        /// </summary>
        private bool IsNewWorkspace(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            foreach (var workspace in workspaces)
            {
                if (workspace.equals(value))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        void CanExecuteNewWorkspaceCmd(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void ExecuteNewWorkspaceCmd(object sender, ExecutedRoutedEventArgs e)
        {
            NewWorkspaceDialog dlg = new NewWorkspaceDialog();
            Utils.MoveToMouse(dlg, "Workspace Name:");

            if (dlg.ShowDialog() == true)
            {
                var value = dlg.Workspace;
                if (string.IsNullOrWhiteSpace(value))
                    return;

                if (IsNewWorkspace(value))
                    AddAndSelectNewWorkspace(value);
                else
                    SelectWorkspace(value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void CanExecuteDeleteWorkspaceCmd(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = selectedWorkspace > 0;
        }

        void ExecuteDeleteWorkspaceCmd(object sender, ExecutedRoutedEventArgs e)
        {
            DeleteWorkspace();
        }

        /// <summary>
        /// 
        /// </summary>
        private string GetWorkspaceFile()
        {
            if (selectedWorkspace < 0 || selectedWorkspace >= workspaces.Count)
                return null;

            return GetWorkspaceFile(workspaces[selectedWorkspace]);
        }

        /// <summary>
        /// 
        /// </summary>
        private string SaveWorkspace()
        {
            string path = GetWorkspaceFile();
            log.Debug($"path = '{path}'");
            if (path != null)
                File.WriteAllText(path, ToXML());
            return path;
        }

        /// <summary>
        /// 
        /// </summary>
        private void DeleteWorkspace()
        {
            string path = GetWorkspaceFile();
            log.Debug($"path = '{path}'");
            if (path != null)
            {
                var msg = $"Do you really want to delete workspace {workspaces[selectedWorkspace]}?";
                var res = MessageBox.Show(msg, "Delete Workspace", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    Utils.DeleteFile(path);
                    workspaces.RemoveAt(selectedWorkspace);
                    SelectedWorkspace = 0;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadWorkspace()
        {
            string path = GetWorkspaceFile();
            log.Debug($"path = '{path}'");
            if (!File.Exists(path))
            {
                log.Debug("file does not exist");
                return;
            }

            string xml = File.ReadAllText(path);

            var quickHack = SelectedReadMode;
            IsInitialized = false;
            ColorSpecs.Clear();
            var vm = Utils.FromXML<SmartLogControlVM>(xml);
            SelectedReadMode = quickHack;

            if (vm == null)
            {
                log.Warning("vm == null");
                IsInitialized = true;
                return;
            }

            CopyValues(vm.MyClientControlVM, MyClientControlVM, true);
            CopyValues(vm.MyServerControlVM, MyServerControlVM, true);
            CopyValues(vm.MyAdditionalControlVM, MyAdditionalControlVM, true);
            CopyValues(vm);
            IsInitialized = true;
            ReloadFiles();
        }

        private void CopyValues(SplitLogControlVM source, SplitLogControlVM target, bool setGridLengths = false)
        {
            target.GridLength0 = source.GridLength0;
            target.GridLength2 = source.GridLength2;
            target.LastFile = source.LastFile;
            target.IsSplitLog = source.IsSplitLog;
            target.IsSyncSelection = source.IsSyncSelection;
            CopyValues(source.MyLogControlVM1, target.MyLogControlVM1);
            CopyValues(source.MyLogControlVM2, target.MyLogControlVM2);
            if (setGridLengths)
                target.SetGridLengths();
        }

        private void CopyValues(LogControlVM source, LogControlVM target)
        {
            target.IsFilterEnabled = source.IsFilterEnabled;
            target.IncludeList = new FilterCollection(source.IncludeList);
            target.ExcludeList = new FilterCollection(source.ExcludeList);
        }

        void CopyValues(SmartLogControlVM vm)
        {
            GridLength0 = vm.GridLength0;
            GridLength2 = vm.GridLength2;
            GridLength4 = vm.GridLength4;
            OnPropertyChanged("SetGridLengths");

            string[] props = new string[] { "Time", "Logger", "Level", "ThreadIds", "Method" };
            foreach (var prop in props)
            {
                OnPropertyChanged("Show" + prop);
                OnPropertyChanged("AmountOf" + prop);
            }
        }

#endregion Workspaces
    }
}
