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

using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;

namespace SmartLogReader.Common;

public class ColorBox : ComboBox
{
    static ColorBox()
    {
        AddColor(222, 222, 222);
        AddColor(202, 202, 202);
        AddColor(255, 171, 171);
        AddColor(255, 211, 171);
        AddColor(255, 255, 171);
        AddColor(226, 255, 171);
        AddColor(171, 233, 171);
        AddColor(171, 255, 255);
        AddColor(171, 211, 255);
        AddColor(171, 171, 255);
        AddColor(211, 211, 255);
        AddColor(244, 171, 244);
    }

    static void AddColor(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        MyBrushes.Add(brush);
    }

    static List<SolidColorBrush> MyBrushes = [];

    static public SolidColorBrush GetBrush(int index)
        => (index >= 0 && index < MyBrushes.Count) ? MyBrushes[index] : Brushes.Transparent;

    public ColorBox()
    {
        foreach (var brush in MyBrushes)
        {
            var rectangle = new Rectangle
            {
                Width = 30,
                Height = 15,
                Margin = new System.Windows.Thickness(0, 1, 0, 1),
                Fill = brush
            };

            Items.Add(rectangle);
        }
    }
}
