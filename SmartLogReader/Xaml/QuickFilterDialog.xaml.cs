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

using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace SmartLogReader
{
    /// <summary>
    /// Interaction logic for QuickFilterDialog.xaml
    /// </summary>
    public partial class QuickFilterDialog : Dialog
    {
        /// <summary>
        /// 
        /// </summary>
        public QuickFilterDialog(LogControlVM viewModel, Record record)
        {
            InitializeComponent();
            ViewModel = viewModel;
            Closing += MeClosing;

            List<IndexValuePair> list = Filter.GetRecordProperties(record);

            foreach (var item in list)
                item.ViewModel = viewModel;

            IndexValuePair.FirstIncluded = IndexValuePair.FirstExcluded = -1;
            listBox.ItemsSource = list;

            if (LastSize.Width > 55 && LastSize.Height > 55)
            {
                Width = LastSize.Width;
                Height = LastSize.Height;
            }

            filterWarning.ViewModel = viewModel;
            if (!viewModel.IsFilterEnabled)
            {
                filterWarning.Visibility = Visibility.Visible;
            }
        }
        LogControlVM ViewModel;

        public static Size LastSize;

        /// <summary>
        /// 
        /// </summary>
        void MeClosing(object sender, CancelEventArgs e)
        {
            LastSize.Width = Width;
            LastSize.Height = Height;
            RemoveLastAnd(ViewModel.IncludeList);
            RemoveLastAnd(ViewModel.ExcludeList);
        }

        /// <summary>
        /// 
        /// </summary>
        void RemoveLastAnd(FilterCollection list)
        {
            if (list.Count > 0)
                list[list.Count - 1].AndNext = false;
        }
    }
}
