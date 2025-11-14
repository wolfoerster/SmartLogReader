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
    /// Interaction logic for FilterControl.xaml
    /// </summary>
    public partial class FilterControl : UserControl
    {
        public FilterControl()
        {
            InitializeComponent();
        }

        public string ButtonText { get; set; }

        public FilterCollection ItemsSource
        {
            get { return (FilterCollection)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(FilterCollection), typeof(FilterControl),
            new FrameworkPropertyMetadata(null, OnItemsSourceChanged));

        static void OnItemsSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            FilterControl filterControl = obj as FilterControl;
            filterControl?.MyItemsSourceChanged();
        }

        public void MyItemsSourceChanged()
        {
            buttonAdd.Content = ButtonText;
            listBox.ItemsSource = ItemsSource;
        }

        void OnButtonAdd(object sender, RoutedEventArgs e)
        {
            FilterCollection data = listBox.ItemsSource as FilterCollection;
            Filter filter = new Filter(7, 0, "*");
            data.Add(filter);
        }

        void OnButtonX(object sender, RoutedEventArgs e)
        {
            ListBoxItem lbItem = Utils.FindParent<ListBoxItem>(sender as Button);
            if (lbItem != null)
            {
                Filter filter = lbItem.Content as Filter;
                if (filter != null)
                {
                    FilterCollection filterList = ItemsSource as FilterCollection;
                    filterList?.Remove(filter);
                }
            }
        }
    }
}
