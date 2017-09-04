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

namespace SmartLogReader
{
    /// <summary>
    /// Interaction logic for FilterDialog.xaml
    /// </summary>
    public partial class FilterDialog : Dialog
    {
        public FilterDialog(LogControlVM viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Closing += MeClosing;
            if (LastSize.Width > 55 && LastSize.Height > 55)
            {
                Width = LastSize.Width;
                Height = LastSize.Height;
            }
        }

        public static Size LastSize;

        void MeClosing(object sender, CancelEventArgs e)
        {
            LastSize.Width = Width;
            LastSize.Height = Height;
        }
    }
}
