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
using System.IO;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SmartLogReader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly SimpleLogger log = new SimpleLogger();

        /// <summary>
        /// 
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string theme = "PresentationFramework.Aero;V3.0.0.0;31bf3856ad364e35;component\\themes/aero.normalcolor.xaml";
            //string theme = "/PresentationFramework.Classic;v3.0.0.0;31bf3856ad364e35;Component/themes/classic.xaml";
            //string theme = "/PresentationFramework.Royale;v3.0.0.0;31bf3856ad364e35;Component/themes/royale.normalcolor.xaml";
            //string theme = "/PresentationFramework.Luna;v3.0.0.0;31bf3856ad364e35;Component/themes/luna.normalcolor.xaml";
            //string theme = "/PresentationFramework.Luna;v3.0.0.0;31bf3856ad364e35;Component/themes/luna.homestead.xaml";
            //string theme = "/PresentationFramework.Luna;v3.0.0.0;31bf3856ad364e35;Component/themes/luna.metallic.xaml";
            Uri uri = new Uri(theme, UriKind.Relative);
            Resources.MergedDictionaries.Add(Application.LoadComponent(uri) as ResourceDictionary);
        }

        /// <summary>
        /// 
        /// </summary>
        static App()
        {
            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(UIElement), new FrameworkPropertyMetadata(30000));
            ToolTipService.ShowOnDisabledProperty.OverrideMetadata(typeof(UIElement), new FrameworkPropertyMetadata(true));

            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyName name = assembly.GetName();
            Version = name.Version;
        }
        public static Version Version;

        /// <summary>
        /// If a command line parameter is specified it must be either "/nocheck", "/restart" or a file name. 
        /// "/restart" is used internally during updates. 
        /// "/nocheck" will not check for updates.
        /// </summary>
        public App()
        {
            //--- get the name of the executable and optional parameter
            string[] args = Environment.GetCommandLineArgs();
            string option = args.Length > 1 ? args[1] : null;
            localName = args[0];
            localDir = Path.GetDirectoryName(localName);
            blockingFile = localDir + "\\SmartLogReader.blocking";

            //--- initialize logging
            SimpleLogger.Init();
            SimpleLogger.MinimumLogLevel = GetMinimumLogLevel();

            //--- don't start twice
            String myprocessname = Process.GetCurrentProcess().ProcessName;
            int count = Process.GetProcesses().Count(p => p.ProcessName == myprocessname);
            if (count > 1)
            {
                log.Debug($"found {count} processes with the same name {myprocessname} ==> shutdown");
                Shutdown();
                return;
            }

            string action = option == "/restart" ? "Restarting" : "Starting";
            log.Debug($"{action} application {localName}, option: '{option}'");

            //--- attach overall exception handler
            DispatcherUnhandledException += MeDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += MeDomainUnhandledException;

            //--- if option is "/nocheck", we're done
            if (option == "/nocheck")
            {
                log.Debug($"Option '{option}' detected ==> will not check for new version");
                return;
            }

            //--- if option specifies a file name, we're done
            if (File.Exists(option))
            {
                OpenFileName = option;
                log.Debug($"File '{option}' will be opened ==> will not check for new version");
                return;
            }

#if false
            //--- so option is either "/restart" or unknown
            if (NeedToRestart())
                Shutdown();
#endif
        }
        public static string OpenFileName;
        string localName, localDir, blockingFile;

        /// <summary>
        /// 
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            log.Debug($"Exiting application {localName}");
            base.OnExit(e);
        }

        /// <summary>
        /// 
        /// </summary>
        LogLevel GetMinimumLogLevel()
        {
            string str = ConfigurationManager.AppSettings["MinimumLogLevel"];
            if (Enum.TryParse<LogLevel>(str, out LogLevel logLevel))
                return logLevel;

            return LogLevel.Information;
        }

        /// <summary>
        /// 
        /// </summary>
        void MeDomainUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            log.Exception(e);
            log.Debug($"args.IsTerminating = {args.IsTerminating}");
            MessageBox.Show(e.Message, "DomainUnhandledException");
        }

        /// <summary>
        /// 
        /// </summary>
        void MeDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.Exception;
            log.Exception(e);
            MessageBox.Show(e.Message, "DispatcherUnhandledException");
            args.Handled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        bool NeedToRestart()
        {
            string original = localDir + "\\SmartLogReader.exe";
            string tempName = localDir + "\\SmartLogReaderTemp.exe";

            //--- if this is the temporary executable, we're in the middle of the update
            if (localName.equals(tempName))
            {
                log.Debug($"About to copy temp file to {original}");
                if (CopyFile(localName, original))
                    Start(original);

                return true;
            }
            else//--- the original executable is running
            {
                //--- if there is a temporary executable, we're nearly finished with the update
                if (File.Exists(tempName))
                {
                    log.Debug($"About to delete temp file {tempName}");
                    DeleteFile(tempName);
                    UpdateFinished();
                    return false;
                }

                //--- check for new version
                string newName = CheckForNewFile();
                if (newName != null)
                {
                    //--- start the update process
                    log.Debug($"About to copy new file to {tempName}");
                    if (CopyFile(newName, tempName))
                        Start(tempName);

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        async void UpdateFinished()
        {
            IsBlocked = false;
            await Task.Run(() => updateFinished());
        }

        void updateFinished()
        {
            //try
            //{
            //	lock (locker)
            //	{
            //		string ids = string.Format("{0}/{1}/{2}", Process.GetCurrentProcess().Id, AppDomain.CurrentDomain.Id, Thread.CurrentThread.ManagedThreadId);
            //		string msg = string.Format("{0}{1}Update finished on '{2}'\r\n", DateTime.Now.ToString("dd-MMM-yy HH:mm:ss.fff "), Utils.FillUp(ids, 12), Environment.MachineName);
            //		File.AppendAllText(@"\\remotecomputer\temp\SmartLogReader.log", msg);
            //	}
            //}
            //catch
            //{
            //}
        }
        private static object locker = new Object();

        /// <summary>
        /// 
        /// </summary>
        void Start(string path)
        {
            log.Debug($"About to start {Path.GetFileName(path)}");
            Process.Start(path, "/restart");
        }

        /// <summary>
        /// 
        /// </summary>
        string CheckForNewFile()
        {
            if (IsBlocked)
            {
                log.Debug("Blocking file detected! Will shutdown.");
                Shutdown();
                return null;
            }
            IsBlocked = true;

            FileInfo localInfo = GetFileInfo(localName);
            FileInfo remoteInfo = GetRemoteFileInfo();

            if (remoteInfo != null)
            {
                if (remoteInfo.LastWriteTime > localInfo.LastWriteTime)
                {
                    log.Debug("Found new version on remote directory");
                    //--- IsBlocked will be set to false in UpdateFinished()
                    return remoteInfo.FullName;
                }

                log.Debug("No new version on remote directory");
            }

            IsBlocked = false;
            return null;
        }

        /// <summary>
        /// Prevent two instances to update simultaneously.
        /// </summary>
        bool IsBlocked
        {
            get { return File.Exists(blockingFile); }
            set
            {
                if (value == false)
                {
                    log.Debug("Remove blocking file");
                    Utils.DeleteFile(blockingFile);
                }
                else
                {
                    log.Debug("Create blocking file");
                    lock (locker)
                    {
                        File.WriteAllText(blockingFile, "Blocking new instances");
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        FileInfo GetRemoteFileInfo()
        {
            string remoteDir = ConfigurationManager.AppSettings["RemoteDir"];
            if (string.IsNullOrWhiteSpace(remoteDir))
            {
                log.Debug("Did not find RemoteDir in appSettings");
                return null;
            }

            if (!Directory.Exists(remoteDir))
            {
                log.Debug($"Did not find remote directory >{remoteDir}<");
                return null;
            }

            log.Debug($"Remote directory is {remoteDir}");
            return GetFileInfo(remoteDir + "\\" + Path.GetFileName(localName));
        }

        /// <summary>
        /// 
        /// </summary>
        FileInfo GetFileInfo(string name)
        {
            if (!File.Exists(name))
            {
                log.Debug($"File >{name}< does not exist");
                return null;
            }
            try
            {
                FileInfo fileInfo = new FileInfo(name);
                log.Debug($"LastWriteTime is {fileInfo.LastWriteTime}");
                return fileInfo;
            }
            catch (Exception e)
            {
                log.Exception(e);
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        bool CopyFile(string source, string dest)
        {
            while (true)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (Utils.CopyFile(source, dest))
                    {
                        log.Debug("success");
                        return true;
                    }
                    Sleep();
                }
                string msg = string.Format("could not copy file '{0}' to target '{1}'. Check if target is running. Try again?", source, dest);
                MessageBoxResult res = MessageBox.Show(msg, "Copy File Error", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes)
                    break;
            }
            log.Debug("cancelled");
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        bool DeleteFile(string source)
        {
            while (true)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (Utils.DeleteFile(source))
                    {
                        log.Debug("success");
                        return true;
                    }
                    Sleep();
                }
                string msg = string.Format("could not delete file '{0}'. Check if file is running. Try again?", source);
                MessageBoxResult res = MessageBox.Show(msg, "Delete File Error", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes)
                    break;
            }
            log.Debug("cancelled");
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        void Sleep()
        {
            int delay = 50;
            log.Debug($"Going to sleep for {delay}ms");
            Thread.Sleep(delay);
            log.Debug("Woke up from sleep");
        }
    }
}
