﻿//******************************************************************************************
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
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SmartLogReader
{
    public partial class WindowMain : Window
    {
        private static readonly SimpleLogger log = new SimpleLogger();
        private readonly DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);

        public WindowMain()
        {
            InitializeComponent();
            log.Debug(Title);

            Loaded += MeLoaded;
            Closing += MeClosing;
            RestoreSizeAndPosition();

            timer.Tick += TimerTick;
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Start();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
                Close();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Utils.ExtendFrameIntoClientArea(this, new Thickness(0, 64, 0, 0));
        }

        private void TimerTick(object sender, EventArgs e)
        {
            Title = $"SmartLogReader {App.Version.Major}.{App.Version.Minor}.{App.Version.Build}, {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }

        private void MeLoaded(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.IsMaximized)
                this.WindowState = WindowState.Maximized;

            if (App.OpenFileName == null)
            {
                smartLogControl.ViewModel = SmartLogControlVM.FromWorkspace(Properties.Settings.Default.LastWorkspace);
                smartLogControl.ViewModel.LoadFiles();
            }
            else
            {
                smartLogControl.ViewModel = SmartLogControlVM.FromWorkspace(null);
                smartLogControl.ViewModel.LoadFileFromCommandLine(App.OpenFileName);
            }
        }

        private void MeClosing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.LastWorkspace = smartLogControl.ViewModel.Shutdown();
            StoreSizeAndPosition();
        }

        private void RestoreSizeAndPosition()
        {
            var name = Properties.Settings.Default.ScreenName;
            var screen = Screen.LookUpByName(name);
            if (screen == null) 
                return;

            this.Top = Properties.Settings.Default.Top;
            this.Left = Properties.Settings.Default.Left;
            this.Width = Properties.Settings.Default.Width;
            this.Height = Properties.Settings.Default.Height;
            this.WindowState = WindowState.Normal;
            this.WindowStartupLocation = WindowStartupLocation.Manual;
        }

        private void StoreSizeAndPosition()
        {
            Properties.Settings.Default.IsMaximized = this.WindowState == WindowState.Maximized;

            if (this.WindowState != WindowState.Normal)
                this.WindowState = WindowState.Normal;

            var pt = new Point(this.Left, this.Top);
            var screen = Screen.LookUpByPixel(pt.ToPixel(this));
            Properties.Settings.Default.ScreenName = screen?.Name;

            Properties.Settings.Default.Top = this.Top;
            Properties.Settings.Default.Left = this.Left;
            Properties.Settings.Default.Width = this.Width;
            Properties.Settings.Default.Height = this.Height;
            Properties.Settings.Default.Save();
        }
    }
}
