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
using System.Windows.Media;
using System.Windows.Controls;

namespace SmartLogReader
{
    public class SplitGrid : Grid
    {
        public SplitGrid()
        {
            Loaded += MeLoaded;
        }

        void MeLoaded(object sender, RoutedEventArgs e)
        {
            double length = 4;
            SplitGridViewModel2 viewModel2 = DataContext as SplitGridViewModel2;
            SplitGridViewModel3 viewModel3 = DataContext as SplitGridViewModel3;

            if (viewModel3 == null)//--- create a single splitter at row 1
            {
                RowDefinitions.Add(new RowDefinition() { Height = new GridLength(viewModel2.GridLength0, GridUnitType.Star) });
                RowDefinitions.Add(new RowDefinition() { Height = new GridLength(length, GridUnitType.Pixel) });
                RowDefinitions.Add(new RowDefinition() { Height = new GridLength(viewModel2.GridLength2, GridUnitType.Star) });

                GridSplitter splitter = NewGridSplitter(1, 0);
                Children.Add(splitter);
            }
            else//--- create two splitters at column 1 and column 3
            {
                ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(viewModel3.GridLength0, GridUnitType.Star) });
                ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(length, GridUnitType.Pixel) });
                ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(viewModel3.GridLength2, GridUnitType.Star) });
                ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(length, GridUnitType.Pixel) });
                ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(viewModel3.GridLength4, GridUnitType.Star) });

                GridSplitter splitter = NewGridSplitter(0, 1);
                Children.Add(splitter);

                splitter = NewGridSplitter(0, 3);
                Children.Add(splitter);
            }
        }

        private static GridSplitter NewGridSplitter(int row, int col)
        {
            GridSplitter splitter = new GridSplitter();

            splitter.Focusable = false;
            splitter.Background = Brushes.Orange;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;

            Grid.SetRow(splitter, row);
            Grid.SetColumn(splitter, col);

            return splitter;
        }
    }
}
