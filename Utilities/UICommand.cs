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
using System.Windows.Input;
using System.Collections.Generic;

namespace SmartLogReader
{
	/// <summary>
	/// 
	/// </summary>
	public class UICommand : RoutedUICommand
	{
		/// <summary>
		/// 
		/// </summary>
		public UICommand()
        {
        }

		/// <summary>
		/// 
		/// </summary>
		public UICommand(string text, string name, KeyGesture keyGesture) :
			base(text, name, typeof(FrameworkElement), new InputGestureCollection(new List<InputGesture>() { keyGesture }))
		{
		}

		/// <summary>
		/// 
		/// </summary>
		public UICommand(string text, string name, Key key, ModifierKeys modifier, string str) :
			this(text, name, new KeyGesture(key, modifier, str) )
		{
		}
	}
}
