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
using System.Reflection;
using System.Windows.Input;

namespace SmartLogReader
{
	public class Dialog : Window
	{
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Key == Key.Escape)
				Close(false);
		}

		void Close(bool result)
		{
			bool isModal = (bool)typeof(Window).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
			if (!isModal)
				Close();
			else
				DialogResult = result;
		}

		protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
		{
			base.OnMouseRightButtonUp(e);
			Close(false);
		}

		public void OnButtonOK(object sender, RoutedEventArgs e)
		{
			Close(true);
		}
	}
}
