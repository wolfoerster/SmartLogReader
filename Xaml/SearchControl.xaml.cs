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

namespace SmartLogReader
{
	/// <summary>
	/// Interaction logic for SearchControl.xaml
	/// </summary>
	public partial class SearchControl : UserControl
	{
		/// <summary>
		/// 
		/// </summary>
		public SearchControl()
		{
			InitializeComponent();
			DataContextChanged += MeDataContextChanged;
		}

		/// <summary>
		/// 
		/// </summary>
		void MeDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			Utils.OnlyOnce(viewModel, e.NewValue);
			viewModel = e.NewValue as SmartLogControlVM;
			viewModel.PropertyChanged += ViewModelPropertyChanged;
		}
		SmartLogControlVM viewModel;

		/// <summary>
		/// 
		/// </summary>
		void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "SetFocusOnSearchBox")
			{
				textBox.Focus();
				textBox.SelectAll();
			}
		}
	}
}
