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
using System.Windows.Controls;

namespace SmartLogReader
{
    /// <summary>
    /// Interaction logic for ColorSpecDialog.xaml
    /// </summary>
    public partial class HighLightingDialog : Dialog
    {
        public HighLightingDialog(ColorSpecCollection colorSpecs)
        {
            InitializeComponent();
            DataContext = ColorSpecs = colorSpecs;
        }
        ColorSpecCollection ColorSpecs;

        void OnButtonNew(object sender, RoutedEventArgs e)
        {
            ColorSpecs.Insert(0, new ColorSpec(7, 0, "*"));
        }

        void OnButtonX(object sender, RoutedEventArgs e)
        {
            ListBoxItem lbItem = Utils.FindParent<ListBoxItem>(sender as Button);
            if (lbItem != null)
            {
                ColorSpec colorSpec = lbItem.Content as ColorSpec;
                ColorSpecs.Remove(colorSpec);
            }
        }

        void OnButtonReset(object sender, RoutedEventArgs e)
        {
            ColorSpecs.Clear();
            ColorSpecCollection colorSpecs = Record.GetDefaultColorSpecs();
            foreach (var item in colorSpecs)
                ColorSpecs.Add(item);
        }

        void OnButtonClear(object sender, RoutedEventArgs e)
        {
            ColorSpecs.Clear();
        }
    }
}
