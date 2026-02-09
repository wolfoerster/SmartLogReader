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

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SmartLogging;
using SmartLogReader.Common;
using SmartLogReader.Properties;

namespace SmartLogReader
{
    public partial class WindowMain : Window
    {
        private static readonly SmartLogger Log = new SmartLogger();
        private readonly Properties.Settings Settings = Settings.Default;
        private readonly DispatcherTimer Timer = new DispatcherTimer(DispatcherPriority.Background);

        public WindowMain()
        {
            InitializeComponent();
            Log.Debug(Title);

            Loaded += MeLoaded;
            Closing += MeClosing;
            RestoreSizeAndPosition();

            Timer.Tick += TimerTick;
            Timer.Interval = TimeSpan.FromSeconds(1);
            Timer.Start();
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
            ByteParserManager.Initialize(Settings.LastParsers);

            if (Settings.IsMaximized)
                WindowState = WindowState.Maximized;

            if (App.OpenFileName == null)
            {
                smartLogControl.ViewModel = SmartLogControlVM.FromWorkspace(Settings.LastWorkspace);
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
            Settings.LastWorkspace = smartLogControl.ViewModel.Shutdown();
            StoreSizeAndPosition();
            LogWriter.Flush();
        }

        private void RestoreSizeAndPosition()
        {
            var name = Settings.ScreenName;
            var screen = Screen.LookUpByName(name);
            if (screen == null) 
                return;

            Top = Settings.Top;
            Left = Settings.Left;
            Width = Settings.Width;
            Height = Settings.Height;
            WindowState = WindowState.Normal;
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        private void StoreSizeAndPosition()
        {
            Settings.IsMaximized = WindowState == WindowState.Maximized;

            if (WindowState != WindowState.Normal)
                WindowState = WindowState.Normal;

            var pt = new Point(Left, Top);
            var screen = Screen.LookUpByPixel(pt.ToPixel(this));
            Settings.ScreenName = screen?.Name;

            Settings.Top = Top;
            Settings.Left = Left;
            Settings.Width = Width;
            Settings.Height = Height;
            Settings.LastParsers = ByteParserManager.LastParsers;
            Settings.Save();
        }
    }
}
