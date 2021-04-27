//******************************************************************************************
// Copyright © 2021 Wolfgang Foerster (wolfoerster@gmx.de)
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
using System.Windows.Input;
using System.Windows.Media;

namespace SmartLogReader
{
    public class FilterWarning : TextBlock
    {
        public FilterWarning()
        {
            FontSize = 13;
            Padding = new Thickness(0, 2, 0, 0);
            Text = "Filtering is not enabled! Click here to enable it.";

            Foreground = Brushes.White;
            Background = Brushes.Firebrick;

            TextAlignment = TextAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Stretch;

            Visibility = Visibility.Collapsed;
        }

        public LogControlVM ViewModel { get; set; }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            ViewModel.MakeFilterEnabled();
            Visibility = Visibility.Collapsed;
        }
    }
}
