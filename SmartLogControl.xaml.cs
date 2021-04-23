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
using System.Windows;
using System.Windows.Controls;

namespace SmartLogReader
{
    /// <summary>
    /// Interaction logic for SmartLogControl.xaml
    /// </summary>
    public partial class SmartLogControl : UserControl
    {
        private static readonly SimpleLogger log = new SimpleLogger();

        /// <summary>
        /// 
        /// </summary>
        public SmartLogControl()
        {
            InitializeComponent();
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
            log.Debug(e.PropertyName);
            if (splitGrid.ColumnDefinitions.Count > 4)
            {
                if (e.PropertyName == "GetGridLengths")
                {
                    GetGridLengths();
                }
                else if (e.PropertyName == "SetGridLengths")
                {
                    SetGridLengths();
                }
                else if (e.PropertyName == "NoLastFile")
                {
                    ApplyNoLastFile();
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
        private void SetGridLengths()
        {
            splitGrid.ColumnDefinitions[0].Width = new GridLength(viewModel.GridLength0, GridUnitType.Star);
            splitGrid.ColumnDefinitions[2].Width = new GridLength(viewModel.GridLength2, GridUnitType.Star);
            splitGrid.ColumnDefinitions[4].Width = new GridLength(viewModel.GridLength4, GridUnitType.Star);
        }

        /// <summary>
        /// 
        /// </summary>
        private void ApplyNoLastFile()
        {
            int index = viewModel.ColumnIndex;

            int i = 2, j = 4;
            switch (index)
            {
                case 2: i = 0; break;
                case 4: i = 0; j = 2; break;
            }

            double sum = splitGrid.ColumnDefinitions[i].Width.Value + splitGrid.ColumnDefinitions[j].Width.Value;
            if (sum > 0)
            {
                splitGrid.ColumnDefinitions[index].Width = new GridLength(0, GridUnitType.Star);
                return;
            }
        }
    }
}
