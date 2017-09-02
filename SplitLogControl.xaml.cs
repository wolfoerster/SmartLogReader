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
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using SmartLogging;

namespace SmartLogReader
{
    /// <summary>
    /// Interaction logic for SplitLogControl.xaml
    /// </summary>
    public partial class SplitLogControl : UserControl
    {
        /// <summary>
        /// 
        /// </summary>
        public SplitLogControl()
        {
            string name = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName;
            log = new SmartLogger($"{name}.{++instanceCounter}");
            InitializeComponent();
        }
        private readonly SmartLogger log;
        private static int instanceCounter;

        /// <summary>
        /// 
        /// </summary>
        public override string ToString()
        {
            return "SplitLogControl[" + viewModel.DisplayName + "]";
        }

        /// <summary>
        /// 
        /// </summary>
        public SplitLogControlVM ViewModel
        {
            get { return viewModel; }
            set
            {
                Utils.OnlyOnce(viewModel, value);
                DataContext = viewModel = value;
                viewModel.PropertyChanged += ViewModelPropertyChanged;
                CommandBindings.AddRange(viewModel.CommandBindings);

                myLogControl1.ViewModel = viewModel.MyLogControlVM1;
                myLogControl2.ViewModel = viewModel.MyLogControlVM2;
            }
        }
        private SplitLogControlVM viewModel;

        /// <summary>
        /// 
        /// </summary>
        void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ShowWaitControl")
            {
                waitControl.Progress = 0;
                waitControl.Visibility = Visibility.Visible;
            }
            else if (e.PropertyName == "HideWaitControl")
            {
                waitControl.Visibility = Visibility.Collapsed;
            }
            else if (e.PropertyName == "ShowProgress")
            {
                waitControl.Progress = viewModel.Reader.Progress;
            }
            //--- need RowDefinitions for the rest of this method
            else if (splitGrid.RowDefinitions.Count > 2)
            {
                if (e.PropertyName == "GridLengthsRequired")
                {
                    viewModel.GridLength0 = splitGrid.RowDefinitions[0].Height.Value;
                    viewModel.GridLength2 = splitGrid.RowDefinitions[2].Height.Value;
                }
                else if (e.PropertyName == "ApplyGridLengths")
                {
                    splitGrid.RowDefinitions[0].Height = new GridLength(viewModel.GridLength0, GridUnitType.Star);
                    splitGrid.RowDefinitions[2].Height = new GridLength(viewModel.GridLength2, GridUnitType.Star);
                }
                else if (e.PropertyName == "IsSplitLog")
                {
                    splitGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                    splitGrid.RowDefinitions[2].Height = new GridLength(viewModel.IsSplitLog ? 1 : 0, GridUnitType.Star);
                }
            }
        }
    }
}
