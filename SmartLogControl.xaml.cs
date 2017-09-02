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
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SmartLogging;

namespace SmartLogReader
{
    /// <summary>
    /// Interaction logic for SmartLogControl.xaml
    /// </summary>
    public partial class SmartLogControl : UserControl
    {
        private static readonly SmartLogger log = new SmartLogger();

        /// <summary>
        /// 
        /// </summary>
        public SmartLogControl()
        {
            InitializeComponent();
            AllowDrop = true;
            Drop += MeDrop;
        }

        /// <summary>
        /// 
        /// </summary>
        public SmartLogControlVM ViewModel
        {
            get { return viewModel; }
            set
            {
                Utils.OnlyOnce(viewModel, value);
                DataContext = viewModel = value;
                viewModel.PropertyChanged += ViewModelPropertyChanged;
                CommandBindings.AddRange(viewModel.CommandBindings);

                myClientControl.ViewModel = viewModel.MyClientControlVM;
                myServerControl.ViewModel = viewModel.MyServerControlVM;
                myAdditionalControl.ViewModel = viewModel.MyAdditionalControlVM;
            }
        }
        private SmartLogControlVM viewModel;

        /// <summary>
        /// 
        /// </summary>
        void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            log.Smart(e.PropertyName);
            if (e.PropertyName == "SelectedRecordDetails")
            {
                viewModel.MyClientControlVM.HandleRecordsChanged(false);
                viewModel.MyServerControlVM.HandleRecordsChanged(false);
                viewModel.MyAdditionalControlVM.HandleRecordsChanged(false);
            }
            //--- need ColumnDefinitions for the rest of this method
            else if (splitGrid.ColumnDefinitions.Count > 4)
            {
                if (e.PropertyName == "GridLengthsRequired")
                {
                    GetGridLengths();
                }
                else if (e.PropertyName == "NoLastFile")
                {
                    ApplyNoLastFile();
                }
                else if (e.PropertyName == "ApplyGridLengths")
                {
                    ApplyGridLengths();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void GetGridLengths()
        {
            viewModel.GridLength0 = splitGrid.ColumnDefinitions[0].Width.Value;
            viewModel.GridLength2 = splitGrid.ColumnDefinitions[2].Width.Value;
            viewModel.GridLength4 = splitGrid.ColumnDefinitions[4].Width.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        private void ApplyGridLengths()
        {
            splitGrid.ColumnDefinitions[0].Width = new GridLength(viewModel.GridLength0, GridUnitType.Star);
            splitGrid.ColumnDefinitions[2].Width = new GridLength(viewModel.GridLength2, GridUnitType.Star);
            splitGrid.ColumnDefinitions[4].Width = new GridLength(viewModel.GridLength4, GridUnitType.Star);
        }

        /// <summary>
        /// 
        /// </summary>
        void ApplyNoLastFile()
        {
            int index = viewModel.ComingFromAdditionalVM ? 4 : 2;
            splitGrid.ColumnDefinitions[index].Width = new GridLength(0, GridUnitType.Star);
        }

        /// <summary>
        /// 
        /// </summary>
        public void EnsureMyAdditionalControlIsVisible()
        {
            GetGridLengths();
            if (viewModel.GridLength4 != 0)
                return;

            splitGrid.ColumnDefinitions[0].Width = new GridLength(viewModel.GridLength0 * 2, GridUnitType.Star);
            splitGrid.ColumnDefinitions[2].Width = new GridLength(viewModel.GridLength2 * 2, GridUnitType.Star);
            splitGrid.ColumnDefinitions[4].Width = new GridLength(viewModel.GridLength0 + viewModel.GridLength2, GridUnitType.Star);
        }

        /// <summary>
        /// Drop handler.
        /// </summary>
        void MeDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var fileName in fileNames)
                {
                    log.Smart(fileName);
                    string ext = Path.GetExtension(fileName);
                    if (ext.equals(".log"))
                    {
                        if (File.Exists(fileName))
                        {
                            EnsureMyAdditionalControlIsVisible();
                            viewModel.MyAdditionalControlVM.LoadFile(fileName);
                            break;
                        }
                    }
                }
            }
        }
    }
}
