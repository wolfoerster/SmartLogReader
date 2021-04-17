//******************************************************************************************
// Copyright © 2017 - 2021 Wolfgang Foerster (wolfoerster@gmx.de)
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
using System;
using System.Windows;
using System.Windows.Input;

namespace SmartLogReader
{
    public class Dialog : Window
    {
        private bool showAsDialog;

        public Dialog()
        {
            Loaded += MeLoaded;
        }

        public new void Show()
        {
            throw new InvalidOperationException("use Show(string title)");
        }

        public new bool? ShowDialog()
        {
            throw new InvalidOperationException("use ShowDialog(string title)");
        }

        public void Show(string title)
        {
            showAsDialog = false;
            SetTitle(title);
            base.Show();
        }

        public bool ShowDialog(string title)
        {
            showAsDialog = true;
            SetTitle(title);
            return base.ShowDialog() ?? false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
                Close(false);
        }

        void Close(bool result)
        {
            if (showAsDialog)
                DialogResult = result;
            else
                Close();
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

        private void SetTitle(string title)
        {
            Title = title + "   (right click or Esc to cancel)";
        }

        private void MeLoaded(object sender, RoutedEventArgs e)
        {
            CenterUnderneathMouse();
        }

        private void CenterUnderneathMouse()
        {
            var mousePos = Mouse.GetPosition(this); // in dips
            var screenPos = PointToScreen(mousePos); // in pixel

            // get the actual size and calculate a position
            var winSize = new Point(ActualWidth, ActualHeight); // in dips
            winSize = winSize.ToPixel(this); // in pixel
            var topLeft = new Point(screenPos.X - winSize.X / 2, screenPos.Y - winSize.Y / 2);

            // top left corner must be on screen
            var screen = Screen.LookUpByPixel(screenPos);
            var workArea = screen.WorkArea; // in pixel
            topLeft.X = Math.Max(topLeft.X, workArea.Left);
            topLeft.Y = Math.Max(topLeft.Y, workArea.Top);

            // bottom right corner must be on screen
            topLeft.X = Math.Min(topLeft.X, workArea.Right - winSize.X);
            topLeft.Y = Math.Min(topLeft.Y, workArea.Bottom - winSize.Y);

            // transform to dips and set position
            topLeft = topLeft.ToDip(this);
            Top = topLeft.Y;
            Left = topLeft.X;
        }
    }
}
