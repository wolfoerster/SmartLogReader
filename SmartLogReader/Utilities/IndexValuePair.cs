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

using System.Windows;

namespace SmartLogReader
{
    /// <summary>
    /// 
    /// </summary>
    public class IndexValuePair : Notifier
    {
        /// <summary>
        /// 
        /// </summary>
        public IndexValuePair(int propertyIndex, string propertyValue)
        {
            PropertyIndex = propertyIndex;
            PropertyValue = propertyValue;
        }
        int PropertyIndex;

        /// <summary>
        /// 
        /// </summary>
        public string PropertyName
        {
            get { return Filter.PropertyNames[PropertyIndex]; }
            set { }
        }

        /// <summary>
        /// 
        /// </summary>
        public string PropertyValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public override string ToString()
        {
            return string.Format("IndexValuePair[{0},{1},{2}]", PropertyIndex, PropertyName, PropertyValue);
        }

        /// <summary>
        /// 
        /// </summary>
        public LogControlVM ViewModel { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsIncluded
        {
            get { return includedFilter != null; }
            set
            {
                OnPropertyChanged();
                ModifyList(ViewModel.IncludeList, ref includedFilter, ref FirstIncluded, value);
            }
        }
        Filter includedFilter;
        public static int FirstIncluded = -1;

        /// <summary>
        /// 
        /// </summary>
        public bool IsExcluded
        {
            get { return excludedFilter != null; }
            set
            {
                OnPropertyChanged();
                ModifyList(ViewModel.ExcludeList, ref excludedFilter, ref FirstExcluded, value);
            }
        }
        Filter excludedFilter;
        public static int FirstExcluded = -1;

        /// <summary>
        /// 
        /// </summary>
        void ModifyList(FilterCollection filterList, ref Filter filter, ref int indexOfFirst, bool createNewFilter)
        {
            if (createNewFilter)
            {
                if (PropertyIndex == 8 && PropertyValue.IndexOf('\n') > 0)
                {
                    MessageBox.Show("Only a single JSON property can be specified in this version.\r\nPlease delete all lines except one.", "We're sorry!", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                filter = new Filter(PropertyIndex, 0, PropertyValue);
                filter.AndNext = true;
                filterList.Add(filter);
            }
            else
            {
                filterList.Remove(filter);
                filter = null;
            }

            ViewModel.NotifyFilterChanged();
        }
    }
}
