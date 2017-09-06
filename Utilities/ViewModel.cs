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
using System.Windows.Input;
using System.Xml.Serialization;

namespace SmartLogReader
{
    /// <summary>
    /// Base viewmodel with commands and a display name.
    /// </summary>
    public class ViewModel : Notifier
    {
        /// <summary>
        /// Static c'tor to create commands only once.
        /// </summary>
        static ViewModel()
        {
            InitCommands();
        }

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public CommandBindingCollection CommandBindings = new CommandBindingCollection();

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public string DisplayName
        {
            get { return displayName; }
            set
            {
                if (displayName != value)
                {
                    displayName = value;
                    FirePropertyChanged("DisplayName");
                }
            }
        }
        string displayName;

        /// <summary>
        /// 
        /// </summary>
        static void InitCommands()
        {
            CopyCmd = new UICommand("Copy selected", "Copy", Key.C, ModifierKeys.Control, "Ctrl+C");
            OpenCmd = new UICommand("Open log file", "Open", Key.O, ModifierKeys.Control, "Ctrl+O");
            CloseCmd = new UICommand("Close log file", "Close", Key.X, ModifierKeys.Control, "Ctrl+X");
            ConfigureCmd = new UICommand("Configure filter", "Configure", Key.C, ModifierKeys.Alt, "Alt+C");
            HighlightingCmd = new UICommand("Select highlighting colors", "Highlighting", Key.H, ModifierKeys.Control, "Ctrl+H");

            SplitCmd = new UICommand("Split log (TODO)", "Split", Key.S, ModifierKeys.Control, "Ctrl+S");
            FindCmd = new UICommand("Find record", "Find", Key.F, ModifierKeys.Control, "Ctrl+F");
            SearchUpCmd = new UICommand("Search records up (Shift+Enter, Shift+F3, F4)", "Search up", Key.Return, ModifierKeys.Shift, "Shift+Return");
            SearchDownCmd = new UICommand("Search records down (Enter, F3)", "Search down", Key.Return, ModifierKeys.None, "Return");
            SaveWorkspaceCmd = new UICommand("Save workspace (Ctrl+Enter)", "Save", Key.Return, ModifierKeys.Control, "Ctrl+Return");
            DeleteWorkspaceCmd = new UICommand("Delete workspace (Ctrl+D)", "Delete", Key.D, ModifierKeys.Control, "Ctrl+D");
        }

        static public UICommand CopyCmd { get; set; }
        static public UICommand OpenCmd { get; set; }
        static public UICommand CloseCmd { get; set; }
        static public UICommand ConfigureCmd { get; set; }
        static public UICommand HighlightingCmd { get; set; }

        static public UICommand SplitCmd { get; set; }
        static public UICommand SearchUpCmd { get; set; }
        static public UICommand FindCmd { get; set; }
        static public UICommand SearchDownCmd { get; set; }
        static public UICommand SaveWorkspaceCmd { get; set; }
        static public UICommand DeleteWorkspaceCmd { get; set; }
    }

    /// <summary>
    /// Viewmodel for controls with a dual SplitGrid.
    /// </summary>
    public class SplitGridViewModel2 : ViewModel
    {
        /// <summary>
        /// 
        /// </summary>
        public double GridLength0
        {
            get { return gridLength0; }
            set
            {
                value = Math.Round(value);
                if (gridLength0 != value)
                {
                    gridLength0 = value;
                    FirePropertyChanged("GridLength0");
                }
            }
        }
        private double gridLength0 = 1;

        /// <summary>
        /// 
        /// </summary>
        public double GridLength2
        {
            get { return gridLength2; }
            set
            {
                value = Math.Round(value);
                if (gridLength2 != value)
                {
                    gridLength2 = value;
                    FirePropertyChanged("GridLength2");
                }
            }
        }
        private double gridLength2 = 1;

        /// <summary>
        /// 
        /// </summary>
        public virtual void GetGridLengths()
        {
            FirePropertyChanged("GridLengthsRequired");
        }
    }

    /// <summary>
    /// Viewmodel for controls with a triple SplitGrid.
    /// </summary>
    public class SplitGridViewModel3 : SplitGridViewModel2
    {
        /// <summary>
        /// 
        /// </summary>
        public double GridLength4
        {
            get { return gridLength4; }
            set
            {
                value = Math.Round(value);
                if (gridLength4 != value)
                {
                    gridLength4 = value;
                    FirePropertyChanged("GridLength4");
                }
            }
        }
        private double gridLength4 = 1;
    }
}
