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
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SmartLogging;

namespace SmartLogReader
{
    public partial class WindowMain : Window
	{
        private static readonly SmartLogger log = new SmartLogger();

        public WindowMain()
		{
			InitializeComponent();
            log.Smart(Title);

			Loaded += MeLoaded;
			Closing += MeClosing;

            timer.Tick += TimerTick;
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Start();

			#region Window size and position

			Top = Properties.Settings.Default.Top;
			Left = Properties.Settings.Default.Left;
			Width = Properties.Settings.Default.Width;
			Height = Properties.Settings.Default.Height;

			doMaximize = false;
			WindowState = WindowState.Normal;

			Screen screen = Utils.GetScreenByName(Properties.Settings.Default.ScreenName);
            log.Smart($"Left = {Left}, Top = {Top}, screen = '{screen?.Name}', {Properties.Settings.Default.ScreenName}");
            log.Smart($"ScreenName = {Properties.Settings.Default.ScreenName}, screen = '{screen?.Name}'");
			if (screen == null)
			{
				screen = Utils.GetPrimaryScreen();
				Top = screen.WorkArea.Top;
				Left = screen.WorkArea.Left + 110;
				Width = screen.WorkArea.Width - 270;
				Height = screen.WorkArea.Height;
			}
			else
			{
				doMaximize = Properties.Settings.Default.WindowState == 2;
				if (doMaximize)
				{
					Top = screen.WorkArea.Top + 1;
					Left = screen.WorkArea.Left + 1;
					Width = screen.WorkArea.Width - 2;
					Height = screen.WorkArea.Height - 2;
                    log.Smart($"Left = {Left}, Top = {Top}, screen = '{screen?.Name}', {Properties.Settings.Default.ScreenName}");
                }
            }

			#endregion Window size and position
		}
        bool doMaximize;
        DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);

        private void TimerTick(object sender, EventArgs e)
        {
            Title = $"SmartLogReader {App.Version.Major}.{App.Version.Minor}, {DateTime.Now.ToString("HH:mm:ss")}";
        }

        void MeLoaded(object sender, RoutedEventArgs e)
		{
            log.Smart();
            if (doMaximize)
				WindowState = WindowState.Maximized;

            smartLogControl.ViewModel = SmartLogControlVM.FromXML(Properties.Settings.Default.SmartLogControlVM);
            smartLogControl.ViewModel.LoadFiles();
		}

		void MeClosing(object sender, CancelEventArgs e)
		{
            if (WindowState == WindowState.Minimized)
            {
                log.Smart("state is minimized", LogLevel.Warn);
                WindowState = WindowState.Normal;
            }
            var screen = WindowState == WindowState.Maximized ? Utils.GetScreenByPixel(Left + Width / 2, Top + Height / 2) : Utils.GetScreenByPixel(Left, Top);
            log.Smart($"Left = {Left}, Top = {Top}, screen = '{screen?.Name}'");
            Properties.Settings.Default.Top = Top;
			Properties.Settings.Default.Left = Left;
			Properties.Settings.Default.Width = Width;
			Properties.Settings.Default.Height = Height;
			Properties.Settings.Default.WindowState = (int)WindowState;
			Properties.Settings.Default.ScreenName = screen?.Name;
            Properties.Settings.Default.SmartLogControlVM = smartLogControl.ViewModel.Shutdown();
			Properties.Settings.Default.Save();
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
	}
}
